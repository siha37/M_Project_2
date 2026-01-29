using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._4._Network
{
    /// <summary>
    /// Unity Lobby Service 전용 관리자 (Room 기반)
    /// </summary>
    public class RoomManager : SingleTone<RoomManager>
    {
        private Lobby currentRoom;
        private CancellationTokenSource heartbeatCts;
        private int customMaxPlayers;

        public int CustomMaxPlayers => customMaxPlayers;

        public event Action<List<RoomInfo>> OnRoomListReceived;
        
        /// <summary>
        /// 룸 생성
        /// </summary>
        public async Task<(bool success, string roomId)> CreateRoomAsync(string roomName, int maxPlayers, bool isPrivate = false)
        {
            customMaxPlayers = maxPlayers;
            if (NetworkStateManager.Instance.CurrentState != NetworkState.Connected)
                return (false, null);
                
            try
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.CreatingLobby, "룸 생성 시작");
                var relayResult = await GameNetworkManager.Instance.StartHostAsync(maxPlayers);
                if (!relayResult.success)
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "릴레이 시작 실패");
                    return (false, null);
                }
                var options = new CreateLobbyOptions
                {
                    IsPrivate = isPrivate,
                    Data = new Dictionary<string, DataObject>
                    {
                        {"gameType", new DataObject(DataObject.VisibilityOptions.Public, "default") },
                        {"joinCode",new DataObject(DataObject.VisibilityOptions.Public,relayResult.joinCode)},
                        {"isJoinable", new DataObject(DataObject.VisibilityOptions.Public, "False")}
                    }
                };
                
                currentRoom = await LobbyService.Instance.CreateLobbyAsync(roomName, maxPlayers, options);
                NetworkStateManager.Instance.SetLobbyInfo(currentRoom.Id, true);
                
                // 호스트 ID 확인 로그
                LogManager.Log(LogCategory.Lobby, $"룸 생성 완료 - 호스트 ID: {currentRoom.HostId}, 현재 사용자 ID: {NetworkStateManager.Instance.CurrentUserId}");
                
                StartHeartbeatIfOwner();
                
                NetworkStateManager.Instance.ChangeState(NetworkState.InLobby, "룸 생성 완료");
                
                return (true, currentRoom.Id);
            }
            catch (Exception ex)
            {
                NetworkStateManager.Instance.SetError($"룸 생성 실패: {ex.Message}");
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "룸 생성 실패");
                return (false, null);
            }
        }
        
        /// <summary>
        /// 방 상태 업데이트 (호스트 전용)
        /// </summary>
        public async Task<bool> UpdateRoomStatusAsync(bool isJoinable)
        {
            if (currentRoom == null)
            {
                LogManager.LogWarning(LogCategory.Lobby, "현재 로비가 없습니다.", this);
                return false;
            }

            // 호스트만 상태 변경 가능
            if (currentRoom.HostId != NetworkStateManager.Instance.CurrentUserId)
            {
                LogManager.LogWarning(LogCategory.Lobby, "호스트만 방 상태를 변경할 수 있습니다.", this);
                return false;
            }

            try
            {
                var updateOptions = new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"gameType", new DataObject(DataObject.VisibilityOptions.Public, "default")},
                        {"joinCode", new DataObject(DataObject.VisibilityOptions.Public, currentRoom.Data?.GetValueOrDefault("joinCode")?.Value)},
                        {"isJoinable", new DataObject(DataObject.VisibilityOptions.Public, isJoinable.ToString())}
                    }
                };

                currentRoom = await LobbyService.Instance.UpdateLobbyAsync(currentRoom.Id, updateOptions);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Lobby, $"방 상태 업데이트 실패: {ex.Message}", this);
                NetworkStateManager.Instance.SetError($"방 상태 업데이트 실패: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 룸 참가
        /// </summary>
        public async Task<bool> JoinRoomAsync(string roomId)
        {
            if (NetworkStateManager.Instance.CurrentState != NetworkState.Connected)
                return false;
                
            try
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.JoiningLobby, "룸 참가 시작");
                
                
                // 기존 로비에서 나가기 (이미 로비에 있는 경우 처리)
                if (currentRoom != null)
                {
                    await LeaveCurrentLobbyQuietly();
                }
                
                // 1) 로비 선참가 (Member 데이터 접근)
                currentRoom = await LobbyService.Instance.JoinLobbyByIdAsync(roomId);
                if (currentRoom == null)
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "로비 참가 실패");
                    return false;
                }

                customMaxPlayers = currentRoom.MaxPlayers;
                
                // 2) 로비에서 joinCode 획득 후 Relay 접속
                var code = currentRoom.Data?.GetValueOrDefault("joinCode")?.Value;
                if (string.IsNullOrEmpty(code))
                {
                    NetworkStateManager.Instance.SetError("JoinCode를 가져올 수 없습니다.");
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "룸 참가 실패");
                    return false;
                }

                bool joinedRelay = await GameNetworkManager.Instance.JoinGameAsync(code);
                if (!joinedRelay)
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "Relay 참가 실패");
                    return false;
                }

                NetworkStateManager.Instance.SetLobbyInfo(currentRoom.Id, false);
                
                // 참가한 로비의 호스트 ID 확인
                LogManager.Log(LogCategory.Lobby, $"룸 참가 완료 - 호스트 ID: {currentRoom.HostId}, 현재 사용자 ID: {NetworkStateManager.Instance.CurrentUserId}");
                
                NetworkStateManager.Instance.ChangeState(NetworkState.InLobby, "룸 참가 완료");
                return true;
            }
            catch (Exception ex)
            {
                NetworkStateManager.Instance.SetError($"룸 참가 실패: {ex.Message}");
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "룸 참가 실패");
                return false;
            }
        }
        
        /// <summary>
        /// Join Code로 비공개방 참가
        /// </summary>
        public async Task<bool> JoinPrivateRoomByCodeAsync(string joinCode)
        {
            if (NetworkStateManager.Instance.CurrentState != NetworkState.Connected)
                return false;
            try
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.JoiningLobby, "비공개방 참가 시작");
                
                // 기존 로비에서 나가기
                if (currentRoom != null)
                {
                    await LeaveCurrentLobbyQuietly();
                }
                
                // Join Code로 로비 참가
                currentRoom = await LobbyService.Instance.JoinLobbyByCodeAsync(joinCode);
                if (currentRoom == null)
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "비공개방 참가 실패");
                    return false;
                }

                customMaxPlayers = currentRoom.MaxPlayers;
                
                // Relay 연결
                var code = currentRoom.Data?.GetValueOrDefault("joinCode")?.Value;
                if (string.IsNullOrEmpty(code))
                {
                    NetworkStateManager.Instance.SetError("JoinCode를 가져올 수 없습니다.");
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "비공개방 참가 실패");
                    return false;
                }

                bool joinedRelay = await GameNetworkManager.Instance.JoinGameAsync(code);
                if (!joinedRelay)
                {
                    NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "Relay 참가 실패");
                    return false;
                }

                NetworkStateManager.Instance.SetLobbyInfo(currentRoom.Id, false);
                
                // 비공개방 참가 시 호스트 ID 확인
                LogManager.Log(LogCategory.Lobby, $"비공개방 참가 완료 - 호스트 ID: {currentRoom.HostId}, 현재 사용자 ID: {NetworkStateManager.Instance.CurrentUserId}");
                
                NetworkStateManager.Instance.ChangeState(NetworkState.InLobby, "비공개방 참가 완료");
                return true;
            }
            catch (Exception ex)
            {
                NetworkStateManager.Instance.SetError($"비공개방 참가 실패: {ex.Message}");
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "비공개방 참가 실패");
                return false;
            }
        }

        
        /// <summary>
        /// 룸 목록 조회
        /// </summary>
        public async Task RefreshRoomListAsync()
        {
            try
            {
                var response = await LobbyService.Instance.QueryLobbiesAsync();
                var roomList = new List<RoomInfo>();
                
                foreach (var lobby in response.Results)
                {
                    if(!lobby.IsPrivate)
                    {
                        roomList.Add(new RoomInfo
                        {
                            roomId = lobby.Id,
                            roomName = lobby.Name,
                            currentPlayers = lobby.Players.Count,
                            maxPlayers = lobby.MaxPlayers,
                            gameType = lobby.Data?.GetValueOrDefault("gameType")?.Value ?? "default",
                            isPrivate = lobby.IsPrivate,
                            joinCode = lobby.Data?.GetValueOrDefault("joinCode")?.Value,
                            isJoinable = bool.TryParse(lobby.Data?.GetValueOrDefault("isJoinable")?.Value, out bool joinable) && joinable
                        });
                    }
                }
                
                OnRoomListReceived?.Invoke(roomList);
            }
            catch (Exception ex)
            {
                NetworkStateManager.Instance.SetError($"룸 목록 조회 실패: {ex.Message}");
            }
        }
        
        public Lobby GetCurrentRoom() => currentRoom;
        
        /// <summary>
        /// 룸 나가기
        /// </summary>
        public async Task LeaveRoomAsync()
        {
            if (currentRoom == null) return;
            
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(currentRoom.Id, NetworkStateManager.Instance.CurrentUserId);
                
                currentRoom = null;
                NetworkStateManager.Instance.ClearLobby();
                NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "룸 나가기");
                
                StopHeartbeat();
            }
            catch (Exception ex)
            {
                NetworkStateManager.Instance.SetError($"룸 나가기 실패: {ex.Message}");
            }
        }
        /// <summary>
        /// 조용히 현재 로비에서 나가기 (오류 무시)
        /// </summary>
        private async Task LeaveCurrentLobbyQuietly()
        {
            try
            {
                if (currentRoom != null && !string.IsNullOrEmpty(NetworkStateManager.Instance.CurrentUserId))
                {
                    await LobbyService.Instance.RemovePlayerAsync(currentRoom.Id,
                        NetworkStateManager.Instance.CurrentUserId);
                }
            }
            finally
            {
                currentRoom = null;
                NetworkStateManager.Instance.ClearLobby();
                StopHeartbeat();
            }
        }

        /// <summary>
        /// 특정 플레이어를 Lobby에서 제거 (호스트 전용)
        /// </summary>
        public async Task<bool> RemovePlayerFromLobbyAsync(string playerId)
        {
            if (currentRoom == null)
            {
                LogManager.LogWarning(LogCategory.Lobby, "현재 로비가 없습니다.", this);
                return false;
            }

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(currentRoom.Id, playerId);
                LogManager.Log(LogCategory.Lobby, $"플레이어 {playerId}를 로비에서 제거했습니다.", this);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Lobby, $"플레이어 제거 실패: {ex.Message}", this);
                NetworkStateManager.Instance.SetError($"플레이어 제거 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 특정 룸의 최신 정보 조회
        /// </summary>
        public async Task<RoomInfo> GetRoomInfoAsync(string roomId)
        {
            try
            {
                var lobby = await LobbyService.Instance.GetLobbyAsync(roomId);
                return new RoomInfo
                {
                    roomId = lobby.Id,
                    roomName = lobby.Name,
                    currentPlayers = lobby.Players.Count,
                    maxPlayers = lobby.MaxPlayers,
                    gameType = lobby.Data?.GetValueOrDefault("gameType")?.Value ?? "default",
                    isPrivate = lobby.IsPrivate,
                    joinCode = lobby.Data?.GetValueOrDefault("joinCode")?.Value,
                    isJoinable = bool.TryParse(lobby.Data?.GetValueOrDefault("isJoinable")?.Value, out bool joinable) && joinable
                };
            }
            catch (Exception ex)
            {
                NetworkStateManager.Instance.SetError($"룸 정보 조회 실패: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// JoinCode로 로비 정보 조회 (참가하지 않고 정보만 가져오기)
        /// </summary>
        public async Task<RoomInfo> GetRoomInfoByJoinCodeAsync(string joinCode)
        {
            try
            {
                // JoinCode로 로비 정보 조회 (참가하지 않음)
                var lobby = await LobbyService.Instance.GetLobbyAsync(joinCode);
        
                if (lobby == null)
                {
                    LogManager.LogWarning(LogCategory.Lobby, "JoinCode로 로비를 찾을 수 없습니다.", this);
                    return null;
                }


                return new RoomInfo
                {
                    roomId = lobby.Id,
                    roomName = lobby.Name,
                    currentPlayers = lobby.Players.Count,
                    maxPlayers = lobby.MaxPlayers,
                    gameType = lobby.Data?.GetValueOrDefault("gameType")?.Value ?? "default",
                    isPrivate = lobby.IsPrivate,
                    joinCode = lobby.Data?.GetValueOrDefault("joinCode")?.Value,
                    isJoinable = bool.TryParse(lobby.Data?.GetValueOrDefault("isJoinable")?.Value, out bool joinable) && joinable
                };
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Lobby, $"JoinCode로 로비 정보 조회 실패: {ex.Message}", this);
                return null;
            }
        }
        
        private void StartHeartbeatIfOwner()
        {
            StopHeartbeat();
            
            string currentUserId = NetworkStateManager.Instance.CurrentUserId;
            string hostId = currentRoom?.HostId;
            
            LogManager.Log(LogCategory.Lobby, $"하트비트 시작 검사 - 현재 사용자 ID: {currentUserId}, 로비 호스트 ID: {hostId}");
            
            if (currentRoom != null && hostId == currentUserId)
            {
                LogManager.Log(LogCategory.Lobby, "호스트 확인 완료 → 하트비트 시작");
                heartbeatCts = new CancellationTokenSource();
                _ = HeartbeatLoop(heartbeatCts.Token);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Lobby, $"호스트가 아님 → 하트비트 시작하지 않음 (HostId: {hostId}, CurrentUserId: {currentUserId})");
            }
        }

        private void StopHeartbeat()
        {
            if (heartbeatCts != null)
            {
                heartbeatCts.Cancel();
                heartbeatCts.Dispose();
                heartbeatCts = null;
            }
        }

        private async Task HeartbeatLoop(CancellationToken token)
        {
            int heartbeatCount = 0;
            LogManager.Log(LogCategory.Lobby, $"하트비트 시작 - Lobby ID: {currentRoom?.Id}");
            
            while (!token.IsCancellationRequested && currentRoom != null)
            {
                heartbeatCount++;
                LogManager.Log(LogCategory.Lobby, $"하트비트 {heartbeatCount}번째 전송 시도 - Lobby: {currentRoom.Name}");
                
                try
                {
                    await LobbyService.Instance.SendHeartbeatPingAsync(currentRoom.Id);
                    LogManager.Log(LogCategory.Lobby, $"하트비트 {heartbeatCount}번째 전송 성공 ✓");
                }
                catch (Exception ex)
                {
                    LogManager.LogError(LogCategory.Lobby, $"하트비트 {heartbeatCount}번째 전송 실패: {ex.Message}");
                    NetworkStateManager.Instance.SetError($"하트비트 실패: {ex.Message}");
                }

                try 
                { 
                    LogManager.Log(LogCategory.Lobby, $"하트비트 다음 전송까지 15초 대기...");
                    await Task.Delay(TimeSpan.FromSeconds(15), token); 
                }
                catch (TaskCanceledException) 
                {
                    LogManager.Log(LogCategory.Lobby, $"하트비트 종료됨 (총 {heartbeatCount}회 전송)");
                }
            }
            
            LogManager.Log(LogCategory.Lobby, $"하트비트 루프 종료 - 총 {heartbeatCount}회 전송 완료");
        }
    }
}