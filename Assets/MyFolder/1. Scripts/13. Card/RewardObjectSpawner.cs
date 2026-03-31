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

        [Header("보상 등급(게임 시간 비율)")]
        [Tooltip("진행도(0~1)가 이 값 미만이면 Normal 등급 카드 풀만 사용 (전체 길이는 TimeManager.EndTime)")]
        [SerializeField] private float epicProgressMin = 1f / 3f;

        [Tooltip("진행도(0~1)가 이 값 미만이면 Epic 등급 풀, 이상이면 Legend 풀")]
        [SerializeField] private float legendProgressMin = 2f / 3f;

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

            RewardCardRarity targetRarity = ResolveTargetRarityFromGameTimeProgress();
            RewardCardRarity rarity = RewardCardRarity.Normal;

            if (GameDataManager.Instance &&
                GameDataManager.Instance.TryPickRandomRewardCardIdByPreferredRarity(targetRarity, out var pickedId, out var resolved))
            {
                currentRewardId = pickedId;
                rarity = resolved;
            }
            else
            {
                ushort maxId = GameDataManager.Instance ? GameDataManager.Instance.GetMaxRewardCardId() : (ushort)1;
                if (maxId == 0) maxId = 1;
                currentRewardId = (ushort)Random.Range(1, maxId + 1);
                RewardCardData cardData = GameDataManager.Instance?.GetRewardCardsByType(currentRewardId);
                rarity = cardData?.rarity ?? RewardCardRarity.Normal;
            }

            var go = Instantiate(rewardObjectPrefab, transform.position, Quaternion.identity);
            ServerManager.Spawn(go);
            currentObject = go;

            if (RewardManager.Instance)
                RewardManager.Instance.RegisterSpawnedRewardObject(go, currentRewardId, rarity, this);

            LogManager.Log(LogCategory.System, $"[RewardObjectSpawner] 생성: rewardId={currentRewardId}, rarity={rarity}, targetRarity={targetRarity}, pos={transform.position}", this);
        }

        /// <summary>
        /// TimeManager의 경과 시간 / EndTime 으로 0~1 진행도를 만들고, 설정한 비율 구간에 맞는 목표 등급을 반환.
        /// TimeManager가 없으면 Normal.
        /// </summary>
        private RewardCardRarity ResolveTargetRarityFromGameTimeProgress()
        {
            if (!_8._Time.TimeManager.instance)
                return RewardCardRarity.Normal;

            float duration = Mathf.Max(1f, _8._Time.TimeManager.instance.EndTime);
            float t = Mathf.Clamp(_8._Time.TimeManager.instance.CurrentTime, 0f, duration);
            float p = t / duration;

            float epicAt = Mathf.Clamp01(epicProgressMin);
            float legendAt = Mathf.Clamp01(legendProgressMin);
            if (legendAt <= epicAt)
                legendAt = Mathf.Min(1f, epicAt + 0.01f);

            if (p < epicAt)
                return RewardCardRarity.Normal;
            if (p < legendAt)
                return RewardCardRarity.Epic;
            return RewardCardRarity.Legend;
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
