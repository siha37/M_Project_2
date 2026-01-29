using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.UTP;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._9._Vivox;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyFolder._1._Scripts._4._Network
{
    /// <summary>
    /// Relay + FishNet 통합 관리자
    /// </summary>
    public class GameNetworkManager : SingleTone<GameNetworkManager>
    {
        [Header("FishNet 컴포넌트")]
        [SerializeField] private NetworkManager fishNetManager;
        [SerializeField] private UnityTransport unityTransport;
        
        public bool IsFishNetConnected => fishNetManager && 
            (fishNetManager.ServerManager.Started || fishNetManager.ClientManager.Started);
            
        public event Action<bool> OnFishNetConnectionChanged;
        
        protected override void Awake()
        {
            base.Awake();
            InitializeFishNet();
        }
        
        private void InitializeFishNet()
        {
            if (!fishNetManager)
                fishNetManager = FindFirstObjectByType<NetworkManager>();
                
            if (!unityTransport && fishNetManager)
                unityTransport = fishNetManager.GetComponent<UnityTransport>();
                
            if (fishNetManager)
            {
                fishNetManager.ServerManager.OnServerConnectionState += OnServerStateChanged;
                fishNetManager.ClientManager.OnClientConnectionState += OnClientStateChanged;
            }
        }
        
        /// <summary>
        /// 호스트로 게임 시작 (Relay 할당 + FishNet 호스트)
        /// </summary>
        public async Task<(bool success, string joinCode)> StartHostAsync(int maxPlayers)
        {
            try
            {
                // Unity Services 인증 상태 확인
                if (!AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.LogError("Unity Authentication이 완료되지 않았습니다.");
                    return (false, null);
                }

                Debug.Log($"Relay 할당 시작 - 최대 플레이어: {maxPlayers}");
        
                // Relay 할당
                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1); // 호스트 제외
        
                if (allocation == null)
                {
                    Debug.LogError("Relay allocation이 null입니다.");
                    return (false, null);
                }
        
                var joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        
                Debug.Log($"Relay 할당 성공 - Join Code: {joinCode}");
        
                // FishNet 설정
                unityTransport.SetRelayServerData(
                    allocation.RelayServer.IpV4, 
                    (ushort)allocation.RelayServer.Port,
                    allocation.AllocationIdBytes, 
                    allocation.Key, 
                    allocation.ConnectionData);
            
                // 호스트 시작
                fishNetManager.ServerManager.StartConnection();
                fishNetManager.ClientManager.StartConnection(); 
        
                return (true, joinCode);
            }
            catch (Exception ex)
            {
                Debug.LogError($"StartHostAsync 실패: {ex.Message}\n{ex.StackTrace}");
                NetworkStateManager.Instance.SetError($"호스트 시작 실패: {ex.Message}");
                return (false, null);
            }
        }
        
        
        /// <summary>
        /// 클라이언트로 게임 참가 (Relay 연결 + FishNet 클라이언트)
        /// </summary>
        public async Task<bool> JoinGameAsync(string joinCode)
        {
            try
            {
                
                // Relay 연결
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                
                // FishNet 설정
                unityTransport.SetRelayServerData(
                    joinAllocation.RelayServer.IpV4, 
                    (ushort)joinAllocation.RelayServer.Port,
                    joinAllocation.AllocationIdBytes, 
                    joinAllocation.Key, 
                    joinAllocation.ConnectionData, 
                    joinAllocation.HostConnectionData);
                    
                // 클라이언트 연결
                fishNetManager.ClientManager.StartConnection();
                
                NetworkStateManager.Instance.ChangeState(NetworkState.InLobby, "게임 참가");
                
                return true;
            }
            catch (RelayServiceException relayEx)
            {
                // Relay 관련 오류 상세 처리
                string errorMsg = relayEx.Reason switch
                {
                    RelayExceptionReason.JoinCodeNotFound => "방을 찾을 수 없습니다. 호스트가 방을 나갔거나 코드가 만료되었습니다.",
                    RelayExceptionReason.AllocationNotFound => "방 연결 정보를 찾을 수 없습니다.",
                    RelayExceptionReason.InvalidRequest => "잘못된 방 코드입니다.",
                    _ => $"Relay 연결 실패: {relayEx.Message}"
                };
                
                LogManager.LogError(LogCategory.Network, $"Relay 오류: {errorMsg} (Reason: {relayEx.Reason})", this);
                NetworkStateManager.Instance.SetError(errorMsg);
                return false;
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"게임 참가 중 예외 발생: {ex.Message}\n{ex.StackTrace}", this);
                NetworkStateManager.Instance.SetError($"게임 참가 실패: {ex.Message}");
                return false;
            }
        }
        
        private void OnServerStateChanged(ServerConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.InLobby, "호스트 연결 완료");
                OnFishNetConnectionChanged?.Invoke(true);
            }
        }
        
        private void OnClientStateChanged(ClientConnectionStateArgs args)
        {
            if (args.ConnectionState == LocalConnectionState.Started)
            {
                NetworkStateManager.Instance.ChangeState(NetworkState.InLobby, "클라이언트 연결 완료");
                OnFishNetConnectionChanged?.Invoke(true);
            }
            else if (args.ConnectionState == LocalConnectionState.Stopped)
            {
                // 클라이언트 연결이 끊겼을 때 (추방 또는 호스트 종료)
                LogManager.LogWarning(LogCategory.Network, "클라이언트 연결이 종료되었습니다. Title 씬으로 이동합니다.");
                OnFishNetConnectionChanged?.Invoke(false);
                
                // Lobby에서도 나가고 Title 씬으로 이동
                if (RoomManager.Instance != null)
                {
                    _ = HandleClientDisconnection();
                }
            }
        }
        
        /// <summary>
        /// 클라이언트 연결 종료 처리 (추방 또는 호스트 종료 시)
        /// </summary>
        private async Task HandleClientDisconnection()
        {
            // Lobby에서 나가기
            if (RoomManager.Instance)
            {
                await RoomManager.Instance.LeaveRoomAsync();
            }
            
            // 2. FishNet 연결 해제
            DisconnectFishNet();
            
            // 3. Vivox 연결 해제
            VivoxManager.Instance.LeaveEchoChannelAsync();
            
            // 상태 업데이트
            NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "연결 종료");
            
            // Title 씬으로 전환 (코루틴 대신 직접 정리)
            CleanupNetworkObjectsAndLoadScene("Title");
        }
        
        /// <summary>
        /// FishNet 연결만 해제 (내부용)
        /// </summary>
        public void DisconnectFishNet()
        {
            if (fishNetManager.ServerManager.Started)
                fishNetManager.ServerManager.StopConnection(true);
            if (fishNetManager.ClientManager.Started)
                fishNetManager.ClientManager.StopConnection();
                
            OnFishNetConnectionChanged?.Invoke(false);
        }

        /// <summary>
        /// 전체 연결 해제 (Lobby + FishNet + 씬 전환)
        /// </summary>
        public async void Disconnect()
        {
            // 1. Lobby에서 먼저 나가기 (await)
            if (RoomManager.Instance)
            {
                await RoomManager.Instance.LeaveRoomAsync();
            }

            // 2. FishNet 연결 해제
            DisconnectFishNet();
            
            // 3. Vivox 연결 해제
            VivoxManager.Instance.LeaveEchoChannelAsync();
            
            // 4. 상태 업데이트
            NetworkStateManager.Instance.ChangeState(NetworkState.Connected, "게임 연결 해제");

            // 5. 씬 전환 (네트워크 오브젝트 정리 포함)
            CleanupNetworkObjectsAndLoadScene("Title");
        }
        
        /// <summary>
        /// 네트워크 오브젝트 정리 후 씬 로드
        /// </summary>
        private void CleanupNetworkObjectsAndLoadScene(string sceneName)
        {
            LogManager.Log(LogCategory.Network, $"네트워크 오브젝트 정리 시작 - 타겟 씬: {sceneName}", this);
            
            // ✅ NetworkTransform 정리 (Transform 에러 방지)
            var networkTransforms = FindObjectsByType<FishNet.Component.Transforming.NetworkTransform>(FindObjectsSortMode.None);
            foreach (var nt in networkTransforms)
            {
                if (nt != null && nt.enabled)
                {
                    nt.enabled = false;
                }
            }
            
            LogManager.Log(LogCategory.Network, $"NetworkTransform 비활성화 완료: {networkTransforms.Length}개", this);
            
            // ✅ 씬 로드
            SceneManager.LoadSceneAsync(sceneName);
        }

        public bool IsHost()
        {
            return fishNetManager.ServerManager.Started;
        }

        public bool IsClient()
        {
            return fishNetManager.ClientManager.Started;
        }

        /// <summary>
        /// 호스트가 클라이언트를 추방합니다.
        /// </summary>
        /// <param name="clientId">추방할 클라이언트 ID</param>
        public async void KickClient(int clientId)
        {
            if (!IsHost())
            {
                Debug.LogWarning("호스트만 클라이언트를 추방할 수 있습니다.");
                return;
            }

            // 1. PlayerSettingManager에서 playerId 가져오기
            var playerSettings = PlayerSettingManager.Instance?.GetPlayerSettings(clientId);
            if (playerSettings == null)
            {
                Debug.LogWarning($"ClientId {clientId}의 플레이어 설정을 찾을 수 없습니다.");
                return;
            }

            // 2. Unity Lobby에서 제거
            if (!string.IsNullOrEmpty(playerSettings.playerId))
            {
                await RoomManager.Instance.RemovePlayerFromLobbyAsync(playerSettings.playerId);
            }

            // 3. FishNet에서 연결 해제
            fishNetManager.ServerManager.Kick(clientId, 
                FishNet.Managing.Server.KickReason.UnusualActivity, 
                FishNet.Managing.Logging.LoggingType.Common, 
                $"ClientId {clientId} ({playerSettings.playerName})가 호스트에 의해 추방되었습니다.");
            
            //4. Vivox 연결 해제
            
        }
    }
}