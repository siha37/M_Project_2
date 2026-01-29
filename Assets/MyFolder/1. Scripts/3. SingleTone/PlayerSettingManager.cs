using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._4._Network;
using MyFolder._1._Scripts._7._PlayerRole;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using Steamworks;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class PlayerSettingManager : NetworkBehaviour
    {
        public static PlayerSettingManager Instance { get; private set; }

        [Serializable]
        public class PlayerSettings
        {
            [FormerlySerializedAs("clinetId")] public int clientId;
            public PlayerRoleType role;
            public ushort playerDataId;
            public string playerName;
            public ulong steamId;
            public string playerId; // Unity Authentication PlayerId
            public bool isReady;
            
        }

        // 플레이어 설정 동기화 (SyncDictionary 사용)
        private readonly SyncDictionary<int, PlayerSettings> syncPlayerSettings = new SyncDictionary<int, PlayerSettings>();

        private List<string> skinName = new List<string>(){"tal/tal_1","tal/tal_2","tal/tal_3","tal/tal_4","tal/tal_5","tal/tal_6","tal/tal_7","tal/tal_8","tal/tal_9"};
        public List<string> SkinName => skinName;

        private readonly SyncVar<bool> isSettingsReady = new SyncVar<bool>(false);
        public bool IsSettingsReady => isSettingsReady.Value;
        
        // 플레이어 설정 변경 이벤트
        public static event Action<int> OnPlayerSettingsChanged;
        public static event Action<int> OnPlayerDisconnected;

        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }
        public override void OnStartServer()
        {
            // ✅ FishNet 기본 연결/해제 이벤트 직접 구독 (NetworkPlayerManager 제거)
            NetworkManager.ServerManager.OnRemoteConnectionState += OnClientConnectionStateChanged;
    
            // ✅ 이미 연결된 클라이언트들을 처리 (타이밍 문제 해결)
            StartCoroutine(ProcessExistingConnections());
            
            // ✅ 서버 준비 완료 플래그 설정
            isSettingsReady.Value = true;
    
            LogManager.Log(LogCategory.System, "PlayerSettingManager 서버 초기화 완료", this);
        }
        public override void OnStartClient()
        {
            LogManager.Log(LogCategory.System, "PlayerSettingManager 클라이언트 초기화 완료", this);
    
            // ✅ 초기 동기화 확인 코루틴 시작
            StartCoroutine(WaitForInitialSync());
        }
        public override void OnStopClient()
        {
            // 이벤트 정리 (필요시)
        }

        public void SkinSupple()
        {
            Random rand = new Random();
            skinName = skinName.OrderBy(_ => rand.Next()).ToList();
        }
        
        /// <summary>
        /// ✅ 이미 연결된 클라이언트들을 처리하는 코루틴
        /// </summary>
        private IEnumerator ProcessExistingConnections()
        {
            while (!NetworkManager?.ServerManager)
                yield return WaitForSecondsCache.Get(0.1f);
            
            if (NetworkManager?.ServerManager)
            {
                var existingConnections = NetworkManager.ServerManager.Clients;
                LogManager.Log(LogCategory.System, $"기존 연결된 클라이언트 수: {existingConnections.Count}명", this);
                
                foreach (var kvp in existingConnections)
                {
                    var connection = kvp.Value;
                    if (connection != null && connection.IsActive)
                    {
                        LogManager.Log(LogCategory.System, $"기존 클라이언트 처리: ClientId={connection.ClientId}", this);
                        OnClientConnected(connection);
                    }
                }
            }
        }

        public override void OnStopServer()
        {
            // ✅ 이벤트 구독 해제
            if (NetworkManager?.ServerManager)
            {
                NetworkManager.ServerManager.OnRemoteConnectionState -= OnClientConnectionStateChanged;
            }
        }

        /// <summary>
        /// ✅ FishNet 클라이언트 연결/해제 상태 변화 처리
        /// </summary>
        private void OnClientConnectionStateChanged(NetworkConnection connection,
            FishNet.Transporting.RemoteConnectionStateArgs args)
        {
            LogManager.Log(LogCategory.System, 
                $"클라이언트 연결 상태 변경: ClientId={connection.ClientId}, State={args.ConnectionState}", this);
                
            switch (args.ConnectionState)
            {
                case FishNet.Transporting.RemoteConnectionState.Started:
                    OnClientConnected(connection);
                    break;

                case FishNet.Transporting.RemoteConnectionState.Stopped:
                    OnClientDisconnected(connection);
                    break;
            }
        }
        private IEnumerator SetSteamInfoAfterConnection()
        {
            // 연결 완료까지 대기
            yield return new WaitUntil(() => Instance);
        
            if (SteamManager.Initialized)
            {
                ulong mySteamId = SteamUser.GetSteamID().m_SteamID;
                string mySteamName = SteamFriends.GetPersonaName(); // 스팀 계정 이름
            
                var clientManager = FishNet.InstanceFinder.ClientManager;
                if (clientManager?.Connection != null)
                {
                    int myClientId = clientManager.Connection.ClientId;
                
                    // 스팀 ID 설정
                    Instance.SetSteamId(myClientId, mySteamId);
                
                    // 스팀 계정 이름을 playerName으로 설정
                    Instance.SetPlayerNameServerRpc(myClientId, mySteamName);
                    
                    // Unity Authentication PlayerId 설정
                    string myPlayerId = NetworkStateManager.Instance.CurrentUserId;
                    if (!string.IsNullOrEmpty(myPlayerId))
                    {
                        Instance.SetPlayerIdServerRpc(myClientId, myPlayerId);
                    }
                }
            }
            else
            {
                string name = NetworkStateManager.Instance.CurrentUserId;
                var clientManager = FishNet.InstanceFinder.ClientManager;
                if (clientManager?.Connection != null)
                {
                    int myClientId = clientManager.Connection.ClientId;
                    Instance.SetPlayerNameServerRpc(myClientId, name);
                    if (!string.IsNullOrEmpty(name))
                    {
                        Instance.SetPlayerIdServerRpc(myClientId, name);
                    }
                }
            }
        }
        

        private void OnClientConnected(NetworkConnection connection)
        {
            int clientId = connection.ClientId;

            if (syncPlayerSettings.ContainsKey(clientId)) return;
            
            // ✅ 기본 설정만 생성 (역할은 기본값)
            var newSettings = new PlayerSettings
            {
                clientId = clientId,
                role = PlayerRoleType.Normal, // 기본값
                playerDataId = 1,
                isReady = false
            };

            syncPlayerSettings.Add(clientId, newSettings);

            LogManager.Log(LogCategory.System,
                $"플레이어 설정 추가: ClientId={clientId}, 총 플레이어={syncPlayerSettings.Count}명", this);
            
            // 호스트(서버)에서도 UI 업데이트를 위해 이벤트 발생
            NotifyPlayerSettingsChangedObserversRpc(clientId);
        }

        private void OnClientDisconnected(NetworkConnection connection)
        {
            int clientId = connection.ClientId;
            
            if (syncPlayerSettings.Remove(clientId))
            {
                LogManager.Log(LogCategory.System,
                    $"플레이어 설정 제거: ClientId={clientId}, 남은 플레이어={syncPlayerSettings.Count}명", this);
                ClientDisconnected(clientId);
            }
        }

        [ObserversRpc]
        public void ClientDisconnected(int ClientId)
        {
            OnPlayerDisconnected.Invoke(ClientId);
        }

        /// <summary>
        /// ✅ 초기 SyncDictionary 동기화 대기
        /// </summary>
        private IEnumerator WaitForInitialSync()
        {
            float timeout = 10f;
            float elapsed = 0f;
    
            LogManager.Log(LogCategory.System, "PlayerSettingManager SyncDictionary 초기 동기화 대기 시작", this);
    
            while (elapsed < timeout)
            {
                // 내 ClientId가 SyncDictionary에 있는지 확인
                var clientManager = FishNet.InstanceFinder.ClientManager;
                if (clientManager?.Connection != null)
                {
                    int myClientId = clientManager.Connection.ClientId;
                    if (syncPlayerSettings.ContainsKey(myClientId))
                    {
                        LogManager.Log(LogCategory.System, 
                            $"✅ PlayerSettingManager 초기 동기화 완료 - ClientId: {myClientId} (소요시간: {elapsed:F1}초)", this);
                
                        // 모든 기존 플레이어에 대해 UI 업데이트 이벤트 발생
                        foreach (var kvp in syncPlayerSettings)
                        {
                            OnPlayerSettingsChanged?.Invoke(kvp.Key);
                        }
                        
                        // Steam 정보 설정 코루틴 시작
                        StartCoroutine(nameof(SetSteamInfoAfterConnection));
                        yield break;
                    }
                }
        
                yield return WaitForSecondsCache.Get(0.1f);
                elapsed += 0.1f;
        
                // 1초마다 상태 로그
                if (elapsed % 1f < 0.1f)
                {
                    LogManager.LogWarning(LogCategory.System, 
                        $"PlayerSettingManager SyncDictionary 동기화 대기 중... (경과: {elapsed:F1}초, Count: {syncPlayerSettings.Count})", this);
                }
            }
    
            LogManager.LogError(LogCategory.System, 
                $"❌ PlayerSettingManager SyncDictionary 동기화 타임아웃 ({timeout}초)", this);
        }
        
        [ServerRpc(RequireOwnership = false)]
        public void SetSteamId(int clientId, ulong steamId)
        {
            if (syncPlayerSettings.TryGetValue(clientId, out var settings))
            {
                settings.steamId = steamId;
                syncPlayerSettings[clientId] = settings;
        
                LogManager.Log(LogCategory.System,
                    $"스팀 ID 설정: ClientId={clientId}, SteamId={steamId}", this);
        
                // 모든 클라이언트(호스트 포함)에 알림
                NotifyPlayerSettingsChangedObserversRpc(clientId);
            }
        }
        
        /// <summary>
        /// 모든 클라이언트에게 플레이어 설정 변경 알림
        /// </summary>
        [ObserversRpc]
        private void NotifyPlayerSettingsChangedObserversRpc(int clientId)
        {
            OnPlayerSettingsChanged?.Invoke(clientId);
        }
        
        #region Settor

        
        /// <summary>
        /// ✅ PlayerRoleManager에서 호출할 역할 업데이트 메서드
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerRoleServerRpc(int clientId, PlayerRoleType role)
        {
            if (syncPlayerSettings.TryGetValue(clientId, out var settings))
            {
                settings.role = role;
                syncPlayerSettings[clientId] = settings;

                LogManager.Log(LogCategory.System,
                    $"플레이어 역할 업데이트: ClientId={clientId}, Role={role}", this);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerDataIdServerRpc(int clientId, ushort playerDataId)
        {
            if (syncPlayerSettings.TryGetValue(clientId, out var settings))
            {
                settings.playerDataId = playerDataId;
                syncPlayerSettings[clientId] = settings;
                LogManager.Log(LogCategory.System,
                    $"플레이어 스탯 업데이트: ClientId={clientId}, Data={playerDataId}", this);
                
                // 모든 클라이언트(호스트 포함)에 알림
                NotifyPlayerSettingsChangedObserversRpc(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerNameServerRpc(int clientId, string playerName)
        {
            if(syncPlayerSettings.TryGetValue(clientId, out var settings))
            {
                settings.playerName = playerName;
                syncPlayerSettings[clientId] = settings;
                LogManager.Log(LogCategory.System,
                    $"플레이어 이름 업데이트: ClientId={clientId}, Name={playerName}", this);
                
                // 모든 클라이언트(호스트 포함)에 알림
                NotifyPlayerSettingsChangedObserversRpc(clientId);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void SetPlayerIdServerRpc(int clientId, string playerId)
        {
            if(syncPlayerSettings.TryGetValue(clientId, out var settings))
            {
                settings.playerId = playerId;
                syncPlayerSettings[clientId] = settings;
                LogManager.Log(LogCategory.System,
                    $"플레이어 ID 업데이트: ClientId={clientId}, PlayerId={playerId}", this);
            }
        }
        

        #endregion

        #region Gettor

        /// <summary>
        /// 특정 클라이언트 설정 가져오기
        /// </summary>
        public PlayerSettings GetPlayerSettings(int clientId)
        {
            return syncPlayerSettings.TryGetValue(clientId, out var settings) ? settings : null;
        }

        /// <summary>
        /// 현재 플레이어 설정 가져오기
        /// </summary>
        public PlayerSettings GetLocalPlayerSettings()
        {
            var clientManager = FishNet.InstanceFinder.ClientManager;
            if (clientManager?.Connection == null) 
            {
                LogManager.LogWarning(LogCategory.System, "❌ ClientManager 또는 Connection이 null", this);
                return null;
            }

            int myClientId = clientManager.Connection.ClientId;
    
            // ✅ 디버깅 로그 추가
            LogManager.Log(LogCategory.System, 
                $"🔍 GetLocalPlayerSettings 호출 - MyClientId: {myClientId}, " +
                $"SyncDictionary Count: {syncPlayerSettings.Count}, " +
                $"IsServer: {IsServerInitialized}, " +
                $"Keys: [{string.Join(", ", syncPlayerSettings.Keys)}]", this);

            var result = GetPlayerSettings(myClientId);
    
            if (result == null)
            {
                LogManager.LogWarning(LogCategory.System, 
                    $"❌ PlayerSettings를 찾을 수 없음 - ClientId: {myClientId}", this);
            }
            else
            {
                LogManager.Log(LogCategory.System, 
                    $"✅ PlayerSettings 찾음 - ClientId: {myClientId}, PlayerDataId: {result.playerDataId}", this);
            }

            return result;
        }

        /// <summary>
        /// 모든 플레이어 설정 가져오기
        /// </summary>
        public Dictionary<int, PlayerSettings> GetAllPlayerSettings()
        {
            var result = new Dictionary<int, PlayerSettings>();
            foreach (var kvp in syncPlayerSettings)
            {
                result.Add(kvp.Key, kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// 플레이어 수 가져오기
        /// </summary>
        public int GetPlayerCount()
        {
            return syncPlayerSettings.Count;
        }

        /// <summary>
        /// 강제 전체 동기화 요청 (새로 접속한 클라이언트용)
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestFullSyncServerRpc(NetworkConnection connection = null)
        {
            // SyncDictionary는 자동으로 새 클라이언트에게 전체 데이터를 전송
            LogManager.Log(LogCategory.System, $"전체 동기화 요청: ClientId={connection?.ClientId}", this);
        }

        #endregion
        
    }
}