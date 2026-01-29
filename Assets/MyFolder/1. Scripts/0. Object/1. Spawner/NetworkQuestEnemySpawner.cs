using System;
using FishNet;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._1._Spawner
{
    public class NetworkQuestEnemySpawner : NetworkBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int maxSpawnCount = 10;
        [SerializeField] private float spawnInterval = 2f;
        [SerializeField] private int OneTimeSpawnAmount = 5;
        [SerializeField] private ushort targetEnemyDataId = 1;
        
        [Header("Quest Settings")]
        [SerializeField] private int questId = -1;
        [SerializeField] private GlobalQuestType questType;
        [SerializeField] private Transform defencePriorityTarget;
        [SerializeField] private QuestPoint questPoint;

        private Transform Container;

        private int currentSpawned;
        private Coroutine spawnLoop;
        private bool isInitialized;
        public GlobalQuestType QuestType => questType;
        public QuestPoint QuestPoint => questPoint;

        public Action QuestSpawnerStop;

        // 생성자 역할: 메타/참조 설정 (Defense는 지연 시작, 나머지는 즉시 시작)
        public void Initialize(int questId, GlobalQuestType type, QuestPoint point, QuestData questData)
        {
            this.questId = questId;
            this.questType = type;
            this.questPoint = point;
            this.maxSpawnCount = questData.maxSpawnCount;
            this.spawnInterval = questData.spawnInterval;
            this.targetEnemyDataId = questData.targetEnemyDataId;
            this.OneTimeSpawnAmount = questData.oneTimeSpawnAmount;
            Container = GameObject.FindGameObjectWithTag("EnemyContainer").transform;
        }


        public void StopSpanwer()
        {
            if (spawnLoop != null) StopCoroutine(spawnLoop);
            NetworkQuestEnemyManager.Instance?.UnregisterSpawner(questId, maxSpawnCount);
            QuestSpawnerStop?.Invoke();
        }
        public override void OnStopServer()
        {
            if (spawnLoop != null) StopCoroutine(spawnLoop);
            NetworkQuestEnemyManager.Instance?.UnregisterSpawner(questId, maxSpawnCount);
        }

        private void StartSpawnerServer()
        {
            if (!enemyPrefab)
            {
                enabled = false;
                return;
            }
            NetworkQuestEnemyManager.Instance?.RegisterSpawner(questId, maxSpawnCount);
            if (spawnLoop == null)
                spawnLoop = StartCoroutine(SpawnRoutine());
            isInitialized = false;
        }

        public void StartSpawningServer()
        {
            if (!IsServerInitialized)
                return;
            StartSpawnerServer();
        }

        public void SetDefenceTarget(Transform target)
        {
            defencePriorityTarget = target;
        }

        private System.Collections.IEnumerator SpawnRoutine()
        {
            while (true)
            {
                if (currentSpawned < maxSpawnCount && NetworkQuestEnemyManager.Instance.CanSpawnEnemy(questId))
                {
                    for(int i=0;i<OneTimeSpawnAmount;i++)
                        SpawnEnemy();
                }
                yield return WaitForSecondsCache.Get(spawnInterval);
            }
        }

        private void SpawnEnemy()
        {
            Vector3 pos = questPoint.GetRandomPointInside();
            NetworkObject nob = NetworkManager.GetPooledInstantiated(enemyPrefab,pos, Quaternion.identity, Container, true);
            GameObject go = nob.gameObject;
            
            if (go.TryGetComponent(out EnemyControll ec))
            {
                ec.SetQuestMeta(true, questId, questType, defencePriorityTarget);
                ec.SetNetowrkQuestSpawnerObject(this);
            }
            
            if(go.TryGetComponent(out EnemyStatus status))
            {
                status.SetDataId(targetEnemyDataId);
            }

            nob.gameObject.SetActive(true);
            InstanceFinder.NetworkManager.ServerManager.Spawn(nob);
            currentSpawned++;
            
            
            NetworkQuestEnemyManager.Instance.AddEnemy(questId);
        }
        public void OnChildEnemyDestroyed()
        {
            if (!IsServerInitialized) return;
            currentSpawned = Mathf.Max(0, currentSpawned - 1);
            NetworkQuestEnemyManager.Instance.RemoveEnemy(questId);
        }
    }
}


