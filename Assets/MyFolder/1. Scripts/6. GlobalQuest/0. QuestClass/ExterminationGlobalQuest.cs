using System;
using FishNet.Object;
using MyFolder._1._Scripts._0._Object._1._Spawner;
using MyFolder._1._Scripts._11._Feel;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public sealed class ExterminationGlobalQuest : GlobalQuestBase
    {
        public override GlobalQuestType Type => GlobalQuestType.Extermination;
        public override bool IsComplete => progress >= target;
        public override bool IsFaile => !IsComplete && currentTime >= limitTime;
        public override string QuestName => "섬멸";

        
        // QuestData 기반 생성자
        public ExterminationGlobalQuest(QuestSpawner spawner, ExterminationQuestData questData,QuestPoint questPoint)
        {
            this.spawner = spawner;
            this.questData = questData; // QuestData 직접 저장
            this.target = questData.target;
            this.progress = questData.progress;
            this.limitTime = questData.limitTime;
            this.waitingTime = questData.waitingTime;
            point = questPoint;
        }
        
        public override void ActiveQuest()
        {
            point.QuestActive(QuestID);
            
            string AlertText = "모든 적을 섬멸하세요";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            
            TimeLimitSoundOff();
            NetworkQuestEnemyManager.Instance.enemyRemoveCallback += enemyCount;
            NetworkQuestEnemySpawner nqes = GlobalQuestManager.instance.GetEnemySpawner(this); 
            if (nqes)
                nqes.StartSpawningServer();
            IsActive = true;
        }


        public override void Complete()
        {
            NetworkQuestEnemyManager.Instance.enemyRemoveCallback -= enemyCount;
            
            string AlertText = "퀘스트 성공";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            point.QuestEnd();
            
            // 퀘스트 카드 시스템 연동
            if (questData != null && QuestCardManager.Instance)
            {
                QuestCardManager.Instance.HandleQuestSuccess(questData);
            }
            
            IsEnd = true;
        }

        public override void Fail()
        {
            NetworkQuestEnemyManager.Instance.enemyRemoveCallback -= enemyCount;
            
            string AlertText = "퀘스트 실패";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            point.QuestEnd();
            
            // 퀘스트 카드 시스템 연동
            if (questData != null && QuestCardManager.Instance)
            {
                QuestCardManager.Instance.HandleQuestFailure(questData);
            }
            
            IsEnd = true;
        }

        public override void Update()
        {
            if(IsEnd)
                return;
            if(!IsActive)
            {
                //대기 시간 연산
                if (waitingTime <= 0)
                {
                    //퀘스트 활성화
                    ActiveQuest();
                    return;
                }
                waitingTime -= Time.deltaTime;
                TimeLimitSoundOn();
                
                return;
            }
            currentTime += Time.deltaTime;
            if(IsComplete)
                Complete();
            if(IsFaile)
                Fail();
        }


        /// <summary>
        /// box 형태 범위 인식으로 변경
        /// </summary>
        /// <param name="position"></param>
        private void enemyCount()
        {
            progress++;
        }
    }
}
