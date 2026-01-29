using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    /// <summary>
    /// 적군들의 타겟 탐지를 중앙화하여 성능 최적화
    /// 모든 적군이 개별적으로 타겟을 찾는 대신 공유 시스템 사용
    /// </summary>
    public class EnemyTargetManager : MonoBehaviour
    {
        private static EnemyTargetManager _instance;
        public static EnemyTargetManager Instance
        {
            get
            {
                if (!_instance)
                {
                    GameObject go = new GameObject("EnemyTargetManager");
                    _instance = go.AddComponent<EnemyTargetManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private List<NetworkObject> cachedPlayers = new();
        private float lastPlayerUpdateTime;
        private float playerUpdateInterval = 0.2f; // 플레이어 목록 업데이트 간격
        
        private Dictionary<Vector3, GameObject> closestPlayerCache = new();
        private float lastCacheUpdateTime;
        private float cacheUpdateInterval = 0.3f; // 캐시 업데이트 간격

        private void Awake()
        {
            if (!_instance)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            UpdatePlayerList();
            UpdateClosestPlayerCache();
        }

        /// <summary>
        /// 플레이어 목록 주기적 업데이트
        /// </summary>
        private void UpdatePlayerList()
        {
            if (Time.time - lastPlayerUpdateTime >= playerUpdateInterval)
            {
                if(NetworkPlayerManager.Instance)
                    cachedPlayers = NetworkPlayerManager.Instance.GetTargetAblePlayers();
                lastPlayerUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 가장 가까운 플레이어 캐시 업데이트
        /// </summary>
        private void UpdateClosestPlayerCache()
        {
            if (Time.time - lastCacheUpdateTime >= cacheUpdateInterval)
            {
                closestPlayerCache.Clear();
                lastCacheUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// 특정 위치에서 가장 가까운 플레이어 반환 (캐시 사용)
        /// </summary>
        public GameObject GetClosestPlayer(Vector3 position)
        {
            // 캐시된 결과가 있으면 반환
            Vector3 gridPosition = GetGridPosition(position, 5f); // 5유닛 그리드로 캐시
            if (closestPlayerCache.ContainsKey(gridPosition))
            {
                return closestPlayerCache[gridPosition];
            }

            // 캐시에 없으면 계산
            GameObject closestPlayer = FindClosestPlayer(position);
            closestPlayerCache[gridPosition] = closestPlayer;
            
            return closestPlayer;
        }

        /// <summary>
        /// 실제 가장 가까운 플레이어 탐색
        /// </summary>
        private GameObject FindClosestPlayer(Vector3 position)
        {
            if (cachedPlayers == null || cachedPlayers.Count == 0) return null;

            float minDistance = float.MaxValue;
            NetworkObject closestPlayer = null;

            foreach (var player in cachedPlayers)
            {
                if (!player) continue;

                float distance = Vector2.Distance(position, player.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestPlayer = player;
                }
            }

            return closestPlayer?.gameObject;
        }

        // 추가: 퀘스트 필터링 타겟 선택
        public GameObject GetClosestNonQuestingPlayer(Vector3 position)
        {
            var players = NetworkPlayerManager.Instance.GetTargetAblePlayersExcludingQuesting();
            return FindClosest(position, players);
        }

        public GameObject GetClosestQuestPlayer(Vector3 position, int questId)
        {
            var players = NetworkPlayerManager.Instance.GetQuestParticipants(questId);
            return FindClosest(position, players);
        }

        private GameObject FindClosest(Vector3 position, System.Collections.Generic.List<NetworkObject> players)
        {
            float minDistance = float.MaxValue;
            NetworkObject closest = null;
            foreach (var p in players)
            {
                if (!p) continue;
                float distance = Vector2.Distance(position, p.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = p;
                }
            }
            return closest ? closest.gameObject : null;
        }

        /// <summary>
        /// 위치를 그리드로 변환하여 캐시 키로 사용
        /// </summary>
        private Vector3 GetGridPosition(Vector3 position, float gridSize)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                Mathf.Round(position.y / gridSize) * gridSize,
                0f
            );
        }

        /// <summary>
        /// 특정 반경 내의 모든 플레이어 반환
        /// </summary>
        public List<GameObject> GetPlayersInRange(Vector3 position, float range)
        {
            List<GameObject> playersInRange = new List<GameObject>();

            foreach (var player in cachedPlayers)
            {
                if (player == null) continue;

                float distance = Vector2.Distance(position, player.transform.position);
                if (distance <= range)
                {
                    playersInRange.Add(player.gameObject);
                }
            }

            return playersInRange;
        }

        /// <summary>
        /// 캐시된 플레이어 목록 반환
        /// </summary>
        public List<NetworkObject> GetCachedPlayers()
        {
            return cachedPlayers;
        }
    }
}
