using System.Security.Cryptography;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;
using MyFolder._1._Scripts._6._GlobalQuest._2._Data;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest._0._QuestClass
{
    public abstract class GlobalQuestBase
    {
        //현 타입
        public abstract GlobalQuestType Type { get; }
        public abstract string QuestName { get; }
        public int QuestID { get; set; }
        
        //Data 연동값
        protected QuestSpawner spawner;
        protected QuestData questData; // 퀘스트 데이터 참조 추가
        protected QuestPoint point; 
        public QuestData QuestData => questData; 
        public QuestPoint Point => point; 
        public float limitTime;
        public float waitingTime = 5;
        public float target;
        public float progress;
        
        
        
        //퀘스트 완료 여부
        public abstract bool IsComplete { get; }
        public abstract bool IsFaile { get; }
        public bool IsActive = false;
        public bool IsEnd = false;
        public bool IsLimit = false;
        
        public float currentTime = 0f;
        public int MarkId;

        //생성자
        public GlobalQuestBase() { }
        
        public abstract void Complete();
        public abstract void Fail();
        public abstract void Update();
        public abstract void ActiveQuest();

        protected void TimeLimitSoundOn()
        {
            if (!IsLimit)
            {
                if (waitingTime <= 10)
                {
                    IsLimit = true;
                    GameSystemSound.Instance.Quest_TimeLimitSFX_Start();
                }
            }
        }

        protected void TimeLimitSoundOff()
        {
            if (IsLimit)
            {
                GameSystemSound.Instance.Quest_TimeLimitSFX_End();
            }
        }

    }
}
