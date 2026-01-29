using System;
using Newtonsoft.Json;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent
{
    [Serializable]
    public class SpawnerData : ObjectData
    {
         private float SpawnInterval = 1f; // 스폰 간격
         private float SpawnDelay; // 초기 스폰 지연 시간
         private int   MaxSpawnCount = 5; // 이 스포너가 생성할 최대 적 수량
         private int   OneTimeSpawnAmount = 5;

         //생성자
         public SpawnerData(){}
         [JsonConstructor]
         public SpawnerData(
             [JsonProperty("TypeId")]ushort typeId,
             [JsonProperty("Hp")]float _hp,
             [JsonProperty("SpawnInterval")]float spawnInterval,
             [JsonProperty("SpawnDelay")]float spawnDelay,
             [JsonProperty("MaxSpawnCount")]int maxSpawnCount,
             [JsonProperty("OneTimeSpawnAmount")]int oneTimeSpawnAmount) : base(typeId, _hp)
         {
            SpawnInterval = spawnInterval;
            SpawnDelay = spawnDelay;
            MaxSpawnCount = maxSpawnCount;
            OneTimeSpawnAmount = oneTimeSpawnAmount;
         }
         
         //Gettor
         public float spawnInterval => SpawnInterval;
         public float spawnDelay => SpawnDelay;
         public int maxSpawnCount => MaxSpawnCount;
         public int oneTimeSpawnAmount => OneTimeSpawnAmount;
    }
}