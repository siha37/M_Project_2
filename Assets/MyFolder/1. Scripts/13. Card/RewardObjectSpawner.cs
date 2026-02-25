using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._13._Card
{
    /// <summary>
    /// 씬의 각 보상 포인트에 배치. 자기 Transform 위치에 RewardObject 1개를 생성·관리.
    /// 수집 횟수가 늘어날수록 재생성 주기가 단축 (오버워치 체력 아이템 방식).
    /// 서버에서만 동작.
    /// </summary>
    public class RewardObjectSpawner : NetworkBehaviour
    {
        [Header("스폰 설정")]
        [SerializeField] private NetworkObject rewardObjectPrefab;

        [Tooltip("초기 재생성 대기 시간(초)")]
        [SerializeField] private float baseSpawnInterval = 30f;

        [Tooltip("최소 재생성 대기 시간(초)")]
        [SerializeField] private float minSpawnInterval = 5f;

        [Tooltip("수집 1회당 대기 시간 단축량(초)")]
        [SerializeField] private float intervalReductionPerCollect = 3f;

        // ─── 서버 전용 상태 ──────────────────────────────────────
        private NetworkObject currentObject = null;
        private int collectCount;
        private ushort currentRewardId;
        private float respawnTimer = 5;
        private bool isWaitingRespawn = true;

        // ─── FishNet 생명주기 ─────────────────────────────────────
        public override void OnStartServer()
        {
            base.OnStartServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
        }

        private void Update()
        {
            if (!IsServerInitialized) return;
            if (currentObject) return;
            if (!isWaitingRespawn) return;

            respawnTimer -= Time.deltaTime;
            if (respawnTimer <= 0f)
            {
                isWaitingRespawn = false;
                SpawnObject();
            }
        }

        // ─── 생성 ─────────────────────────────────────────────────
        private void SpawnObject()
        {
            if (!IsServerInitialized) return;
            if (!rewardObjectPrefab) return;

            ushort maxId = GameDataManager.Instance ? GameDataManager.Instance.GetMaxRewardCardId() : (ushort)1;
            if (maxId == 0) maxId = 1;

            // 보상 ID 점진적 증가 (1 → maxId 순환)
            currentRewardId = (ushort)(currentRewardId % maxId + 1);

            // 데이터에서 Rarity 조회
            var cardData = GameDataManager.Instance?.GetRewardCardsByType(currentRewardId);
            RewardCardRarity rarity = cardData?.rarity ?? RewardCardRarity.Normal;

            var go = Instantiate(rewardObjectPrefab, transform.position, Quaternion.identity);
            ServerManager.Spawn(go);
            currentObject = go;

            if (RewardManager.Instance)
                RewardManager.Instance.RegisterSpawnedRewardObject(go, currentRewardId, rarity, this);

            LogManager.Log(LogCategory.System, $"[RewardObjectSpawner] 생성: rewardId={currentRewardId}, rarity={rarity}, pos={transform.position}", this);
        }

        // ─── 수집 콜백 (RewardManager에서 호출) ──────────────────
        public void OnObjectCollected()
        {
            collectCount++;
            currentObject = null;

            float interval = Mathf.Max(
                minSpawnInterval,
                baseSpawnInterval - intervalReductionPerCollect * collectCount);

            respawnTimer = interval;
            isWaitingRespawn = true;

            LogManager.Log(LogCategory.System,
                $"[RewardObjectSpawner] 수집됨. 다음 스폰까지 {interval:F1}s (수집횟수={collectCount})", this);
        }
    }
}
