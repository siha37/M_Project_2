using System;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._3._QuestAgent;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    public abstract class QuestSpawner
    {
        // 섬멸 : 딱히 필요없음
        // 방어 : 방어 오브젝트 생성 /
        // 생존 : 생존 유지 오브젝트 생성
        
        // 절차
        // 1. 생성 오브젝트 갯수 설정 / 생성 프리펩 설정
        // 2. 생성 위치 선정
        // 3. 전체 생성 or 순차적 생성

        protected abstract int createAmount { get; }
        private NetworkObject questPrefab;
        private QuestPoint questPoint;
        public Vector3 spawnPoint;
        private List<NetworkObject> spawnedObjects = new List<NetworkObject>();
        private ushort spawnedObjectDataID = 1;
        
        private int createStep;
        
        
        public event Action<GameObject> OnSpawned;
        public event Action<GameObject> OnDespawned;

        protected void RaiseSpawned(GameObject go)   => OnSpawned?.Invoke(go);
        protected void RaiseDespawned(GameObject go) => OnDespawned?.Invoke(go);

        private int currentCreateAmount => spawnedObjects.Count;
        public int CurrentCreateAmount=> currentCreateAmount;
        public void SetSpawnPoints(QuestPoint point)
        {
            this.spawnPoint = point.Point;
            questPoint = point;
        }

        public void SetSpawnPrefab(NetworkObject spawnPrefab)
        {
            this.questPrefab = spawnPrefab;
        }

        public void SetObjectID(ushort typeId)
        {
            spawnedObjectDataID = typeId;
        }
        
        #region Spawn

        public abstract void SpawnStart();
        protected void AllSpawn()
        {
            System.Random rand = new System.Random();
            List<Transform> shuffled = questPoint.SubPoints.OrderBy(_ => rand.Next()).ToList();
            int count = Mathf.Clamp(createAmount, 0, shuffled.Count);
            spawnedObjects.Clear();
            
            for (int i = 0; i < count; i++)
            {
                NetworkObject nob = Object.Instantiate(questPrefab,shuffled[i].position, Quaternion.identity);
                //obj.transform.SetParent(transform);
                if (nob.TryGetComponent(out QuestAgentControll controll))
                    controll.SetMyMother(this);
                if (nob.TryGetComponent(out QuestAgentStatus status))
                    status.InitializeData(spawnedObjectDataID);
                InstanceFinder.ServerManager.Spawn(nob);
                nob.gameObject.SetActive(true);
                RaiseSpawned(nob.gameObject);
                spawnedObjects.Add(nob);
            }
        }
        public void AllRemove()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                NetworkObject obj = spawnedObjects[i];
                if (obj)
                {
                    if (obj.IsSpawned)
                        InstanceFinder.ServerManager.Despawn(obj);
                    RaiseDespawned(obj.gameObject);
                    Object.Destroy(obj.gameObject);
                }
            }
            spawnedObjects.Clear();
        }

        #endregion

        #region Controll

        public void DestroyObject(NetworkObject obj)
        {
            RaiseDespawned(obj.gameObject);
            spawnedObjects.Remove(obj);
        }

        #endregion

    }
}