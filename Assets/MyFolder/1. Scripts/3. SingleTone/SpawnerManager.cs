using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object._1._Spawner;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class SpawnerManager : NetworkBehaviour
    {
        public static SpawnerManager instance;
        
        private void Awake()
        {
            if(!instance)
                instance = this;
        }

        [SerializeField] List<Transform> spawnPoints = new List<Transform>(); 
        
        [SerializeField] NetworkObject spawnerPrefab;
        [SerializeField] Transform spawnersParent;

        private Coroutine spawnManagerRoutine;
        
        public Action<int> spawnerSpawn;

        private readonly SyncVar<ushort> enemyLevel = new SyncVar<ushort>();
        [SerializeField] private int spawnMaxAmount = 6;
        public ushort EnemyLevel => enemyLevel.Value;
        
        //Data
        private SpawnerManagerData spawnerManagerData;
        private List<float> spawnTimes = new();
        private int spawnTimesIndex = 0;

        public List<float> SpawnTimes => spawnTimes;
        public int SpawnTimesIndex => spawnTimesIndex;
        public float NowSpawnTime => spawnTimes == null || spawnTimes.Count == 0 ? 1 : spawnTimes[spawnTimesIndex];


        [SerializeField] private Dictionary<NetworkEnemySpawner, Transform> CurrentSpawner = new();
        private readonly SyncVar<int> SpawnerAmount = new();
        public int SpawnerCount => CurrentSpawner.Count;
        public Action Enemylevel_Changed;
        
        public override void OnStartServer()
        {
            spawnerManagerData = GameDataManager.Instance.GetSpawnerManagerDataById(1);
            enemyLevel.Value = spawnerManagerData.startEnemyTypeId;
            enemyLevel.OnChange += EnemyLevel_Changed;
            spawnMaxAmount = spawnerManagerData.maxSpawnerAmount;
            Enemylevel_Changed?.Invoke();
            spawnTimesSplits();
            spawnManagerRoutine = StartCoroutine(nameof(SpawnSpawnerRutine));
        }

        public override void OnStartClient()
        {
            StartCoroutine(nameof(SpawnAmountSyncWait));
        }

        private IEnumerator SpawnAmountSyncWait()
        {
            while (!SpawnerAmount.IsInitialized)
            {
                yield return WaitForSecondsCache.Get(1);
            }

            Request_SpawnerTimerSync();
            spawnerSpawn?.Invoke(SpawnerAmount.Value);
            Enemylevel_Changed?.Invoke();
        }
        
        public override void OnStopServer()
        {
            spawnerManagerData = GameDataManager.Instance.GetSpawnerManagerDataById(1);
            spawnTimesSplits();
            StopCoroutine(spawnManagerRoutine);
        }

        private void spawnTimesSplits()
        {
            string[] time_string = spawnerManagerData.spawnTime_string.Split("/");
            for (int i = 0; i < time_string.Length; i++)
            {
                spawnTimes.Add(float.Parse(time_string[i]));
            }

            SpawnerTimerSync(spawnTimes.ToArray());
        }
       

        private IEnumerator SpawnSpawnerRutine()
        {
            for (int i = 0; i < spawnTimes.Count; i++)
            {
                spawnTimesIndex = i;
                SpawnerTimerIndexSync(spawnTimesIndex);
                while(_8._Time.TimeManager.instance.CurrentTime < spawnTimes[i])
                    yield return WaitForSecondsCache.Get(1f);
                if (spawnMaxAmount - CurrentSpawner.Count > 0)
                {
                    SpawnSpawner();
                }
                else
                {
                    enemyLevel.Value++;
                }
            }
        }
 

        private void SpawnSpawner()
        {
            System.Random rand = new System.Random();
            int amount = (spawnMaxAmount - CurrentSpawner.Count) >= spawnerManagerData.oneTimeSpawnerAmount ? spawnerManagerData.oneTimeSpawnerAmount : spawnMaxAmount - CurrentSpawner.Count;
            for (int i = 0; i < amount; i++)
            {
                spawnPoints = spawnPoints.OrderBy(_ => rand.Next()).ToList();

                NetworkObject neb = Instantiate(spawnerPrefab, spawnPoints[0].position,quaternion.identity,spawnersParent);
                if (neb)
                {
                    if (neb.TryGetComponent(out NetworkEnemySpawner spawner))
                    {
                        if (!CurrentSpawner.ContainsKey(spawner))
                        {
                            CurrentSpawner[spawner] = spawnPoints[0];
                            SpawnerAmount.Value = CurrentSpawner.Count;
                            SpawnerCountSync(CurrentSpawner.Count);
                            spawnPoints.RemoveAt(0);
                        }
                    }

                    // 활성화 보장
                    neb.gameObject.SetActive(true);
                    
                    //Data 초기화
                    //if (neb.TryGetComponent(out SpawnerStatus status))
                     //   status.InitializeData(1);
                    
                    InstanceFinder.ServerManager.Spawn(neb);
                    
                    SpawnerDataSync(neb);
                }
            }
        }

        [ObserversRpc]
        private void SpawnerDataSync(NetworkObject neb)
        {
            if(neb.TryGetComponent(out SpawnerStatus status))
                status.InitializeData(1);
        }
        [ObserversRpc]
        private void SpawnerCountSync(int value)
        {
            spawnerSpawn.Invoke(value);
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void Request_SpawnerTimerSync(NetworkConnection conn = null)
        {
            // 요청한 클라이언트에게만 전송
            Target_SpawnerTimerSync(conn, spawnTimes.ToArray());
        }

        [TargetRpc]
        private void Target_SpawnerTimerSync(NetworkConnection conn, float[] value)
        {
            spawnTimes = new List<float>(value);
        }
        
        [ObserversRpc]
        private void SpawnerTimerSync(float[] value)
        {
            if (!IsServerInitialized)
            {
                spawnTimes = new List<float>(value);
            }
        }

        [ObserversRpc]
        private void SpawnerTimerIndexSync(int value)
        {
            if (!IsServerInitialized)
            {
                spawnTimesIndex = value;
            }
        }
        

        public void RemoveSpawner(NetworkEnemySpawner spawner)
        {
            if (CurrentSpawner != null && CurrentSpawner.ContainsKey(spawner))
            {
                if (!spawnPoints.Contains(CurrentSpawner[spawner]))
                    spawnPoints.Add(CurrentSpawner[spawner]);
                CurrentSpawner.Remove(spawner);
                SpawnerAmount.Value = CurrentSpawner.Count;
                SpawnerCountSync(CurrentSpawner.Count);
            }
        }

        private void EnemyLevel_Changed(ushort oldValue, ushort newValue, bool isServer)
        {
            if(newValue != oldValue)
                Enemylevel_Changed?.Invoke();
        }
    }
}
