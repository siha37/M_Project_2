using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class NetworkQuestEnemyManager : NetworkBehaviour
    {
        private static NetworkQuestEnemyManager instance;
        public static NetworkQuestEnemyManager Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<NetworkQuestEnemyManager>();
                }
                return instance;
            }
        }

        // 서버 전용 퀘스트별 적 수량 관리
        private readonly Dictionary<int, int> questCurrentCounts = new Dictionary<int, int>();
        private readonly Dictionary<int, int> questMaxCounts = new Dictionary<int, int>();

        
        public delegate void vectordel();

        public vectordel enemyRemoveCallback;
        
        public override void OnStartServer()
        {
            questCurrentCounts.Clear();
            questMaxCounts.Clear();
        }

        public void RegisterSpawner(int questId, int max)
        {
            if (!IsServerInitialized) return;
            if (!questCurrentCounts.ContainsKey(questId)) questCurrentCounts[questId] = 0;
            if (!questMaxCounts.ContainsKey(questId)) questMaxCounts[questId] = 0;
            questMaxCounts[questId] += max;
        }

        public void UnregisterSpawner(int questId, int max)
        {
            if (!IsServerInitialized) return;
            if (!questMaxCounts.ContainsKey(questId)) return;
            questMaxCounts[questId] = Mathf.Max(0, questMaxCounts[questId] - max);
        }

        public bool CanSpawnEnemy(int questId)
        {
            if (!questCurrentCounts.ContainsKey(questId) || !questMaxCounts.ContainsKey(questId)) return false;
            return questCurrentCounts[questId] < questMaxCounts[questId];
        }

        public void AddEnemy(int questId)
        {
            if (!IsServerInitialized) return;
            if (!questCurrentCounts.ContainsKey(questId)) questCurrentCounts[questId] = 0;
            questCurrentCounts[questId]++;
        }

        public void RemoveEnemy(int questId)
        {
            if (!IsServerInitialized) return;
            if (!questCurrentCounts.ContainsKey(questId)) return;
            questCurrentCounts[questId] = Mathf.Max(0, questCurrentCounts[questId] - 1);
            enemyRemoveCallback?.Invoke();
        }
    }
}


