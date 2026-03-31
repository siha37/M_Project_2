using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._4._Shooting;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using MyFolder._1._Scripts._1._UI._0._GameStage._2._Card;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using UnityEngine;

namespace MyFolder._1._Scripts._13._Card
{
    public class RewardManager : NetworkBehaviour
    {
        public static RewardManager Instance { get; private set; }

        // ─── 내부 상태 ───────────────────────────────────────────
        private class RewardObjectState
        {
            public int instanceId;
            public NetworkObject networkObject;
            public ushort rewardId;
            public RewardCardRarity rarity;
            public bool isCollected;
            public RewardObjectSpawner spawner;
        }

        private Dictionary<int, RewardObjectState> spawnedObjects = new();
        private Dictionary<int, Queue<List<RewardCardInstance>>> pendingPlayerCards = new();
        private int nextInstanceId = 1;

        // ─── 카드 타입 ───────────────────────────────────────────
        [Serializable]
        public class RewardCardInstance
        {
            public RewardCardData baseData;
            public StatType rewardType;
            public float actualPercentage;

            public RewardCardInstance() { }

            public RewardCardInstance(RewardCardData baseData, StatType rewardType, float actualPercentage)
            {
                this.baseData = baseData;
                this.rewardType = rewardType;
                this.actualPercentage = actualPercentage;
            }
        }

        private static readonly StatType[] rewardableStats =
        {
            StatType.Speed, StatType.Hp, StatType.Defence, StatType.BulletSpeed,
            StatType.BulletDamage, StatType.BulletSize, StatType.MagazineCapacity,
            StatType.ShotDelay, StatType.ReloadTime
        };

        [SerializeField] private CardRewardRecordBoard playerCardRecordBoard;

