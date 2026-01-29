using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._6._GlobalQuest._2._Data
{
    [Serializable]
    public class ExterminationQuestData : QuestData
    {
        [JsonConstructor]
        public ExterminationQuestData(
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
            [JsonProperty("DefeatCardId")]ushort defeatCardId) :
            base(typeId,LimitTime, WaitingTime, Target,Progress,MaxSpawnCount,SpawnInterval,TargetEnemyDataId,OneTimeSpawnAmount,BaseSpawnInvincibleOffTime, rewardCardId, defeatCardId)
        {
        }
    }
}