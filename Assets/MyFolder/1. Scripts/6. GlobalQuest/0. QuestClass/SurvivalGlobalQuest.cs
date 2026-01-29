using MyFolder._1._Scripts._0._Object._1._Spawner;
using MyFolder._1._Scripts._0._Object._3._QuestAgent;
using MyFolder._1._Scripts._11._Feel;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public class SurvivalGlobalQuest : GlobalQuestBase
    {
        
        //Data 연동값
        public float minusProgress = 1f;
        public float minusTiming = 1f;
        public float minusMutiple = 1.3f;
        
        
        public override GlobalQuestType Type => GlobalQuestType.Survival;
        public override bool IsComplete => progress> 0 && currentTime >= limitTime;
        public override bool IsFaile => currentTime < limitTime && progress <= 0;
        public override string QuestName => "생존";
        
        private SurvivalQuestData survivalQuestData => questData as SurvivalQuestData;


        protected float minusTime = 0f;
        
        // QuestData 기반 생성자
        public SurvivalGlobalQuest(QuestSpawner spawner, SurvivalQuestData questData,QuestPoint questPoint)
        {
            this.spawner = spawner;
            this.questData = questData; // QuestData 직접 저장
            limitTime = questData.limitTime;
            waitingTime = questData.waitingTime;
            progress = questData.progress;
            target = questData.target;
            minusMutiple = questData.minusMutiple;
            minusProgress = questData.minusProgress;
            minusTiming = questData.minusTiming;
            point = questPoint;
            
        }
        public override void ActiveQuest()
        {
            TimeLimitSoundOff();
            
            
            string AlertText = "수치를 유지하세요";
            Feel_InGame.Instance.AlertFeel_Start(AlertText);
            
            spawner.OnDespawned += OnObjectiveDespawned;
            spawner.SetObjectID(survivalQuestData.surviverTargetID);
            spawner.SpawnStart();
            
            point.QuestActive(QuestID);
            NetworkQuestEnemyManager.Instance.enemyRemoveCallback += OnEnemyKilled; // 처치 보상
            NetworkQuestEnemySpawner nqes = GlobalQuestManager.instance.GetEnemySpawner(this); 
            if (nqes)
                nqes.StartSpawningServer();
            
            IsActive = true;
        }


        public override void Complete()
        {
            NetworkQuestEnemyManager.Instance.enemyRemoveCallback -= OnEnemyKilled; // 처치 보상
            
            
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
            NetworkQuestEnemyManager.Instance.enemyRemoveCallback -= OnEnemyKilled; // 처치 보상
            
            
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

                TimeLimitSoundOn();
                waitingTime -= Time.deltaTime;
                return;
            }
            currentTime += Time.deltaTime;
            minusTime+= Time.deltaTime;
            if (minusTime >= minusTiming)
            {
                minusTime = 0f;
                progress = Mathf.Clamp(progress - minusProgress, 0, target);
            }
            
            progress = Mathf.Max(0f, progress);
            if(IsComplete)
                Complete();
            if(IsFaile)
                Fail();
        }
        
        /// <summary>
        /// 적군 AI 사망 시
        /// </summary>
        private void OnEnemyKilled()
        {
            if (Random.value < 0.3f)
                progress += 4;
        }
        
        /// <summary>
        /// 생명 유지 장치 파괴 시 
        /// </summary>
        /// <param name="go"></param>
        private void OnObjectiveDespawned(GameObject go)
        {
            minusProgress *= minusMutiple;
        }
    }
}
