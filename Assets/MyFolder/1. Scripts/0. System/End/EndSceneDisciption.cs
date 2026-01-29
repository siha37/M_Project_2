using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._0._System.End
{
    public class EndSceneDisciption : NetworkBehaviour
    {
        [SerializeField] TextMeshProUGUI textMeshProUGUI;

        public override void OnStartServer()
        {
            Discription_Server();    
        }
        
        
        private void Discription_Server()
        {
            Discription_Client(GameSettingManager.Instance.EndDescription);
            textMeshProUGUI.text = GameSettingManager.Instance.EndDescription;   
        }

        [ObserversRpc(ExcludeOwner =  false)]
        private void Discription_Client(string text)
        {
            if(IsServerInitialized)
                return;
            textMeshProUGUI.text = text;
        }
    }
}