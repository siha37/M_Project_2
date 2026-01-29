using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._6._GlobalQuest._2._Data
{
    [Serializable]
    public class SurvivalQuestData : QuestData
    {

        private float MinusProgress;
        private float MinusTiming;
        private float MinusMutiple;
        private ushort SurviverTargetID;

        [JsonConstructor]
        public SurvivalQuestData(
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
            [JsonProperty("MinusProgress")]float minusProgress,
            [JsonProperty("MinusTiming")]float minusTiming,
            [JsonProperty("MinusMutiple")]float minusMutiple,
            [JsonProperty("SurviverTargetID")]ushort SurviverTargetID,
            [JsonProperty("RewardCardId")]ushort rewardCardId,
            [JsonProperty("DefeatCardId")]ushort defeatCardId) 
            : base(typeId,LimitTime, WaitingTime,Target, Progress,MaxSpawnCount,SpawnInterval,TargetEnemyDataId,OneTimeSpawnAmount,BaseSpawnInvincibleOffTime, rewardCardId, defeatCardId)
        {
            MinusProgress = minusProgress;
            MinusTiming = minusTiming;
            MinusMutiple = minusMutiple;
            this.SurviverTargetID = SurviverTargetID;
        }
        public float minusProgress => MinusProgress;
        public float minusTiming => MinusTiming;
        public float minusMutiple => MinusMutiple;
        public ushort surviverTargetID => SurviverTargetID;
    }
}