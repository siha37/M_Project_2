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

        
        private StatType[] rewardAlbeState = new[]
        {
            StatType.Speed, StatType.Hp, StatType.Defence, StatType.BulletSpeed,
            StatType.BulletDamage, StatType.BulletSize, StatType.MagazineCapacity, StatType.ShotDelay,
            StatType.ReloadTime
        };
        
        private StatType[] defeatAlbeState = new[]
        {
            StatType.Speed, StatType.Hp, StatType.BulletSpeed,
            StatType.BulletDamage, StatType.BulletSize
        };
        // 플레이어별 카드 선택 상태 관리
        private Dictionary<int, List<RewardCardInstance>> playerRewardCards = new Dictionary<int, List<RewardCardInstance>>();
        private Dictionary<int, List<DefeatCardInstance>> playerDefeatCards = new Dictionary<int, List<DefeatCardInstance>>();

        [FormerlySerializedAs("cardRewardRecordBoard")] [SerializeField] private CardRewardRecordBoard PlayercardRewardRecordBoard;
        [SerializeField] private CardRewardRecordBoard EnemycardRewardRecordBoard;
        
        // 카드 인스턴스 클래스들
        [System.Serializable]
        public class RewardCardInstance
        {
            public RewardCardData baseData;
            public StatType rewardType;           // 선택된 보상 타입
            public float actualPercentage;        // 실제 적용될 값
            
            public RewardCardInstance() { }

            public RewardCardInstance(RewardCardData baseData, StatType rewardType, float actualPercentage)
            {
                this.baseData = baseData;
                this.rewardType = rewardType;
                this.actualPercentage = actualPercentage;
            }
        }
        
        [System.Serializable]
        public class DefeatCardInstance
        {
            public DefeatCardData baseData;
            public StatType defeatType;           // 선택된 패배 타입
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
    
            // 플레이어 연결 해제 이벤트 구독
            ServerManager.OnRemoteConnectionState += OnRemoteConnectionState;
        }

        public override void OnStopServer()
        {
            base.OnStopServer();
    
            // 이벤트 구독 해제
            ServerManager.OnRemoteConnectionState -= OnRemoteConnectionState;
        }
        /// <summary>
        /// 플레이어 연결 상태 변경 시 호출
        /// </summary>
        private void OnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs args)
        {
            // 플레이어가 연결 해제되면 Dictionary 정리
            if (args.ConnectionState == RemoteConnectionState.Stopped)
            {
                if (playerRewardCards.ContainsKey(conn.ClientId))
                {
                    playerRewardCards.Remove(conn.ClientId);
                    LogManager.Log(LogCategory.System, 
                        $"플레이어 {conn.ClientId} 연결 해제 - 보상 카드 데이터 정리", this);
                }
        
                if (playerDefeatCards.ContainsKey(conn.ClientId))
                {
                    playerDefeatCards.Remove(conn.ClientId);
                    LogManager.Log(LogCategory.System, 
                        $"플레이어 {conn.ClientId} 연결 해제 - 패배 카드 데이터 정리", this);
                }
            }
        }
        
        /// <summary>
        /// 퀘스트 성공 처리
        /// </summary>
        public void HandleQuestSuccess(QuestData questData)
        {
            if (!IsServerInitialized) return;
            
            var allPlayers = NetworkPlayerManager.Instance.GetAllPlayers();
            
            foreach(var player in allPlayers)
            {
                var playerStatus = player.GetComponent<PlayerStatus>();
                var playerContext = playerStatus?.GetComponent<PlayerContext>();
                
                if(playerStatus && playerContext?.Sync?.Owner != null)
                {
                    var connection = playerContext.Sync.Owner;
                    
                    // 퀘스트의 RewardCardId 기반으로 카드 생성
                    var rewardCards = GenerateRewardCardInstances(questData.rewardCardId, GetRewardCardCount());
                    
                    // 플레이어별 카드 상태 저장
                    playerRewardCards[connection.ClientId] = rewardCards;
                    
                    ShowRewardCardsToPlayer(playerStatus, rewardCards);
                }
            }
            
            LogManager.Log(LogCategory.System, 
                $"퀘스트 성공 - 모든 플레이어에게 보상 카드 배분 완료", this);
        }
        
        /// <summary>
        /// 퀘스트 실패 처리
        /// </summary>
        public void HandleQuestFailure(QuestData questData)
        {
            if (!IsServerInitialized) return;
            
            
            // AI에 적용
            //ApplyDefeatCardsToAI(defeatCards);
            
            // 제거자들에게 선택 UI 제공
            var destroyers = GetRandomDestroyers();
            foreach(var destroyer in destroyers)
            {
                var playerStatus = destroyer.GetComponent<PlayerStatus>();
                var playerContext = playerStatus?.GetComponent<PlayerContext>();
                // 패배 카드 생성
                var defeatCards = GenerateDefeatCardInstances(questData.defeatCardId, GetDefeatCardCount());
                
                if (playerContext?.Sync?.Owner != null)
                {
                    var connection = playerContext.Sync.Owner;
                    
                    // 플레이어별 카드 상태 저장
                    playerDefeatCards[connection.ClientId] = defeatCards;
                }
                ShowDefeatCardsToPlayer(destroyer, defeatCards);
            }
            LogManager.Log(LogCategory.System,$"퀘스트 실패 - AI 패배 효과 적용 및 제거자에게 카드 배분 완료", this);
        }
        
        /// <summary>
        /// 보상 카드 인스턴스 생성
        /// </summary>
        private List<RewardCardInstance> GenerateRewardCardInstances(ushort cardTypeId, int count)
        {
            var availableCards = GameDataManager.Instance.GetRewardCardsByType(cardTypeId);
            var cardInstances = new List<RewardCardInstance>();
            
            if (availableCards == null)
            {
                LogManager.LogWarning(LogCategory.System, 
                    $"CardTypeId {cardTypeId}에 해당하는 보상 카드가 없습니다", this);
                return cardInstances;
            }
            
            // 중복 없이 랜덤 보상 타입들 선택
            var selectedRewardTypes = GetRandomRewardTypes(count);
            
            for(int i = 0; i < selectedRewardTypes.Count; i++)
            {
                StatType rewardType = selectedRewardTypes[i];
                
                // 선택된 타입에 맞는 속성값 가져오기
                float actualValue = GetRewardValue(availableCards, rewardType);
                
                var cardInstance = new RewardCardInstance(availableCards, rewardType, actualValue);
                
                cardInstances.Add(cardInstance);
                
                LogManager.Log(LogCategory.System, 
                    $"보상 카드 생성: {availableCards.cardName} - {rewardType} : {actualValue:F1}%", this);
            }
            
            return cardInstances;
        }
        
        /// <summary>
        /// 패배 카드 인스턴스 생성
        /// </summary>
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
            
            // 중복 없이 랜덤 패배 타입들 선택
            var selectedDefeatTypes = GetRandomDefeatTypes(count);
            
            for(int i = 0; i < selectedDefeatTypes.Count; i++)
            {
                StatType defeatType = selectedDefeatTypes[i];
                
                // 선택된 타입에 맞는 속성값 가져오기
                float actualValue = GetDefeatValue(availableCards, defeatType);
                
                var cardInstance = new DefeatCardInstance(availableCards, defeatType, actualValue);
                cardInstances.Add(cardInstance);
                
                LogManager.Log(LogCategory.System, 
                    $"패배 카드 생성: {availableCards.cardName} - {defeatType} : {actualValue:F1}%", this);
            }
            
            return cardInstances;
        }
        
        /// <summary>
        /// 랜덤 보상 타입 선택 (중복 없이)
        /// </summary>
        private List<StatType> GetRandomRewardTypes(int count)
        {
            var availableTypes = rewardAlbeState.ToList();
            var selectedTypes = new List<StatType>();
            
            // count가 전체 타입 수보다 크면 전체 타입 수로 제한
            int actualCount = Mathf.Min(count, availableTypes.Count);
            
            for (int i = 0; i < actualCount; i++)
            {
                int randomIndex = Random.Range(0, availableTypes.Count);
                selectedTypes.Add(availableTypes[randomIndex]);
                availableTypes.RemoveAt(randomIndex); // 선택된 타입 제거로 중복 방지
            }
            availableTypes.Clear();
            return selectedTypes;
        }
        
        /// <summary>
        /// 랜덤 패배 타입 선택 (중복 없이)
        /// </summary>
        private List<StatType> GetRandomDefeatTypes(int count)
        {
            var availableTypes = defeatAlbeState.ToList();
            var selectedTypes = new List<StatType>();
            
            // count가 전체 타입 수보다 크면 전체 타입 수로 제한
            int actualCount = Mathf.Min(count, availableTypes.Count);
            
            for (int i = 0; i < actualCount; i++)
            {
                int randomIndex = Random.Range(0, availableTypes.Count);
                selectedTypes.Add(availableTypes[randomIndex]);
                availableTypes.RemoveAt(randomIndex); // 선택된 타입 제거로 중복 방지
            }
            availableTypes.Clear();
            return selectedTypes;
        }
        
        /// <summary>
        /// 보상 카드 데이터에서 해당 타입의 값 가져오기
        /// </summary>
        private float GetRewardValue(RewardCardData cardData, StatType rewardType)
        {
            switch(rewardType)
            {
                case StatType.BulletSpeed:
                    return Random.Range(cardData.bulletSpeedMinPercentage, cardData.bulletSpeedMaxPercentage);
                case StatType.BulletDamage:
                    return Random.Range(cardData.bulletDamageMinPercentage, cardData.bulletDamageMaxPercentage);
                case StatType.Speed:
                    return Random.Range(cardData.speedMinPercentage, cardData.speedMaxPercentage);
                case StatType.Hp:
                    return Random.Range(cardData.hpMinPercentage, cardData.hpMaxPercentage);
                case StatType.Defence:
                    return Random.Range(cardData.defenceMinPercentage, cardData.defenceMaxPercentage);
                case StatType.BulletSize:
                    return Random.Range(cardData.bulletSizeMinPercentage, cardData.bulletSizeMaxPercentage);
                case StatType.ShotDelay:
                    return Random.Range(cardData.shotDelayMinPercentage, cardData.shotDelayMaxPercentage);
                case StatType.MagazineCapacity:
                    return Random.Range(cardData.magazineCapacityMinPercentage, cardData.magazineCapacityMaxPercentage);
                case StatType.ReloadTime:
                    return Random.Range(cardData.reloadTimeMinPercentage, cardData.reloadTimeMaxPercentage);
                default:
                    return 0f;
            }
        }
        
        /// <summary>
        /// 패배 카드 데이터에서 해당 타입의 값 가져오기
        /// </summary>
        private float GetDefeatValue(DefeatCardData cardData, StatType defeatType)
        {
            switch(defeatType)
            {
                case StatType.BulletSpeed:
                    return Random.Range(cardData.enemyBulletSpeedMinPercentage, cardData.enemyBulletSpeedMaxPercentage);
                case StatType.BulletDamage:
                    return Random.Range(cardData.enemyBulletDamageMinPercentage, cardData.enemyBulletDamageMaxPercentage);
                case StatType.BulletSize:
                    return Random.Range(cardData.enemyBulletSizeMinPercentage, cardData.enemyBulletSizeMaxPercentage);
                case StatType.Speed:
                    return Random.Range(cardData.enemySpeedMinPercentage, cardData.enemySpeedMaxPercentage);
                case StatType.Hp:
                    return Random.Range(cardData.enemyHpMinPercentage, cardData.enemyHpMaxPercentage);
                default:
                    return 0f;
            }
        }
        
        [ObserversRpc]
        private void ApplyRewardToPlayerObserversRpc(NetworkObject playerObject, RewardCardInstance cardInstance,int clientId)
        {
            var playerStatus = playerObject.GetComponent<PlayerStatus>();
            if (playerStatus)
            {
                // 모든 클라이언트에서 실행됨
                ApplyRewardToPlayer(playerStatus, cardInstance);
                
                if(clientId == ClientManager.Connection.ClientId)
                    PlayercardRewardRecordBoard.AddRecord(cardInstance.rewardType,cardInstance.rewardType.ToString(),cardInstance.actualPercentage);
            }
        }
        
        /// <summary>
        /// 플레이어에게 보상 적용
        /// </summary>
        public void ApplyRewardToPlayer(PlayerStatus playerStatus, RewardCardInstance cardInstance)
        {
            if (!playerStatus || cardInstance == null) return;
            
            var modifier = new StatModifier<float>(
                $"reward_{cardInstance.baseData.cardId}_{cardInstance.rewardType}_{Time.time}",
                cardInstance.actualPercentage,
                $"{cardInstance.rewardType} 보상 (+{cardInstance.actualPercentage:F1}%)"
            );
            
            playerStatus.ApplyStatModifier(cardInstance.rewardType, modifier);
            
            LogManager.Log(LogCategory.Player, 
                $"보상 카드 적용: {cardInstance.baseData.cardName} - {cardInstance.rewardType} -> {cardInstance.actualPercentage:F1}%", this);
        }
        
        /// <summary>
        /// AI에게 패배 카드 적용
        /// </summary>
        private void ApplyDefeatCardsToAI(DefeatCardInstance cardInstance)
        {   
            var modifier = new StatModifier<float>(
                $"defeat_{cardInstance.baseData.cardId}_{cardInstance.defeatType}_{Time.time}",
                cardInstance.actualPercentage,
                $"{cardInstance.defeatType} 패배 효과"
            );
            
            ApplyDefeatModifierToAllAI(cardInstance.defeatType, modifier);
        }
        
        
        
        /// <summary>
        /// 모든 AI에 패배 효과 적용
        /// </summary>
        private void ApplyDefeatModifierToAllAI(StatType statType, StatModifier<float> modifier)
        {
            if (ApplyShootingDataModifierToAllAI(statType, modifier))
            {
                return;
                
            }
            
            var allEnemyData = GameDataManager.Instance.GetAllEnemyData();
            
            foreach(var enemyData in allEnemyData)
            {
                switch(statType)
                {
                    case StatType.Speed:
                        enemyData.AddSpeedModifier(modifier);
                        break;
                    case StatType.Hp:
                        enemyData.AddHpModifier(modifier);
                        break;
                }
            }
        }
        
        /// <summary>
        /// 모든 AI의 ShootingData에 수정자 적용
        /// </summary>
        private bool ApplyShootingDataModifierToAllAI(StatType statType, StatModifier<float> modifier)
        {
            if (statType != StatType.BulletDamage && statType != StatType.BulletSpeed &&
                statType != StatType.BulletSize)
            {
                return false;
            }
            var allShootingData = GameDataManager.Instance.GetAllShootingData();
            foreach(var shootingData in allShootingData)
            {
                switch(statType)
                {
                    case StatType.BulletSpeed:
                        shootingData.AddBulletSpeedModifier(modifier);
                        break;
                    case StatType.BulletDamage:
                        shootingData.AddBulletDamageModifier(modifier);
                        break;
                    case StatType.BulletSize:
                        shootingData.AddBulletSizeModifier(modifier);
                        break;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 랜덤 제거자 플레이어 선택
        /// </summary>
        private List<NetworkObject> GetRandomDestroyers()
        {
            var allPlayers = NetworkPlayerManager.Instance.GetAllPlayers();
            var destroyers = new List<NetworkObject>();
            
            foreach(var player in allPlayers)
            {
                var playerSettings = PlayerSettingManager.Instance.GetPlayerSettings(player.Owner.ClientId);
                if(playerSettings is { role: PlayerRoleType.Destroyer })
                {
                    destroyers.Add(player);
                }
            }
            
            // 설정된 최대 수만큼 랜덤 선택
            int maxCount = GetMaxDestroyersForDefeatCards();
            var selectedDestroyers = destroyers.OrderBy(x => Random.value).Take(maxCount).ToList();
            
            LogManager.Log(LogCategory.System, 
                $"제거자 {selectedDestroyers.Count}명 선정 (총 제거자: {destroyers.Count}명)", this);
            
            return selectedDestroyers;
        }
        
        /// <summary>
        /// 플레이어에게 보상 카드 UI 표시
        /// </summary>
        private void ShowRewardCardsToPlayer(PlayerStatus playerStatus, List<RewardCardInstance> rewardCards)
        {
            // PlayerContext를 통해 NetworkConnection 얻기
            var playerContext = playerStatus.GetComponent<PlayerContext>();
            if (!playerStatus || playerContext?.Sync?.Owner == null) return;
            
            var connection = playerContext.Sync.Owner;
            
            // 해당 클라이언트에게만 UI 표시
            ShowRewardCardsClientRpc(connection, rewardCards);
            
            LogManager.Log(LogCategory.System,$"플레이어 {connection.ClientId}에게 보상 카드 {rewardCards.Count}장 표시", this);
        }
        
        /// <summary>
        /// 제거자에게 패배 카드 UI 표시
        /// </summary>
        private void ShowDefeatCardsToPlayer(NetworkObject destroyer, List<DefeatCardInstance> defeatCards)
        {
            var playerStatus = destroyer.GetComponent<PlayerStatus>();
            var playerContext = playerStatus?.GetComponent<PlayerContext>();
            
            if (!destroyer || playerContext?.Sync?.Owner == null) return;
            
            var connection = playerContext.Sync.Owner;
            
            // 해당 클라이언트에게만 UI 표시
            ShowDefeatCardsClientRpc(connection, defeatCards);
            
            LogManager.Log(LogCategory.System, 
                $"제거자 {connection.ClientId}에게 패배 카드 {defeatCards.Count}장 표시", this);
        }
        
        /// <summary>
        /// 클라이언트에게 보상 카드 UI 표시 RPC
        /// </summary>
        [TargetRpc]
        private void ShowRewardCardsClientRpc(NetworkConnection conn, List<RewardCardInstance> rewardCards)
        {
            if (CardSelectionUI.Instance)
            {
                CardSelectionUI.Instance.ShowRewardCards(rewardCards, OnRewardCardSelected);
            }
            else
            {
                LogManager.LogWarning(LogCategory.UI, "CardSelectionUI 인스턴스가 없습니다", this);
            }
        }
        
        /// <summary>
        /// 클라이언트에게 패배 카드 UI 표시 RPC
        /// </summary>
        [TargetRpc]
        private void ShowDefeatCardsClientRpc(NetworkConnection conn, List<DefeatCardInstance> defeatCards)
        {
            if (CardSelectionUI.Instance)
            {
                CardSelectionUI.Instance.ShowDefeatCards(defeatCards, OnDefeatCardSelected);
            }
            else
            {
                LogManager.LogWarning(LogCategory.UI, "CardSelectionUI 인스턴스가 없습니다", this);
            }
        }
        
        /// <summary>
        /// 보상 카드 선택 완료 콜백
        /// </summary>
        private void OnRewardCardSelected(int cardIndex)
        {
            // 서버에 선택 결과 전송
            RewardCardSelectedServerRpc(cardIndex,ClientManager.Connection);
        }
        
        /// <summary>
        /// 패배 카드 선택 완료 콜백
        /// </summary>
        private void OnDefeatCardSelected(int cardIndex)
        {
            // 서버에 선택 결과 전송
            DefeatCardSelectedServerRpc(cardIndex,ClientManager.Connection);
        }
        
        /// <summary>
        /// 보상 카드 선택 결과 서버 처리
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void RewardCardSelectedServerRpc(int cardIndex, NetworkConnection sender = null)
        {
            if (sender == null) return;
            
            // 해당 플레이어의 저장된 카드 목록에서 선택된 카드 가져오기
            if (playerRewardCards.TryGetValue(sender.ClientId, out var rewardCards) && 
                cardIndex >= 0 && cardIndex < rewardCards.Count)
            {
                RewardCardInstance selectedCard = rewardCards[cardIndex];
                // 해당 플레이어의 PlayerStatus 찾기
                NetworkObject playerObject = NetworkPlayerManager.Instance.GetPlayerByClientId(sender.ClientId);
                if (playerObject)
                {
                    ApplyRewardToPlayerObserversRpc(playerObject, selectedCard,sender.ClientId);
                    LogManager.Log(LogCategory.System,$"플레이어 {sender.ClientId}가 보상 카드 '{selectedCard.baseData.cardName}' - {selectedCard.rewardType} 선택 및 적용", this);
                }
                // 사용된 카드 목록 정리
                playerRewardCards.Remove(sender.ClientId);
            }
            else
            {
                LogManager.LogWarning(LogCategory.System,$"플레이어 {sender.ClientId}의 유효하지 않은 카드 선택: {cardIndex}", this);
            }
        }
        
        /// <summary>
        /// 패배 카드 선택 결과 서버 처리
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        private void DefeatCardSelectedServerRpc(int cardIndex, NetworkConnection sender = null)
        {
            
            if (sender == null) return;
    
            if (playerDefeatCards.TryGetValue(sender.ClientId, out var defeatCards) && 
                cardIndex >= 0 && cardIndex < defeatCards.Count)
            {
                var selectedCard = defeatCards[cardIndex];

                ApplyDefeatCardsToAI(selectedCard);
        
                // 사용된 카드 목록 정리
                playerDefeatCards.Remove(sender.ClientId);
        
                // 모든 클라이언트에 적 카드 기록 전파
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
        
        /// <summary>
        /// 모든 클라이언트에 적 카드 기록 업데이트
        /// </summary>
        [ObserversRpc]
        private void UpdateDefeatCardRecordObserversRpc(StatType defeatType, float actualPercentage)
        {
            if (EnemycardRewardRecordBoard)
            {
                EnemycardRewardRecordBoard.AddERecord(defeatType, defeatType.ToString(), actualPercentage);
            }
        }
        
        /// <summary>
        /// 게임 설정에서 값 가져오기
        /// </summary>
        /// <returns></returns>
        private int GetRewardCardCount()
        {
            return 3;
        }
        
        private int GetDefeatCardCount()
        {
            return 3;
        }
        
        private int GetMaxDestroyersForDefeatCards()
        {
            return GameSettingManager.Instance?.GetCurrentSettings()?.maxDestroyersForDefeatCards ?? 2;
        }
    }
}
