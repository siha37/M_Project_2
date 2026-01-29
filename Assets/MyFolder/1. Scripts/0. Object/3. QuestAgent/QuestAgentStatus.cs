using System.Collections;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._3._QuestAgent
{
    public class QuestAgentStatus : AgentStatus
    {
        private static readonly int Die = Animator.StringToHash("DIE");

        // 퀘스트 에이전트는 특별한 데이터 타입이 없다면 기본 AgentData 사용
        public AgentData QuestAgentData => data as AgentData;
        

        [SerializeField] private QuestObjSfx sfx; 
        // 스포너 생성자가 호출
        public void InitializeData(ushort spawnerID)
        {
            SetDataId(spawnerID);
            
            if (CanLoadData())
            {
                LoadQuestAgentData();
            }
            else
            {
                // 기본값으로 초기화
                data = CreateDefaultAgentData();
                
                // 나중에 데이터 로딩 시도
                RegisterDataLoadCallbacks();
            }
        }
        
        protected virtual void LoadQuestAgentData() { }


        private void RegisterDataLoadCallbacks()
        {
            if (GameDataManager.Instance)
            {
                StartCoroutine(CheckDataPeriodically());
            }
        }

        private IEnumerator CheckDataPeriodically()
        {
            while (!_dataLoaded && !_isLoadingData)
            {
                yield return WaitForSecondsCache.Get(0.5f);
                
                if (CanLoadData())
                {
                    LoadQuestAgentData();
                    break;
                }
            }
        }
        
        public void OnDeathEffectAnim()
        {
            StartCoroutine(nameof(DeathSequence));
        }
        
        /// <summary>
        /// 기절 상태 - 색상만 변경
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator DeathSequence()
        {
            yield return WaitForSecondsCache.Get(2f);
            yield return base.DeathSequence();
        }
        public override bool TakeDamage(float damage, Vector2 hitDirection = default)
        {
            if (isDead) return false;
        
            base.TakeDamage(damage, hitDirection);
        
            if (currentHp <= 0)
            {
                isDead = true;
                sfx.Play(QuestObjSfxType.Broken);
            }

            return false;
        }
    }
}