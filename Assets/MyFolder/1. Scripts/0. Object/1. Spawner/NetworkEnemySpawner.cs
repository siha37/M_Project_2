using System;
using System.Collections;
using FishNet;
using FishNet.Managing;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._0._Object._1._Spawner
{
    public class NetworkEnemySpawner : NetworkBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private float spawnInterval = 1f; // 스폰 간격
        [SerializeField] private float spawnDelay; // 초기 스폰 지연 시간
        [SerializeField] private int maxSpawnCount = 5; // 이 스포너가 생성할 최대 적 수량
        [SerializeField] private bool enableDebugLogs = false; // 디버그 로그 활성화
        [SerializeField] private int OneTimeSpawnAmount = 5;
    
        [SerializeField] private float SpawnRadius = 5f;

        private Transform Container;

        private int area = 1 << 0;
        
        [SerializeField] private int currentSpawnedCount;
        private Coroutine spawnCoroutine;

        private ushort targetEnemyDataId => SpawnerManager.instance.EnemyLevel;
        
        
        private bool isPaused;
    
        public void Initialize(SpawnerData data)
        {
            
            Container = GameObject.FindGameObjectWithTag("EnemyContainer").transform;
            if (data != null)
            {
                spawnInterval = data.spawnInterval;
                spawnDelay = data.spawnDelay;
                maxSpawnCount = data.maxSpawnCount;
                OneTimeSpawnAmount = data.oneTimeSpawnAmount;
            }
            
            if (!enemyPrefab)
            {
                LogError("스폰할 프리팹이 설정되지 않았습니다.");
                enabled = false;
                return;
            }

            // Enemy 프리팹에 NetworkObject가 있는지 확인
            var enemyNetworkObject = enemyPrefab.GetComponent<NetworkObject>();
            if (!enemyNetworkObject)
            {
                LogError("Enemy 프리팹에 NetworkObject 컴포넌트가 없습니다. 네트워크 스폰이 불가능합니다.");
                enabled = false;
                return;
            }
            
            // NetworkEnemyManager 대기 및 등록
            StartCoroutine(WaitForNetworkEnemyManagerAndRegister());
        
            Log("서버에서 네트워크 스포너 시작");
        }

        public override void OnStartClient()
        {
            if (!IsServerInitialized)
            {
                // 클라이언트에서는 스폰 로직 비활성화
                enabled = false;
                Log("클라이언트에서 스포너 비활성화");
            }
        }
        private IEnumerator WaitForNetworkEnemyManagerAndRegister()
        {
            // NetworkEnemyManager 초기화 대기
            float waitTime = 0f;
            const float maxWaitTime = 10f; // 최대 10초 대기
        
            while (waitTime < maxWaitTime)
            {
                // NetworkEnemyManager 인스턴스 존재 확인
                if (NetworkEnemyManager.Instance)
                {
                    // NetworkEnemyManager가 서버로 완전히 초기화되었는지 확인
                    if (NetworkEnemyManager.Instance.IsServerInitialized)
                    {
                        Log("NetworkEnemyManager 서버 초기화 완료 - 연결 성공");
                        break;
                    }
                    else
                    {
                        Log("NetworkEnemyManager 인스턴스 존재하지만 서버 초기화 대기 중...");
                    }
                }
                else
                {
                    Log("NetworkEnemyManager 인스턴스 대기 중...");
                }
            
                yield return WaitForSecondsCache.Get(0.1f);
                waitTime += 0.1f;
            }
        
            if (!NetworkEnemyManager.Instance)
            {
                LogError("NetworkEnemyManager 초기화 타임아웃! 스포너를 비활성화합니다.");
                enabled = false;
                yield break;
            }
        
            if (!NetworkEnemyManager.Instance.IsServerInitialized)
            {
                LogError("NetworkEnemyManager가 서버로 초기화되지 않았습니다! 스포너를 비활성화합니다.");
                enabled = false;
                yield break;
            }

            // 안전하게 NetworkEnemyManager에 스포너 등록
            Log("NetworkEnemyManager 연결 완료 - 스포너 등록 시작");
            Log($"스포너 등록 전 상태 확인 - NetworkEnemyManager IsServer: {NetworkEnemyManager.Instance.IsServerInitialized}, IsNetworked: {NetworkEnemyManager.Instance.GetIsNetworked()}");
        
            NetworkEnemyManager.Instance.AddSpawner(maxSpawnCount);
        
            Log("스포너 등록 완료 - 스폰 코루틴 시작 준비");

            // 스폰 코루틴 시작
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private IEnumerator SpawnRoutine()
        {
            // 초기 지연 시간 대기
            if (spawnDelay > 0f)
            {
                Log($"초기 지연 대기: {spawnDelay}초");
                yield return WaitForSecondsCache.Get(spawnDelay);
            }

            while (true)
            {
                // 서버에서만 실행하고, 최대 스폰 수량 체크
                if (IsServerInitialized && CanSpawnMoreEnemies())
                {
                    int amount = Mathf.Clamp(maxSpawnCount - currentSpawnedCount, 0, OneTimeSpawnAmount);
                    for (int i = 0; i < amount; i++)
                    {
                        SpawnEnemy();
                    }
                }
                yield return WaitForSecondsCache.Get(spawnInterval);
            }
        }

        private bool CanSpawnMoreEnemies()
        {
            // 개별 스포너 제한 체크
            if (currentSpawnedCount >= maxSpawnCount)
            {
                Log($"개별 스포너 최대 수량 도달: {currentSpawnedCount}/{maxSpawnCount}");
                return false;
            }

            // 전역 적 수량 제한 체크
            bool canSpawn = NetworkEnemyManager.Instance.CanSpawnEnemy();
            if (!canSpawn)
            {
                Log($"전역 적 수량 한계 도달: {NetworkEnemyManager.Instance.CurrentEnemyCount}" +
                    $"/{NetworkEnemyManager.Instance.MaxEnemyCount}");
            }
        
            return canSpawn;
        }

        private void SpawnEnemy()
        {
            if (!IsServerInitialized) return;

            
            Log("적 오브젝트 생성");
            // ✅ FishNet 올바른 방식: Instantiate 후 NetworkManager를 통해 스폰
            TryRandomPointInCircleXY(
                transform.position,
                SpawnRadius,
                out Vector3 point,
                0,
                30,
                area,
                2f);
            
            NetworkManager networkManager = InstanceFinder.NetworkManager;
            if (!networkManager || !networkManager.ServerManager)
            {
                LogError("NetworkManager 또는 ServerManager를 찾을 수 없습니다.");
                return;
            }

            // ✅ FishNet 내장 풀링 사용
            // GetPooledInstantiated: 풀에서 가져오거나 없으면 새로 생성
            NetworkObject networkObject = networkManager.GetPooledInstantiated(enemyPrefab,point,Quaternion.identity, Container, true);
            
            GameObject enemy = networkObject.gameObject;
        
            enemy.TryGetComponent(out EnemyControll controller);
            controller.SetNetworkSpawnerObject(this);
            
            // ✅ Spawn 이후에 데이터 설정 (OnStartServer가 이미 실행된 상태)
            if(enemy.TryGetComponent(out EnemyStatus status))
            {
                status.SetDataId(targetEnemyDataId);
            }
            
            enemy.SetActive(true);
            
            // ✅ FishNet 스폰 (DespawnType=Pool이면 자동 풀링)
            networkManager.ServerManager.Spawn(networkObject);
            
            
            currentSpawnedCount++;
            NetworkEnemyManager.Instance.AddEnemy();
        }
        
        /// XY 평면(2D) 원형 범위 안에서 NavMesh 위 임의 위치 찾기
        public bool TryRandomPointInCircleXY(
            Vector2 centerXY, float radius,
            out Vector3 result,           // 반환은 Vector3( z = zPlane 고정 )
            float zPlane = 0f,
            int maxAttempts = 30,
            int areaMask = NavMesh.AllAreas,
            float sampleMaxDistance = 2f)
        {
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector2 r = Random.insideUnitCircle * radius;
                var probe = new Vector3(centerXY.x + r.x, 0,centerXY.y + r.y);

                try
                {
                    NavMeshHit hit;
                    if (NavMesh.SamplePosition(probe, out hit, sampleMaxDistance, areaMask))
                    {
                        // NavMeshPlus는 XY 상에서 길을 찾지만 좌표 타입은 여전히 Vector3
                        result = new Vector3(hit.position.x, hit.position.y, zPlane);
                        return true;
                    }
                }
                catch (System.Exception e)
                {
                    LogError($"NavMesh.SamplePosition 에러: {e.Message}");
                }
            }
            
            result = new Vector3(centerXY.x, centerXY.y, zPlane);
            return false;
        }

        // ✅ 적이 죽었을 때 호출될 메서드 (EnemyController에서 호출)
        public void OnEnemyDestroyed(Vector3 position)
        {
            if (IsServerInitialized)
            {
                currentSpawnedCount = Mathf.Max(0, currentSpawnedCount - 1);
                NetworkEnemyManager.Instance.RemoveEnemy(position);
                Log($"적 제거됨: {currentSpawnedCount}/{maxSpawnCount}");
            }
        }



        public void RemoveSpawner()
        {
            // 스폰 코루틴 정리
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }
            
            // NetworkEnemyManager에서 스포너 제거
            if (NetworkEnemyManager.Instance)
            {
                NetworkEnemyManager.Instance.RemoveSpawner(maxSpawnCount);
                Log("서버에서 스포너 정리 완료");
            }

            // 스포너 메니저 삭제처리
            if (SpawnerManager.instance)
            {
                SpawnerManager.instance.RemoveSpawner(this);
            }
        }
        

        [ContextMenu("Reset Spawn Count")]
        public void ResetSpawnCount()
        {
            if (IsServerInitialized)
            {
                currentSpawnedCount = 0;
                Log("스폰 카운트 리셋 완료");
            }
        }

        // ✅ 로깅 헬퍼 메서드
        private void Log(string message)
        {
            if (enableDebugLogs)
            {
                LogManager.Log(LogCategory.Spawner, $"NetworkSpawnerObject - {gameObject.name} {message}", this);
            }
        }

        private void LogError(string message)
        {
            LogManager.LogError(LogCategory.Spawner, $"NetworkSpawnerObject - {gameObject.name} {message}", this);
        }

        // ✅ 네트워크 상태 안전 체크 헬퍼
        private bool IsNetworkServerSafe()
        {
            try
            {
                // NetworkBehaviour가 초기화된 상태에서만 IsServer 체크
                return Application.isPlaying && GetIsNetworked() && IsServerInitialized;
            }
            catch
            {
                // NetworkBehaviour가 초기화되지 않은 상태
                return false;
            }
        }

        private bool IsNetworkClientSafe()
        {
            try
            {
                // NetworkBehaviour가 초기화된 상태에서만 클라이언트 체크
                return Application.isPlaying && GetIsNetworked() && !IsServerInitialized;
            }
            catch
            {
                // NetworkBehaviour가 초기화되지 않은 상태
                return false;
            }
        }

        private void OnDestroy()
        {
            RemoveSpawner();
            
        }