        // ─── 생명주기 ─────────────────────────────────────────────
        private void Awake()
        {
            Instance = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }

        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                pendingPlayerCards.Remove(conn.ClientId);
            }
        }

        // ─── 인스턴스 등록 (Spawner → RewardManager) ─────────────
        public void RegisterSpawnedRewardObject(
            NetworkObject rewardNetworkObject,
            ushort rewardId,
            RewardCardRarity rarity,
            RewardObjectSpawner spawner)
        {
            if (!IsServerInitialized) return;

            int instanceId = nextInstanceId++;

            var state = new RewardObjectState
            {
                instanceId = instanceId,
                networkObject = rewardNetworkObject,
                rewardId = rewardId,
                rarity = rarity,
                isCollected = false,
                spawner = spawner
            };
            spawnedObjects[instanceId] = state;

            if (rewardNetworkObject.TryGetComponent(out RewardObject rewardObject))
            {
                rewardObject.InitializeServer(instanceId, rewardId, rarity);
            }

            LogManager.Log(LogCategory.System,
                $"RewardObject 등록: instanceId={instanceId}, rewardId={rewardId}, rarity={rarity}", this);
        }

        public void UnregisterRewardObject(int instanceId)
        {
            spawnedObjects.Remove(instanceId);
        }

        // ─── 수집 요청 ServerRpc (클라 → 서버) ───────────────────
        [ServerRpc(RequireOwnership = false)]
        public void RequestCollectServerRpc(int instanceId, NetworkConnection sender = null)
        {
            if (sender == null) return;
            ValidateAndGrantReward(instanceId, sender);
        }

        private void ValidateAndGrantReward(int instanceId, NetworkConnection conn)
        {
            if (!spawnedObjects.TryGetValue(instanceId, out var state))
            {
                LogManager.LogWarning(LogCategory.System, $"유효하지 않은 instanceId: {instanceId}", this);
                return;
            }

            if (state.isCollected)
            {
                LogManager.LogWarning(LogCategory.System, $"이미 수집된 RewardObject: instanceId={instanceId}", this);
                return;
            }

            state.isCollected = true;
            spawnedObjects.Remove(instanceId);

            // 스포너 타이머 재시작
            state.spawner?.OnObjectCollected();

            // 오브젝트 제거
            if (state.networkObject)
                state.networkObject.Despawn();
            
            conn.FirstObject.TryGetComponent(out PlayerStatus status);
            ShootingData shootingData = status.GetShootingData;
            
            // 카드 생성 및 UI 표시
            var cards = GenerateRewardCards(state.rewardId, 3,shootingData.burstCount);
            if(!pendingPlayerCards.ContainsKey(conn.ClientId))
                pendingPlayerCards[conn.ClientId] = new Queue<List<RewardCardInstance>>();
            
            pendingPlayerCards[conn.ClientId].Enqueue(cards);
            
            ShowRewardCardsTargetRpc(conn, cards);

            LogManager.Log(LogCategory.System, $"플레이어 {conn.ClientId}가 RewardObject 수집 완료 (instanceId={instanceId})", this);
        }

        // ─── 카드 생성 ────────────────────────────────────────────
        private List<RewardCardInstance> GenerateRewardCards(ushort cardTypeId, int count,int burstCount)
        {
            var cardData = GameDataManager.Instance.GetRewardCardsByType(cardTypeId);
            var result = new List<RewardCardInstance>();

            if (cardData == null)
            {
                LogManager.LogWarning(LogCategory.System, $"rewardId={cardTypeId}에 해당하는 카드 데이터 없음", this);
                return result;
            }

            var selectedTypes = GetRandomStatTypes(count);
            foreach (var statType in selectedTypes)
            {
                float value = GetRewardValue(cardData, statType,burstCount);
                result.Add(new RewardCardInstance(cardData, statType, value));
                LogManager.Log(LogCategory.System, $"카드 생성: {cardData.cardName} - {statType} : {value:F1}%", this);
            }

            return result;
        }

        private List<StatType> GetRandomStatTypes(int count)
        {
            var pool = rewardableStats.ToList();
            var selected = new List<StatType>();
            int actual = Mathf.Min(count, pool.Count);

            for (int i = 0; i < actual; i++)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                selected.Add(pool[idx]);
                pool.RemoveAt(idx);
            }
            return selected;
        }

        private float GetRewardValue(RewardCardData data, StatType type,int burstCount)
        {
            return type switch
            {
                StatType.BulletSpeed    => UnityEngine.Random.Range(data.bulletSpeedMinPercentage,    data.bulletSpeedMaxPercentage),
                StatType.BulletDamage   => UnityEngine.Random.Range(data.bulletDamageMinPercentage,   data.bulletDamageMaxPercentage),
                StatType.Speed          => UnityEngine.Random.Range(data.speedMinPercentage,           data.speedMaxPercentage),
                StatType.Hp             => UnityEngine.Random.Range(data.hpMinPercentage,              data.hpMaxPercentage),
                StatType.Defence        => UnityEngine.Random.Range(data.defenceMinPercentage,         data.defenceMaxPercentage),
                StatType.BulletSize     => UnityEngine.Random.Range(data.bulletSizeMinPercentage,      data.bulletSizeMaxPercentage),
                StatType.ShotDelay      => UnityEngine.Random.Range(data.shotDelayMinPercentage,       data.shotDelayMaxPercentage),
                StatType.MagazineCapacity => UnityEngine.Random.Range(data.magazineCapacityMinPercentage, data.magazineCapacityMaxPercentage) * burstCount,
                StatType.ReloadTime     => UnityEngine.Random.Range(data.reloadTimeMinPercentage,      data.reloadTimeMaxPercentage),
                _                       => 0f
            };
        }

        // ─── 카드 UI 표시 TargetRpc ───────────────────────────────
        [TargetRpc]
        private void ShowRewardCardsTargetRpc(NetworkConnection conn, List<RewardCardInstance> cards)
        {
            if (CardSelectionUI.Instance)
                CardSelectionUI.Instance.ShowRewardCards(cards, OnRewardCardSelected);
            else
                LogManager.LogWarning(LogCategory.UI, "CardSelectionUI 인스턴스가 없습니다", this);
        }

        private void OnRewardCardSelected(int cardIndex)
        {
            SelectRewardCardServerRpc(cardIndex, ClientManager.Connection);
        }

        // ─── 카드 선택 ServerRpc ──────────────────────────────────
        [ServerRpc(RequireOwnership = false)]
        private void SelectRewardCardServerRpc(int cardIndex, NetworkConnection sender = null)
        {
            if (sender == null) return;
            List<RewardCardInstance> cards = null;
            if(pendingPlayerCards.TryGetValue(sender.ClientId, out Queue<List<RewardCardInstance>> Q_cardLise))
            {
                cards = Q_cardLise.Dequeue();
            }
            
            if (cards == null || cardIndex < 0 || cardIndex >= cards.Count)
            {
                LogManager.LogWarning(LogCategory.System, $"플레이어 {sender.ClientId}의 유효하지 않은 카드 선택: {cardIndex}",
                    this);
                return;
            }

            var selected = cards[cardIndex];

            NetworkObject playerObject = NetworkPlayerManager.Instance.GetPlayerByClientId(sender.ClientId);
            if (playerObject)
            {
                ApplyRewardObserversRpc(playerObject, selected, sender.ClientId);
                LogManager.Log(LogCategory.System, $"플레이어 {sender.ClientId} 카드 선택 적용: {selected.baseData.cardName} - {selected.rewardType}", this);
            }
        }

        // ─── 보상 적용 ObserversRpc ───────────────────────────────
        [ObserversRpc]
        private void ApplyRewardObserversRpc(NetworkObject playerObject, RewardCardInstance card, int clientId)
        {
            var playerStatus = playerObject.GetComponent<PlayerStatus>();
            if (!playerStatus) return;

            ApplyRewardToPlayer(playerStatus, card);

            if (clientId == ClientManager.Connection.ClientId && playerCardRecordBoard)
                playerCardRecordBoard.AddRecord(card.rewardType, card.rewardType.ToString(), card.actualPercentage);
        }

        private void ApplyRewardToPlayer(PlayerStatus playerStatus, RewardCardInstance card)
        {
            if (!playerStatus || card == null) return;

            var modifier = new StatModifier<float>(
                $"reward_{card.baseData.cardId}_{card.rewardType}_{Time.time}",
                card.actualPercentage,
                $"{card.rewardType} 보상 (+{card.actualPercentage:F1}%)");

            playerStatus.ApplyStatModifier(card.rewardType, modifier);

            LogManager.Log(LogCategory.Player,
                $"보상 적용: {card.baseData.cardName} - {card.rewardType} -> {card.actualPercentage:F1}%", this);
        }
    }
}
