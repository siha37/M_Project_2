using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkPlayerManager : NetworkBehaviour
    {
        private readonly SyncVar<int> syncPlayerCount = new SyncVar<int>();
    
        private static NetworkPlayerManager instance;
        public static NetworkPlayerManager Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<NetworkPlayerManager>();
                }
                return instance;
            }
        }

        // 모든 클라이언트에서 접근 가능한 플레이어 리스트
        private List<NetworkObject> allPlayers = new List<NetworkObject>();

        public int PlayerCount => syncPlayerCount.Value;
        public List<NetworkObject> AllPlayers => new List<NetworkObject>(allPlayers);

        private void Awake()
        {
            if (!instance)
            {
                instance = this;
                LogManager.Log(LogCategory.Network, "NetworkPlayerManager 인스턴스 생성 완료", this);
            }
            else if (instance != this)
            {
                LogManager.LogWarning(LogCategory.Network, "NetworkPlayerManager 중복 인스턴스 제거", this);
                Destroy(gameObject);
            }
        }

        public override void OnStartServer()
        {
            syncPlayerCount.Value = 0;
            LogManager.Log(LogCategory.Network, "NetworkPlayerManager 서버 초기화 완료", this);
        }

        public override void OnStartClient()
        {
            syncPlayerCount.OnChange += OnPlayerCount_Changed;
            LogManager.Log(LogCategory.Network, "NetworkPlayerManager 클라이언트 초기화 완료", this);
            if (!IsServerInitialized)
            {
                RequestFullSyncServerRpc();
            }
        }

        public override void OnStopClient()
        {
                syncPlayerCount.OnChange -= OnPlayerCount_Changed;
        }

        // ✅ SceneBasedPlayerSpawner에서 호출할 등록 메서드
        public void RegisterPlayer(NetworkObject player)
        {
            if (!player) return;

            // 서버에서는 직접 등록하고 클라이언트에 알림
            if (IsServerInitialized)
            {
                RegisterPlayerInternal(player);
                NotifyPlayerAddedClientRpc(player);
            }
        }

        // ✅ SceneBasedPlayerSpawner에서 호출할 해제 메서드
        public void UnregisterPlayer(NetworkObject player)
        {
            if (!player) return;

            // 서버에서는 직접 해제하고 클라이언트에 알림
            if (IsServerInitialized)
            {
                UnregisterPlayerInternal(player);
                NotifyPlayerRemovedClientRpc(player);
            }
        }

        // 서버 내부 등록 처리
        private void RegisterPlayerInternal(NetworkObject player)
        {
            if (!allPlayers.Contains(player))
            {
                allPlayers.Add(player);
                syncPlayerCount.Value = allPlayers.Count;
            
                LogManager.Log(LogCategory.Network, 
                    $"NetworkPlayerManager 플레이어 등록: {player.Owner?.ClientId}, 총 플레이어: {allPlayers.Count}명", this);
            
                OnPlayerAdded?.Invoke(player);
            }
        }

        // 서버 내부 해제 처리
        private void UnregisterPlayerInternal(NetworkObject player)
        {
            if (allPlayers.Contains(player))
            {
                allPlayers.Remove(player);
                syncPlayerCount.Value = allPlayers.Count;
            
                LogManager.Log(LogCategory.Network, 
                    $"NetworkPlayerManager 플레이어 해제: {player.Owner?.ClientId}, 총 플레이어: {allPlayers.Count}명", this);
            
                OnPlayerRemoved?.Invoke(player);
            }
        }

        // 클라이언트에 플레이어 추가 알림
        [ObserversRpc(BufferLast = true)]
        private void NotifyPlayerAddedClientRpc(NetworkObject player)
        {
            if (IsServerInitialized) return; // 서버는 이미 처리했으므로 건너뜀
        
            if (player && !allPlayers.Contains(player))
            {
                allPlayers.Add(player);
                LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 클라이언트에 플레이어 추가: {player.name}", this);
                OnPlayerAdded?.Invoke(player);
            }
        }

        // 클라이언트에 플레이어 제거 알림
        [ObserversRpc(BufferLast = true)]
        private void NotifyPlayerRemovedClientRpc(NetworkObject player)
        {
            if (IsServerInitialized) return; // 서버는 이미 처리했으므로 건너뜀
        
            if (player && allPlayers.Contains(player))
            {
                allPlayers.Remove(player);
                LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 클라이언트에서 플레이어 제거: {player.name}", this);
                OnPlayerRemoved?.Invoke(player);
            }
        }

        // ✅ 초기 합류 클라이언트를 위한 전체 동기화 요청 (소유권 불필요)
        [ServerRpc(RequireOwnership = false)]
        private void RequestFullSyncServerRpc(NetworkConnection caller = null)
        {
            if (!IsServerInitialized)
                return;

            if (caller == null)
                return;

            SendFullSyncTargetRpc(caller, allPlayers.ToArray());
        }

        // ✅ 서버 → 특정 클라이언트로 전체 목록 전달
        [TargetRpc]
        private void SendFullSyncTargetRpc(NetworkConnection target, NetworkObject[] players)
        {
            if (IsServerInitialized)
                return; // 서버는 불필요

            allPlayers.Clear();
            if (players != null)
            {
                foreach (var p in players)
                {
                    if (p && !allPlayers.Contains(p))
                    {
                        allPlayers.Add(p);
                        OnPlayerAdded?.Invoke(p);
                    }
                }
            }

            LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 전체 동기화 수신: 총 {allPlayers.Count}명", this);
        }

        // ✅ 플레이어 조회 메서드들
        public NetworkObject GetPlayerByClientId(int clientId)
        {
            return allPlayers.Find(player => player.Owner != null && player.Owner.ClientId == clientId);
        }
        /// <summary>
        /// 현재 디바이스(클라이언트)의 오너 NetworkObject 반환
        /// </summary>
        public NetworkObject GetLocalOwnedPlayer()
        {
            if (!InstanceFinder.ClientManager)
                return null;

            int localClientId = InstanceFinder.ClientManager.Connection.ClientId;

            return GetPlayerByClientId(localClientId);
        }

        public List<NetworkObject> GetAllPlayers()
        {
            return allPlayers;
        }

        public List<NetworkObject> GetAlivePlayers()
        {
            List<NetworkObject> alivePlayers = new List<NetworkObject>();
            foreach (var player in allPlayers)
            {
                PlayerNetworkSync playerSync = player.GetComponent<PlayerNetworkSync>();
                if (playerSync && !playerSync.IsDead())
                {
                    alivePlayers.Add(player);
                }
            }
            return alivePlayers;
        }

        public List<NetworkObject> GetTargetAblePlayers()
        {
            List<NetworkObject> targetAblePlayers = new List<NetworkObject>();
            foreach (NetworkObject player in allPlayers)
            {
                PlayerNetworkSync playerSync = player.GetComponent<PlayerNetworkSync>();
                if (playerSync && !playerSync.IsDead() && playerSync.IsCanSee())
                {
                    targetAblePlayers.Add(player);
                }
            }
            return targetAblePlayers;
        }
        
        // 추가: 퀘스트 관련 필터들
        public List<NetworkObject> GetTargetAblePlayersExcludingQuesting()
        {
            List<NetworkObject> result = new List<NetworkObject>();
            foreach (NetworkObject player in allPlayers)
            {
                PlayerNetworkSync sync = player.GetComponent<PlayerNetworkSync>();
                if (sync && !sync.IsDead() && sync.IsCanSee() && !sync.IsQuesting())
                {
                    result.Add(player);
                }
            }
            return result;
        }

        public List<NetworkObject> GetQuestParticipants(int questId)
        {
            List<NetworkObject> result = new List<NetworkObject>();
            foreach (NetworkObject player in allPlayers)
            {
                PlayerNetworkSync sync = player.GetComponent<PlayerNetworkSync>();
                if (sync && !sync.IsDead() && sync.IsCanSee() && sync.IsQuesting() && sync.GetActiveQuestId() == questId)
                {
                    result.Add(player);
                }
            }
            return result;
        }
        public List<NetworkObject> GetDeadPlayers()
        {
            List<NetworkObject> deadPlayers = new List<NetworkObject>();
            foreach (var player in allPlayers)
            {
                PlayerNetworkSync playerSync = player.GetComponent<PlayerNetworkSync>();
                if (playerSync && playerSync.IsDead())
                {
                    deadPlayers.Add(player);
                }
            }
            return deadPlayers;
        }

        public List<NetworkObject> GetDestroyerPlayers()
        {
            List<NetworkObject> destroyer = new List<NetworkObject>();
            foreach (var player in allPlayers)
            {
                PlayerSettingManager.PlayerSettings setting = PlayerSettingManager.Instance.GetPlayerSettings(player.Owner.ClientId);
                if (setting != null && setting.role == PlayerRoleType.Destroyer)
                {
                    destroyer.Add(player);
                }
            }
            return destroyer;
            
        }
        

        // ✅ 이벤트 시스템
        public System.Action<NetworkObject> OnPlayerAdded;
        public System.Action<NetworkObject> OnPlayerRemoved;
        public System.Action<int> OnPlayerCountChanged;

        private void OnPlayerCount_Changed(int previousValue, int newValue, bool asServer)
        {
            LogManager.Log(LogCategory.Network, $"NetworkPlayerManager 플레이어 수 변경: {previousValue} → {newValue}", this);
            OnPlayerCountChanged?.Invoke(newValue);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
} 