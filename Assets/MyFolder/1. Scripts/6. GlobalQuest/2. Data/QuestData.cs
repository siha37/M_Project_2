using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._6._GlobalQuest._2._Data
{
    [Serializable]
    public class QuestData
    {
        protected ushort TypeId;
        protected float LimitTime;
        protected float WaitingTime;
        protected float Target;
        protected float Progress;
        protected int MaxSpawnCount;
        protected int OneTimeSpawnAmount;
        protected float SpawnInterval;
        protected ushort TargetEnemyDataId;
        protected float BaseSpawnInvincibleOffTime;
        
        // 카드 ID 필드 추가
        protected ushort RewardCardId;
        protected ushort DefeatCardId;

        [JsonConstructor]
        public QuestData(
            [JsonProperty("TypeId")] ushort typeId,
            [JsonProperty("LimitTime")]float LimitTime,
            [JsonProperty("WaitingTime")]float WaitingTime,
            [JsonProperty("Target")]float Target,
            [JsonProperty("Progress")]float Progress,
            [JsonProperty("MaxSpawnCount")]int MaxSpawnCount,
            [JsonProperty("SpawnInterval")]float SpawnInterval,
            [JsonProperty("TargetEnemyDataId")]ushort TargetEnemyDataId,
            [JsonProperty("OneTimeSpawnAmount")]int OneTimeSpawnAmount,
            [JsonProperty("BaseSpawnInvincibleOffTime")]float BaseSpawnInvincibleOffTime,
            [JsonProperty("RewardCardId")]ushort rewardCardId,
            [JsonProperty("DefeatCardId")]ushort defeatCardId)
        {
            this.TypeId = typeId;
            this.LimitTime = LimitTime;
            this.WaitingTime = WaitingTime;
            this.Target = Target;
            this.Progress = Progress;
            this.MaxSpawnCount = MaxSpawnCount;
            this.SpawnInterval = SpawnInterval;
            this.TargetEnemyDataId = TargetEnemyDataId;
            this.OneTimeSpawnAmount = OneTimeSpawnAmount;
            this.BaseSpawnInvincibleOffTime = BaseSpawnInvincibleOffTime;
            this.RewardCardId = rewardCardId;
            this.DefeatCardId = defeatCardId;
        }
        public ushort typeId => TypeId;
        public float  limitTime => LimitTime;
        public float  waitingTime => WaitingTime;
        public float  target => Target;
        public float  progress => Progress;
        public int maxSpawnCount =>MaxSpawnCount;
        public float spawnInterval =>SpawnInterval;
        public ushort targetEnemyDataId =>TargetEnemyDataId;
        public int oneTimeSpawnAmount =>OneTimeSpawnAmount;
        public float baseSpawnInvincibleOffTime =>BaseSpawnInvincibleOffTime;
        
        // 카드 ID 접근자 추가
        public ushort rewardCardId => RewardCardId;
        public ushort defeatCardId => DefeatCardId;
    }
}