#if UNITY_EDITOR
        // ✅ 기즈모로 시각화 (안전한 네트워크 상태 체크)
        private void OnDrawGizmos()
        {
            // 안전한 네트워크 상태 체크
            if (IsNetworkServerSafe())
            {
                Gizmos.color = Color.green; // 서버에서는 녹색
            }
            else if (IsNetworkClientSafe())
            {
                Gizmos.color = Color.red; // 클라이언트에서는 빨간색
            }
            else
            {
                Gizmos.color = Color.yellow; // 에디터 또는 초기화 전에는 노란색
            }
        
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawWireSphere(transform.position, SpawnRadius);
        
            // 서버/클라이언트 표시 (안전한 상태에서만)
            if (Application.isPlaying)
            {
                try
                {
                    if (GetIsNetworked() && IsServerInitialized)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.3f);
                    }
                    else if (GetIsNetworked())
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.3f);
                    }
                }
                catch
                {
                    // NetworkBehaviour 초기화 전에는 기본 표시
                    Gizmos.color = Color.gray;
                    Gizmos.DrawWireCube(transform.position + Vector3.up * 1f, Vector3.one * 0.2f);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // 스폰 범위 표시 (필요시)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 2f);
        
            // 정보 텍스트 (에디터에서만)
            if (!Application.isPlaying)
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                    $"Max: {maxSpawnCount}\nInterval: {spawnInterval}s\nDelay: {spawnDelay}s");
            }
            else
            {
                string statusText;
            
                try
                {
                    if (GetIsNetworked() && IsServerInitialized)
                    {
                        statusText = $"서버 모드\nSpawned: {currentSpawnedCount}/{maxSpawnCount}";
                    }
                    else if (GetIsNetworked())
                    {
                        statusText = $"클라이언트 모드\n스폰 비활성화";
                    }
                    else
                    {
                        statusText = "로컬 모드\n네트워크 없음";
                    }
                }
                catch
                {
                    statusText = "NetworkBehaviour\n초기화 대기 중...";
                }
            
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, statusText);
            }
        }
#endif
    }
} 