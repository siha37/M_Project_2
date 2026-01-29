using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._3._SingleTone
{
    [Serializable]
    public class SpawnerManagerData
    {
        private ushort TypeId;
        private string SpawnTime;
        private int OneTimeSpawnerAmount;
        private ushort StartEnemyTypeId;
        private int MaxSpawnerAmount;
        
        [JsonConstructor]
        public SpawnerManagerData(
            [JsonProperty("TypeId")] ushort typeId,
            [JsonProperty("SpawnTime")] string spawnTime,
            [JsonProperty("OneTimeSpawnerAmount")] int oneTimeSpawnerAmount,
            [JsonProperty("StartEnemyTypeId")] ushort startEnemyTypeId,
            [JsonProperty("MaxSpawnerAmount")] int MaxSpawnerAmount
            )
        {
            SpawnTime = spawnTime;
            TypeId = typeId;
            OneTimeSpawnerAmount = oneTimeSpawnerAmount;
            StartEnemyTypeId = startEnemyTypeId;
            this.MaxSpawnerAmount   = MaxSpawnerAmount;
        }
        public string spawnTime_string => SpawnTime;
        public int oneTimeSpawnerAmount => OneTimeSpawnerAmount;
        public int maxSpawnerAmount => MaxSpawnerAmount;
        public ushort typeId => TypeId;
        public ushort startEnemyTypeId => StartEnemyTypeId;
    }
}