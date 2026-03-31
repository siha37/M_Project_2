using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Transporting;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using MyFolder._1._Scripts._1._UI._0._GameStage._2._Card;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._6._GlobalQuest._3._Card
{
    public class QuestCardManager : NetworkBehaviour
    {
        public static QuestCardManager Instance { get; private set; }

        private StatType[] defeatAlbeState =
        {
            StatType.Speed, StatType.Hp, StatType.BulletSpeed,
            StatType.BulletDamage, StatType.BulletSize
        };

        private Dictionary<int, List<DefeatCardInstance>> playerDefeatCards = new();

        [SerializeField] private CardRewardRecordBoard EnemycardRewardRecordBoard;

        [System.Serializable]
        public class DefeatCardInstance
        {
            public DefeatCardData baseData;
            public StatType defeatType;
            public float actualPercentage;

            public DefeatCardInstance() { }

            public DefeatCardInstance(DefeatCardData baseData, StatType defeatType, float actualPercentage)
            {
                this.baseData = baseData;
                this.defeatType = defeatType;
                this.actualPercentage = actualPercentage;
            }
        }

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
                playerDefeatCards.Remove(conn.ClientId);
            }
        }

        // ─── 퀘스트 실패 처리 ─────────────────────────────────────
        /*
        public void HandleQuestFailure(QuestData questData)
        {
            if (!IsServerInitialized) return;

            var destroyers = GetRandomDestroyers();
            foreach (var destroyer in destroyers)
            {
                var playerStatus = destroyer.GetComponent<PlayerStatus>();
                var playerContext = playerStatus?.GetComponent<PlayerContext>();
                var defeatCards = GenerateDefeatCardInstances(questData.DamageStack, GetDefeatCardCount());

                if (playerContext?.Sync?.Owner != null)
                    playerDefeatCards[playerContext.Sync.Owner.ClientId] = defeatCards;

                ShowDefeatCardsToPlayer(destroyer, defeatCards);
            }
            LogManager.Log(LogCategory.System, "퀘스트 실패 - 제거자에게 패배 카드 배분 완료", this);
        }*/

        private List<DefeatCardInstance> GenerateDefeatCardInstances(ushort cardTypeId, int count)
        {
            var availableCards = GameDataManager.Instance.GetDefeatCardsByType(cardTypeId);
            var cardInstances = new List<DefeatCardInstance>();

            if (availableCards == null)
            {
                LogManager.LogWarning(LogCategory.System,
                    $"CardTypeId {cardTypeId}에 해당하는 패배 카드가 없습니다", this);
                return cardInstances;
            }

            var selectedTypes = GetRandomDefeatTypes(count);
            for (int i = 0; i < selectedTypes.Count; i++)
            {
                StatType defeatType = selectedTypes[i];
                float actualValue = GetDefeatValue(availableCards, defeatType);
                cardInstances.Add(new DefeatCardInstance(availableCards, defeatType, actualValue));
                LogManager.Log(LogCategory.System,
                    $"패배 카드 생성: {availableCards.cardName} - {defeatType} : {actualValue:F1}%", this);
            }
            return cardInstances;
        }

        private List<StatType> GetRandomDefeatTypes(int count)
        {
            var availableTypes = defeatAlbeState.ToList();
            var selectedTypes = new List<StatType>();
            int actualCount = Mathf.Min(count, availableTypes.Count);
            for (int i = 0; i < actualCount; i++)
            {
                int randomIndex = Random.Range(0, availableTypes.Count);
                selectedTypes.Add(availableTypes[randomIndex]);
                availableTypes.RemoveAt(randomIndex);
            }
            return selectedTypes;
        }

        private float GetDefeatValue(DefeatCardData cardData, StatType defeatType)
        {
            return defeatType switch
            {
                StatType.BulletSpeed  => Random.Range(cardData.enemyBulletSpeedMinPercentage,  cardData.enemyBulletSpeedMaxPercentage),
                StatType.BulletDamage => Random.Range(cardData.enemyBulletDamageMinPercentage, cardData.enemyBulletDamageMaxPercentage),
                StatType.BulletSize   => Random.Range(cardData.enemyBulletSizeMinPercentage,   cardData.enemyBulletSizeMaxPercentage),
                StatType.Speed        => Random.Range(cardData.enemySpeedMinPercentage,         cardData.enemySpeedMaxPercentage),
                StatType.Hp           => Random.Range(cardData.enemyHpMinPercentage,            cardData.enemyHpMaxPercentage),
                _                     => 0f
            };
        }

        private void ShowDefeatCardsToPlayer(NetworkObject destroyer, List<DefeatCardInstance> defeatCards)
        {
            var playerStatus = destroyer.GetComponent<PlayerStatus>();
            var playerContext = playerStatus?.GetComponent<PlayerContext>();
            if (!destroyer || playerContext?.Sync?.Owner == null) return;
            ShowDefeatCardsClientRpc(playerContext.Sync.Owner, defeatCards);
        }

        [TargetRpc]
        private void ShowDefeatCardsClientRpc(NetworkConnection conn, List<DefeatCardInstance> defeatCards)
        {
            if (CardSelectionUI.Instance)
                CardSelectionUI.Instance.ShowDefeatCards(defeatCards, OnDefeatCardSelected);
            else
                LogManager.LogWarning(LogCategory.UI, "CardSelectionUI 인스턴스가 없습니다", this);
        }

        private void OnDefeatCardSelected(int cardIndex)
        {
            DefeatCardSelectedServerRpc(cardIndex, ClientManager.Connection);
        }

        [ServerRpc(RequireOwnership = false)]
        private void DefeatCardSelectedServerRpc(int cardIndex, NetworkConnection sender = null)
        {
            if (sender == null) return;

            if (playerDefeatCards.TryGetValue(sender.ClientId, out var defeatCards) &&
                cardIndex >= 0 && cardIndex < defeatCards.Count)
            {
                var selectedCard = defeatCards[cardIndex];
                ApplyDefeatCardsToAI(selectedCard);
                playerDefeatCards.Remove(sender.ClientId);
                UpdateDefeatCardRecordObserversRpc(selectedCard.defeatType, selectedCard.actualPercentage);
                LogManager.Log(LogCategory.System,
                    $"제거자 {sender.ClientId}가 패배 카드 '{selectedCard.baseData.cardName}' - {selectedCard.defeatType} 선택 및 적용", this);
            }
            else
            {
                LogManager.LogWarning(LogCategory.System,
                    $"제거자 {sender.ClientId}의 유효하지 않은 카드 선택: {cardIndex}", this);
            }
        }

        private void ApplyDefeatCardsToAI(DefeatCardInstance cardInstance)
        {
            var modifier = new StatModifier<float>(
                $"defeat_{cardInstance.baseData.cardId}_{cardInstance.defeatType}_{Time.time}",
                cardInstance.actualPercentage,
                $"{cardInstance.defeatType} 패배 효과");
            ApplyDefeatModifierToAllAI(cardInstance.defeatType, modifier);
        }

        private void ApplyDefeatModifierToAllAI(StatType statType, StatModifier<float> modifier)
        {
            if (ApplyShootingDataModifierToAllAI(statType, modifier)) return;

            var allEnemyData = GameDataManager.Instance.GetAllEnemyData();
            foreach (var enemyData in allEnemyData)
            {
                switch (statType)
                {
                    case StatType.Speed: enemyData.AddSpeedModifier(modifier); break;
                    case StatType.Hp:    enemyData.AddHpModifier(modifier);    break;
                }
            }
        }

        private bool ApplyShootingDataModifierToAllAI(StatType statType, StatModifier<float> modifier)
        {
            if (statType != StatType.BulletDamage && statType != StatType.BulletSpeed &&
                statType != StatType.BulletSize)
                return false;

            var allShootingData = GameDataManager.Instance.GetAllShootingData();
            foreach (var shootingData in allShootingData)
            {
                switch (statType)
                {
                    case StatType.BulletSpeed:  shootingData.AddBulletSpeedModifier(modifier);  break;
                    case StatType.BulletDamage: shootingData.AddBulletDamageModifier(modifier); break;
                    case StatType.BulletSize:   shootingData.AddBulletSizeModifier(modifier);   break;
                }
            }
            return true;
        }

        [ObserversRpc]
        private void UpdateDefeatCardRecordObserversRpc(StatType defeatType, float actualPercentage)
        {
            if (EnemycardRewardRecordBoard)
                EnemycardRewardRecordBoard.AddERecord(defeatType, defeatType.ToString(), actualPercentage);
        }

        private List<NetworkObject> GetRandomDestroyers()
        {
            var allPlayers = NetworkPlayerManager.Instance.GetAllPlayers();
            var destroyers = new List<NetworkObject>();

            foreach (var player in allPlayers)
            {
                var playerSettings = PlayerSettingManager.Instance.GetPlayerSettings(player.Owner.ClientId);
                if (playerSettings is { role: PlayerRoleType.Destroyer })
                    destroyers.Add(player);
            }

            int maxCount = GetMaxDestroyersForDefeatCards();
            return destroyers.OrderBy(x => Random.value).Take(maxCount).ToList();
        }

        private int GetDefeatCardCount() => 3;

        private int GetMaxDestroyersForDefeatCards()
            => GameSettingManager.Instance?.GetCurrentSettings()?.maxDestroyersForDefeatCards ?? 2;
    }
}
