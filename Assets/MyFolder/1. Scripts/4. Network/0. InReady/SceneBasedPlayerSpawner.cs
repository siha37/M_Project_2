using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Transporting;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._4._Network._0._InReady
{
    public class SceneBasedPlayerSpawner : NetworkBehaviour
    {
        [Header("플레이어 스폰 설정")]
        [SerializeField] private GameObject playerPrefab;
        [SerializeField] private GameObject spawnParent;
        [SerializeField] private Transform[] spawnPoints;
        
        [Header("씬 설정")]
        [SerializeField] private string[] targetSceneNames = { "Ready", "MainScene" };

        private NetworkManager networkManager;
        private Dictionary<NetworkConnection, NetworkObject> spawnedPlayers = new();
        private bool isInitialized = false;
        
        private void Start()
        {
            if (isInitialized) return;
            isInitialized = true;
            
            // ✅ 더 안전한 초기화 처리
            try
            {
                // ✅ 클라이언트는 자동으로 ServerRPC 호출
                if (InstanceFinder.NetworkManager && !InstanceFinder.NetworkManager.IsServerStarted)
                {
                    LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 클라이언트에서 자동 스폰 요청", this);
                    RequestPlayerSpawnServerRpc();
                }
                else
                {
                    // ✅ 서버는 기존 로직 유지
                    StartCoroutine(InitializeWithStateCheck());
                }
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner Start 오류: {ex.Message}", this);
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (networkManager)
                {
                    networkManager.ServerManager.OnRemoteConnectionState -= OnClientDisconnected;
                }
            
                // 안전하게 모든 플레이어 제거
                DespawnAllPlayers();
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner OnDestroy 오류: {ex.Message}", this);
            }
        }

        // ✅ 서버 초기화 (클라이언트는 ServerRPC로 처리)
        private IEnumerator InitializeWithStateCheck()
        {
            // NetworkManager 초기화 대기
            while (!InstanceFinder.NetworkManager)
            {
                yield return WaitForSecondsCache.Get(0.1f);
            }
            
            networkManager = InstanceFinder.NetworkManager;
            
            // ✅ NetworkManager 유효성 검사
            if (!networkManager)
            {
                LogManager.LogError(LogCategory.Network, "SceneBasedPlayerSpawner NetworkManager가 null입니다!", this);
                yield break;
            }
            
            LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner NetworkManager 확인 완료", this);
            
            // 연결 해제 이벤트만 등록
            if (networkManager.ServerManager)
            {
                networkManager.ServerManager.OnRemoteConnectionState += OnClientDisconnected;
            }

            LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 서버 초기화 완료", this);
            
            // ✅ 서버인 경우 즉시 기존 플레이어들 스폰 확인
            if (networkManager.IsServerStarted)
            {
                StartCoroutine(CheckAndSpawnExistingPlayersWithStateCheck());
            }
        }

        // ✅ 상태 확인 후 기존 플레이어들 스폰
        private IEnumerator CheckAndSpawnExistingPlayersWithStateCheck()
        {
            // 서버 상태와 씬 상태 확인 대기
            while (!networkManager.IsServerStarted || 
                   !IsInTargetScene())
            {
                yield return WaitForSecondsCache.Get(0.1f);
            }
            
            // 추가 안전 대기 (1프레임)
            yield return new WaitForEndOfFrame();
            
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 네트워크 준비 완료 - 연결된 플레이어: {networkManager.ServerManager.Clients.Count}명", this);
            
            // ✅ 안전한 클라이언트 반복 처리
            var clientsToProcess = new List<NetworkConnection>();
            if (networkManager.ServerManager?.Clients != null)
            {
                foreach (var kvp in networkManager.ServerManager.Clients)
                {
                    if (kvp.Value != null && kvp.Value.IsValid)
                    {
                        clientsToProcess.Add(kvp.Value);
                    }
                }
            }
            
            // 모든 연결된 플레이어에 대해 스폰 확인
            foreach (var conn in clientsToProcess)
            {
                if (conn != null && conn.IsValid && !spawnedPlayers.ContainsKey(conn))
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 기존 플레이어 스폰: {conn.ClientId}", this);
                    SpawnPlayer(conn);
                    
                    // 스폰 간격 (1프레임씩)
                    yield return null;
                }
            }
            
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 모든 기존 플레이어 스폰 완료 - 총 {spawnedPlayers.Count}명", this);
        }


        private void OnClientDisconnected(NetworkConnection conn, RemoteConnectionStateArgs stateArgs)
        {
            try
            {
                if (stateArgs.ConnectionState == RemoteConnectionState.Stopped)
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 클라이언트 연결 해제: {conn.ClientId}", this);
                
                    if (spawnedPlayers.ContainsKey(conn))
                    {
                        NetworkObject player = spawnedPlayers[conn];
                        
                        // ✅ 1. 먼저 FishNet에서 디스폰
                        if (player != null && player.IsSpawned)
                        {
                            networkManager.ServerManager.Despawn(player);
                            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 플레이어 디스폰 완료: {conn.ClientId}", this);
                        }
                        
                        // ✅ 2. 그 다음 NetworkPlayerManager에서 해제
                        if (NetworkPlayerManager.Instance != null && player != null)
                        {
                            NetworkPlayerManager.Instance.UnregisterPlayer(player);
                        }
                    
                        spawnedPlayers.Remove(conn);
                        
                        // ✅ 3. GameSessionManager에 플레이어 수 업데이트 요청
                        if (GameSessionManager.Instance != null)
                        {
                            GameSessionManager.Instance.UpdatePlayerCountServerRpc();
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner OnClientDisconnected 오류: {ex.Message}", this);
            }
        }

        private void SpawnPlayer(NetworkConnection conn)
        {
            try
            {
                // ✅ NetworkManager 유효성 검사
                if (!networkManager)
                {
                    LogManager.LogError(LogCategory.Network, "SceneBasedPlayerSpawner NetworkManager가 null입니다!", this);
                    return;
                }
                
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner SpawnPlayer 호출 - ClientID: {conn.ClientId}, IsServer: {networkManager.IsServerStarted}", this);

                if (!playerPrefab)
                {
                    LogManager.LogError(LogCategory.Network, "SceneBasedPlayerSpawner Player Prefab이 설정되지 않았습니다!", this);
                    return;
                }

                // 서버에서만 플레이어 생성 및 스폰
                if (!networkManager.IsServerStarted)
                {
                    LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 서버가 아닌 클라이언트에서 스폰 시도 - ClientID: {conn.ClientId}", this);
                    return;
                }

                // 이미 스폰된 플레이어인지 한번 더 확인
                if (spawnedPlayers.ContainsKey(conn))
                {
                    LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 이미 스폰된 플레이어 - ClientID: {conn.ClientId}", this);
                    return;
                }

                Vector3 spawnPos = GetRandomSpawnPoint();
                
                // 플레이어 생성
                GameObject playerObj;
                if(spawnParent)
                    playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity, spawnParent.transform);
                else
                    playerObj = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
                playerObj.name = playerObj.name + $"_{spawnedPlayers.Count}";
                NetworkObject player = playerObj.GetComponent<NetworkObject>();
                
                if (!player)
                {
                    LogManager.LogError(LogCategory.Network, "SceneBasedPlayerSpawner Player Prefab에 NetworkObject 컴포넌트가 없습니다!", this);
                    Destroy(playerObj);
                    return;
                }
                
                // FishNet에 스폰
                networkManager.ServerManager.Spawn(player, conn);
                player.GiveOwnership(conn);
                
                // 로컬 딕셔너리에 저장
                spawnedPlayers[conn] = player;

                // ✅ NetworkPlayerManager에 등록
                if (NetworkPlayerManager.Instance)
                {
                    NetworkPlayerManager.Instance.RegisterPlayer(player);
                }

                LogManager.Log(LogCategory.Network, 
                    $"SceneBasedPlayerSpawner 플레이어 스폰 완료 - ClientID: {conn.ClientId}, Position: {spawnPos}, Owner: {player.Owner?.ClientId}, 총 스폰된 플레이어: {spawnedPlayers.Count}명", this);
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner SpawnPlayer 오류: {ex.Message}", this);
            }
        }

        private void DespawnAllPlayers()
        {
            try
            {
                if (!networkManager) return;

                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 모든 플레이어 제거 시작 - 총 {spawnedPlayers.Count}명", this);

                // ✅ ToList() 대신 직접 반복 (메모리 절약)
                var connectionsToRemove = new List<NetworkConnection>();
                
                foreach (var kvp in spawnedPlayers)
                {
                    NetworkConnection conn = kvp.Key;
                    NetworkObject player = kvp.Value;
                    
                    try
                    {
                        // ✅ 1. FishNet에서 디스폰
                        if (player != null && player.IsSpawned)
                        {
                            networkManager.ServerManager.Despawn(player);
                            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 플레이어 디스폰: {conn?.ClientId}", this);
                        }
                        
                        // ✅ 2. NetworkPlayerManager에서 해제
                        if (NetworkPlayerManager.Instance != null && player != null)
                        {
                            NetworkPlayerManager.Instance.UnregisterPlayer(player);
                        }
                        
                        connectionsToRemove.Add(conn);
                    }
                    catch (System.Exception ex)
                    {
                        LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner 플레이어 제거 중 오류: {ex.Message}", this);
                        // 오류가 발생해도 다음 플레이어 처리 계속
                    }
                }
                
                // ✅ 안전하게 제거
                foreach (var conn in connectionsToRemove)
                {
                    spawnedPlayers.Remove(conn);
                }
                
                LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 모든 플레이어 제거 완료", this);
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner DespawnAllPlayers 오류: {ex.Message}", this);
            }
        }

        private bool IsInTargetScene()
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            
            if (targetSceneNames == null || targetSceneNames.Length == 0)
            {
                LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 타겟 씬 이름이 설정되지 않았습니다!", this);
                return false;
            }
            
            foreach (string targetSceneName in targetSceneNames)
            {
                if (currentScene.Contains(targetSceneName))
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 현재 씬: {currentScene} -> 타겟 씬 '{targetSceneName}'과 일치", this);
                    return true;
                }
            }
            
            LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 현재 씬: {currentScene} -> 타겟 씬과 불일치 (타겟: {string.Join(", ", targetSceneNames)})", this);
            return false;
        }

        private Vector3 GetRandomSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 스폰 포인트 선택: {spawnPos}", this);
                return spawnPos;
            }

            LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 스폰 포인트가 없어 기본 위치 사용", this);
            return Vector3.zero;
        }

        // ✅ 상태 확인용 공개 메서드들
        public bool IsNetworkReady()
        {
            return networkManager && networkManager.IsServerStarted;
        }

        public int GetSpawnedPlayerCount()
        {
            return spawnedPlayers.Count;
        }

        public int GetConnectedPlayerCount()
        {
            return networkManager?.ServerManager?.Clients?.Count ?? 0;
        }

        // ✅ 클라이언트에서 서버로 플레이어 스폰 요청
        [ServerRpc(RequireOwnership = false)]
        private void RequestPlayerSpawnServerRpc(NetworkConnection conn = null)
        {
            try
            {
                // 연결 정보 가져오기
                if (conn == null)
                {
                    conn = InstanceFinder.NetworkManager.ClientManager.Connection;
                }
                
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 클라이언트 스폰 요청 받음 - ClientID: {conn.ClientId}", this);
                
                // 서버에서만 실행
                if (!InstanceFinder.NetworkManager.IsServerStarted)
                {
                    LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 서버가 아니므로 스폰 요청 무시", this);
                    return;
                }
                
                // 타겟 씬인지 확인
                if (!IsInTargetScene())
                {
                    LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 타겟 씬이 아니므로 스폰하지 않음", this);
                    return;
                }
                
                // 이미 스폰된 플레이어인지 확인
                if (spawnedPlayers.ContainsKey(conn))
                {
                    LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 이미 스폰된 플레이어: {conn.ClientId}", this);
                    return;
                }
                
                // 연결 유효성 확인
                if (!conn.IsValid)
                {
                    LogManager.LogWarning(LogCategory.Network, $"SceneBasedPlayerSpawner 유효하지 않은 연결: {conn.ClientId}", this);
                    return;
                }
                
                LogManager.Log(LogCategory.Network, $"SceneBasedPlayerSpawner 클라이언트 요청으로 플레이어 스폰: {conn.ClientId}", this);
                SpawnPlayer(conn);
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Network, $"SceneBasedPlayerSpawner RequestPlayerSpawnServerRpc 오류: {ex.Message}", this);
            }
        }

        // ✅ 수동 플레이어 스폰 확인 (디버그용)
        [ContextMenu("Check All Players")]
        public void CheckAllPlayersManually()
        {
            if (networkManager?.IsServerStarted == true)
            {
                StartCoroutine(CheckAndSpawnExistingPlayersWithStateCheck());
            }
            else
            {
                LogManager.LogWarning(LogCategory.Network, "SceneBasedPlayerSpawner 서버에서만 플레이어 확인이 가능합니다.", this);
            }
        }
        
        /// <summary>
        /// 씬 전환 전 모든 플레이어 강제 정리 (외부 호출용)
        /// </summary>
        public void ForceCleanupAllPlayers()
        {
            LogManager.Log(LogCategory.Network, "SceneBasedPlayerSpawner 강제 플레이어 정리 시작", this);
            DespawnAllPlayers();
        }
    }
} 