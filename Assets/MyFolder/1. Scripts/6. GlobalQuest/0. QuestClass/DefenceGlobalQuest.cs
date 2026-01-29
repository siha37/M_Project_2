using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._Object._1._Spawner;
using MyFolder._1._Scripts._0._Object._3._QuestAgent;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._11._Feel;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public class DefenceGlobalQuest : GlobalQuestBase
    {
        private List<QuestAgentStatus> statuss = new();

        public List<QuestAgentStatus> Statuses => statuss;
        // QuestData 기반 생성자
        public DefenceGlobalQuest(QuestSpawner spawner, DefenceQuestData questData,QuestPoint questPoint)
        {
            this.spawner = spawner;
            this.questData = questData; // QuestData 직접 저장
            limitTime = questData.limitTime;
            waitingTime = questData.waitingTime;
            target = questData.target;
            progress = questData.progress;
            point = questPoint;
        }
        public override GlobalQuestType Type => GlobalQuestType.Defense;
        public override bool IsComplete => IsActive && currentTime >= limitTime;
        public override bool IsFaile => IsActive && currentTime < limitTime && statuss?.Count <= 0;
        public override string QuestName => "방어";
        
        private DefenceQuestData defenceQuestData => questData as DefenceQuestData;

        public override void Complete()
        {
            
            string AlertText = "퀘스트 성공";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            
            point.QuestEnd();
            spawner.AllRemove();
            // 퀘스트 카드 시스템 연동
            if (questData != null && QuestCardManager.Instance)
            {
                QuestCardManager.Instance.HandleQuestSuccess(questData);
            }
            
            IsEnd = true;
        }

        public override void Fail()
        {
            string AlertText = "퀘스트 실패";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            point.QuestEnd();
            spawner.AllRemove();
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
            
            float maxHP=0,progressHP=0;
            for (int i = 0; i < statuss.Count; i++)
            {
                try
                {
                    if(statuss[i]){}
                }
                catch (Exception e)
                {
                    LogManager.LogError(LogCategory.Quest,e.ToString());
                    continue;
                }
                maxHP+=statuss[i].Data.hp;
                progressHP += statuss[i].currentHp;
            }

            if(maxHP!=0) target = maxHP;
            progress = progressHP;
            
            //방어 오브젝트가 한번도 등록된 적 없음
            if(target == 0)
            {
                return;
            }
            
            if(IsComplete)
                Complete();
            if(IsFaile)
                Fail();
        }

        public override void ActiveQuest()
        {
            point.QuestActive(QuestID);
            
            string AlertText = "장승을 보호하세요";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            
            TimeLimitSoundOff();
            spawner.OnSpawned += OnObjectiveSpawned;
            spawner.OnDespawned += OnObjectiveDespawned;
            spawner.SetObjectID(defenceQuestData.defenceTargetID);
            spawner.SpawnStart();
            
        }


        private void OnObjectiveSpawned(GameObject go)
        {
            if (go.TryGetComponent(out QuestAgentStatus hp))
            {
                statuss.Add(hp);
                IsActive = true;
                // 방어형: 방어 오브젝트가 등록되는 순간에 적 스포너 시작
                NetworkQuestEnemySpawner nqes = GlobalQuestManager.instance.GetEnemySpawner(this); 
                if (nqes)
                {
                    nqes.SetDefenceTarget(hp.transform);
                    nqes.StartSpawningServer();
                }
            }
        }
        private void OnObjectiveDespawned(GameObject go)
        {
            if (go.TryGetComponent(out QuestAgentStatus hp))
            {
                statuss.Remove(hp);
            }
        }
    }
}
