using MyFolder._1._Scripts._3._SingleTone;

namespace MyFolder._1._Scripts._0._Object._3._QuestAgent
{
    public class DefenceQuestAgentStatus : QuestAgentStatus
    {
        protected override void LoadQuestAgentData()
        {
            if (_dataLoaded || _isLoadingData) return;
            
            _isLoadingData = true;
            
            try
            {
                // 스포너 전용 데이터가 있다면 로드, 없다면 기본 AgentData 사용
                var questAgentData = GameDataManager.Instance.GetDefenceQuestObjectDataById(GetDataId());
                
                if (questAgentData != null)
                {
                    data = questAgentData;
                    
                    _dataLoaded = true;
                    
                    LogManager.Log(LogCategory.Quest, $"{gameObject.name} 퀘스트 에이전트 데이터 로딩 완료", this);
                }
            }
            catch (System.Exception ex)
            {
                LogManager.LogError(LogCategory.Quest, $"{gameObject.name} 퀘스트 에이전트 데이터 로딩 실패: {ex.Message}", this);
            }
            finally
            {
                _isLoadingData = false;
            }
        }
    }
}