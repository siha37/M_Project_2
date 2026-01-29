using System;
using MyFolder._1._Scripts._6._GlobalQuest;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest
{
    public abstract class QuestPanel : MonoBehaviour
    {
        [SerializeField] protected TextMeshProUGUI nameText;
        [SerializeField] protected TextMeshProUGUI timeText;
        [SerializeField] protected Image progressImage;
        [SerializeField] protected GlobalQuestReplicator globalQuestReplicator;
        
        protected float progress = 0f;
        protected float target = 0f;
        protected float waitTime = float.MaxValue;
        protected float elapsedTime = 0f;
        protected float limitTime = 0f;
        
        public virtual void Initialize(GlobalQuestReplicator rep)
        {
            nameText.text  = rep.QuestName.Value;
            limitTime  = rep.LimitTime.Value;
            target = rep.Target.Value;
            progress = rep.Progress.Value;
            elapsedTime = rep.ElapsedTime.Value;
            waitTime = rep.WaitingTime.Value;
            globalQuestReplicator = rep;
            
            OnEvent();
        }

        public void OnDestroy()
        {
            OffEvent();
        }

        public virtual void ProgressUpdate()
        {
            progressImage.fillAmount = progress / target;
        }

        public void TimeUpdate()
        {
            if (waitTime > 0)
            {
                timeText.color = Color.yellow;
                timeText.text = waitTime.ToString("00:00");
                return;
            }
            
            timeText.color = limitTime - elapsedTime <= 15f ? Color.red : Color.white;
            timeText.text = (limitTime-elapsedTime).ToString("00:00");
        }

        public void OnEvent()
        {
            globalQuestReplicator.WaitingTime.OnChange += WaitTimeChanged;
            globalQuestReplicator.Progress.OnChange += ProgressChanged;
            globalQuestReplicator.Target.OnChange += TargetChanged;
            globalQuestReplicator.ElapsedTime.OnChange += ElapsedTimeChanged;
        }

        public void OffEvent()
        {   
            globalQuestReplicator.WaitingTime.OnChange -= WaitTimeChanged;
            globalQuestReplicator.Progress.OnChange -= ProgressChanged;
            globalQuestReplicator.Target.OnChange -= TargetChanged;
            globalQuestReplicator.ElapsedTime.OnChange -= ElapsedTimeChanged;
        }

        protected void WaitTimeChanged(float oldValue, float newValue, bool asServer)
        {
            waitTime = newValue;
            TimeUpdate();
        }

        protected void ProgressChanged(float oldValue, float newValue, bool asServer)
        {
            progress = newValue;
            ProgressUpdate();
        }

        protected void TargetChanged(float oldValue, float newValue, bool asServer)
        {
            target = newValue;
            ProgressUpdate();
        }

        protected void ElapsedTimeChanged(float oldValue, float newValue, bool asServer)
        {
            elapsedTime = newValue;
            TimeUpdate();
        }

    }
}