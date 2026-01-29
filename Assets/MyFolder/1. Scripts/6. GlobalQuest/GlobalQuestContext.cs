using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest
{

    public sealed class GlobalQuestContext
    {
        public QuestSpawner Spawner;
        public GlobalQuestType type;
        public float targetAmount;
        public float progressAmount;
        public int defenceAmount;
        public float limitTime;
        public float waitingTime;
        public Vector3 position;
        public float distance;
        public float minusProgress;
        public float minusTiming;
        public float minusMutiple;
        // 섬멸용
        public GlobalQuestContext(
            QuestSpawner spawner,
            float targetAmount,
            float limitTime,
            Vector3 position,
            float distance )
        {
            Spawner = spawner;
            this.targetAmount = targetAmount;
            this.limitTime = limitTime;
            this.position = position;
            this.distance = distance;
        }
        // 방어용
        public GlobalQuestContext(
            QuestSpawner spawner,
            float targetAmount,
            int defenceAmount,
            float limitTime)
        {
            Spawner = spawner;
            this.defenceAmount = defenceAmount;
            this.targetAmount = targetAmount;
            this.defenceAmount = defenceAmount;
            this.limitTime = limitTime;
        }
        //생존
        public GlobalQuestContext(
            QuestSpawner spawner,
            float targetAmount,
            float progressAmount,
            float limitTime,
            float minusMutiple,
            float minusProgress,
            float minusTiming)
        {
            Spawner = spawner;
            this.targetAmount = targetAmount;
            this.progressAmount = progressAmount;
            this.limitTime = limitTime;
            this.minusMutiple = minusMutiple;
            this.minusProgress = minusProgress;
            this.minusTiming = minusTiming;
        }
        public GlobalQuestContext(QuestSpawner spawner, int targetAmount,float limitTime,int defenceAmount )
        {
            Spawner = spawner;
            this.defenceAmount = defenceAmount;
            this.targetAmount = targetAmount;
            this.limitTime = limitTime;
        }
        public GlobalQuestContext(QuestSpawner spawner, int targetAmount, float limitTime)
        {
            Spawner = spawner;
            this.targetAmount = targetAmount;
            this.limitTime = limitTime;
        }
        public GlobalQuestContext(QuestSpawner spawner, float limitTime, int defenceAmount)
        {
            Spawner = spawner;
            this.defenceAmount = defenceAmount;
            this.limitTime = limitTime;
        }
        public GlobalQuestContext(QuestSpawner spawner,float limitTime)
        {
            Spawner = spawner;
            this.limitTime = limitTime;
        }
    }
}