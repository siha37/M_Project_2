using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using MyFolder._1._Scripts._7._PlayerRole;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class PlayerRoleManager : NetworkBehaviour
    {
        public static PlayerRoleManager instance { get; private set; }
        private void Awake()
        {
            instance = this;
        }        
        public static PlayerRoleManager Instance
        {
            get
            {
                if (!instance)
                    instance = FindFirstObjectByType<PlayerRoleManager>();
                return instance;
            }
        }
        
        [SerializeField] private List<PlayerRoleDefinition> roleDefinitions;

        [SerializeField] private List<PlayerRoleType> seeRoles = new(); // key: ClientId
        
        // 역할 배정 완료 이벤트
        private bool readyRole = false;

        
        /// <summary>
        /// 모든 플레이어에게 역할 동시 배정 (서버에서 호출)
        /// </summary>
        public void AssignRolesToAllPlayers()
        {
            if (!IsServerInitialized) return;
            
            PlayerSettingManager playerSettingManager = PlayerSettingManager.Instance;
            if(!playerSettingManager)
            {
                LogManager.LogError(LogCategory.System, "PlayerSettingManager 인스턴스 없음!!", this);
                return;
            }
            
            LogManager.Log(LogCategory.System, "모든 플레이어에게 역할 배정 시작", this);
            
            // 기존 할당 초기화
            seeRoles.Clear();
            
            // GameSettingManager에서 역할 설정 가져오기
            GameSettings gameSettings = GameSettingManager.Instance.GetCurrentSettings();
            Dictionary<PlayerRoleType, PlayerRoleSettings> roleSettings = gameSettings.PlayerRoleSettings;
            
            // 현재 연결된 모든 클라이언트 가져오기
            Dictionary<int,NetworkConnection> connectedClients = NetworkManager.ServerManager.Clients;
            
            // 역할 풀 생성 (설정된 수량만큼)
            List<PlayerRoleType> rolePool = new List<PlayerRoleType>();
            foreach (var roleSetting in roleSettings)
            {
                for (int i = 0; i < roleSetting.Value.RoleAmount; i++)
                {
                    rolePool.Add(roleSetting.Value.RoleType);
                }
            }

            if (rolePool.Count < connectedClients.Count)
            {
                for (int i = rolePool.Count; i < connectedClients.Count; i++)
                {
                    rolePool.Add(PlayerRoleType.Normal);
                }
            }
            // 역할 풀을 랜덤하게 섞기
            rolePool = rolePool.OrderBy(x => Random.Range(0f, 1f)).ToList();
            
            // 각 클라이언트에게 역할 배정
            int roleIndex = 0;
            foreach (KeyValuePair<int,NetworkConnection> client in connectedClients)
            {
                
                if (roleIndex < rolePool.Count)
                {
                    RoleApply(client.Value.ClientId,rolePool[roleIndex]);
                    roleIndex++;
                }
                // 역할이 부족한 경우 기본 역할 배정
                else
                {
                    RoleApply(client.Value.ClientId,PlayerRoleType.Normal);
                }
            }
            
            LogManager.Log(LogCategory.System, $"총 {seeRoles.Count}명의 플레이어에게 역할 배정 완료", this);
            
            // 준비 완료상태 전환
            readyRole = true;
        }

        /// <summary>
        /// 지정한 클라이언트에게 해당 타입의 역할을 지정
        /// </summary>
        /// <param name="ClientId">지정하고자 하는 클라이언트 아이디</param>
        /// <param name="assignedRole">지정하고자하는 역할</param>
        private void RoleApply(int ClientId,PlayerRoleType assignedRole)
        {
            PlayerSettingManager.Instance.SetPlayerRoleServerRpc(ClientId, assignedRole);
                    
            seeRoles.Add(assignedRole);
                    
            LogManager.Log(LogCategory.System, $"클라이언트 {ClientId}에게 역할 {assignedRole} 배정됨", this);

        }
        

        /// <summary>
        /// 역할 배정이 완료되었는지 확인
        /// </summary>
        public bool AreRolesAssigned()
        {
            return readyRole;
        }
    }
}