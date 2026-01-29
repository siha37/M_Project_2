using FishNet.Object;
using MyFolder._1._Scripts._11._Feel;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI
{
    public class GameStageUI : NetworkBehaviour
    {
        //TIME
        [SerializeField] private TextMeshProUGUI timeText;
        //MAP
        
        //DESTROYER END LIMIT
        [SerializeField] private TextMeshProUGUI limitText;

        private void Start()
        {
            _8._Time.TimeManager.instance.OnTimeChange += TimeUpdate;
        }

        public override void OnStartServer()
        {
        }

        private void TimeUpdate()
        {
            float currentTime = _8._Time.TimeManager.instance.CurrentTime;
            float leftTime = _8._Time.TimeManager.instance.EndTime - currentTime;
            int min = (int)(leftTime / 60);
            int sec = (int)(leftTime % 60);
            string timeString = $"{min:00}:{sec:00}";
            if (leftTime <= 30)
            {
                TimeOut();   
            }
            timeText.text = timeString;
        }

        private void TimeOut()
        {
            Feel_InGame.Instance.GameTimeOut_Start();
        }

        public void DestroyerTimeLimitUpdate(float time)
        {
            int min = (int)(time / 60);
            int sec = (int)(time % 60);
            string timeString = $"{min:00}:{sec:00}";
            limitText.text = timeString;
        }
    }
}