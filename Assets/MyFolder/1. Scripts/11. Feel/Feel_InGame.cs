using FishNet.Object;
using MoreMountains.Feedbacks;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._7._PlayerRole;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._11._Feel
{
    public sealed class Feel_InGame : NetworkBehaviour
    {
        private static Feel_InGame instance;
        
        //-----------------------------------------

        public static Feel_InGame Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<Feel_InGame>();
                }
                return instance;
            }
        }

        private void Start()
        {
            if (!instance)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            switch (PlayerSettingManager.Instance.GetLocalPlayerSettings().role)
            {
                case PlayerRoleType.Destroyer:
                    DestoryerTargetFeel_Start();
                    break;
                case PlayerRoleType.Normal:
                    NormalTargetFeel_Start();
                    break;
            }
        }

        //-----------------------------------------
        
        [SerializeField] MMFeedbacks AlertFeel;
        [SerializeField] TextMeshProUGUI AlertText;
        
        public void AlertFeel_Start(string text)
        {
            AlertText.text = text;
            AlertFeel?.PlayFeedbacks();
            AlertFeel_Start_Client(text);
        }

        [ObserversRpc]
        public void AlertFeel_Start_Client(string text)
        {
            if(IsServerInitialized)
                return;
            AlertText.text = text;
            AlertFeel?.PlayFeedbacks();
        }
        
        //-----------------------------------------
        
        [SerializeField] MMFeedbacks CardTimeOutFeel;

        public void CardTimeOutFeel_Start()
        {
            CardTimeOutFeel?.PlayFeedbacks();
        }

        public void CardTimeOutFeel_Stop()
        {
            CardTimeOutFeel?.StopFeedbacks();
        }

        //-----------------------------------------
        [SerializeField] MMFeedbacks GameTimeOutFeel;
        public void GameTimeOut_Start()
        {
            GameTimeOutFeel?.PlayFeedbacks();
            GameTimeOut_Start_Client();
        }
        
        [ObserversRpc]
        public void GameTimeOut_Start_Client()
        {
            GameTimeOutFeel?.PlayFeedbacks();
        }
        //-----------------------------------------
        
        [SerializeField] MMFeedbacks NormalTargetFeel;
        public void NormalTargetFeel_Start()
        {
            NormalTargetFeel?.PlayFeedbacks();
        }
        
        //-----------------------------------------
        [SerializeField] MMFeedbacks DestoryerTargetFeel;

        public void DestoryerTargetFeel_Start()
        {
            DestoryerTargetFeel?.PlayFeedbacks();
        }
        
        //-----------------------------------------
        [SerializeField] MMFeedbacks On_DestroyerEndLimitUIFeel;
        [SerializeField] MMFeedbacks Off_DestroyerEndLimitUIFeel;

        public void On_DestroyerEndLimitUIFeel_Start()
        {
            On_DestroyerEndLimitUIFeel?.PlayFeedbacks();
        }

        public void Off_DestroyerEndLimitUIFeel_Start()
        {
            Off_DestroyerEndLimitUIFeel?.PlayFeedbacks();
        }
    }
}
