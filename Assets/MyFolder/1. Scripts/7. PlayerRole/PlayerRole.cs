using FishNet.Object;
using MyFolder._1._Scripts._0._Object._0._Agent;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEditor;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    public class PlayerRole : NetworkBehaviour
    {
        private PlayerControll controll;
        private PlayerInputControll inputControll;
        private PlayerUI playerUI;
        private PlayerStatus state;
        private PlayerRoleType type;
        private PlayerRoleDefinition definition;

        public override void OnStartClient()
        {
            if(IsOwner)
            {
                TryGetComponent(out inputControll);
                TryGetComponent(out state);
                TryGetComponent(out playerUI);
                if(!TryGetComponent(out controll))
                {
                    LogManager.LogError(LogCategory.System,$"controll이 없습니다 {controll}");
                }
                else
                {
                    if (!PlayerSettingManager.Instance || ! GameDataManager.Instance)
                    {
                        LogManager.LogError(LogCategory.System, $"{gameObject.name} : PlayerRoleManager is Null");
                    }
                    else
                    {
                        PlayerSettingManager.PlayerSettings settings = PlayerSettingManager.Instance.GetLocalPlayerSettings();
                        //세팅이 없을 경우
                        if (settings == null)
                        {
                            LogManager.Log(LogCategory.Player, $"{gameObject.name} : PlayerSetting is Null",this);
                            type = PlayerRoleType.Normal;
                            definition = GameDataManager.Instance.GetPlayerRoleDefinitionByRole(type);
                            SetSkill();
                            SetIcon();
                        }
                        //세팅 성공
                        else
                        {
                            type = PlayerSettingManager.Instance.GetLocalPlayerSettings().role;
                            definition = GameDataManager.Instance.GetPlayerRoleDefinitionByRole(type);
                            SetSkill();
                            SetIcon();   
                        }
                    }
                }
            }
        }

        private void SetSkill()
        {
            inputControll.IsActive_skill_1 = definition?.CanUseSkill1 ?? false;
        }

        private void SetIcon()
        {
            playerUI.SetRoleImage(type);
        }
    }
}