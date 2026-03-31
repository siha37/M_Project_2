using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._3._SingleTone.GameSetting;
using UnityEngine;
using VInspector;

namespace MyFolder._1._Scripts._7._PlayerRole
{
    public class DamageStackManager : NetworkBehaviour
    {
        private static DamageStackManager instance;

        public static DamageStackManager Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<DamageStackManager>();

                    if (instance) return instance;
                    
                    GameObject obj = new GameObject
                    {
                        name = nameof(DamageStackManager)
                    };
                    instance = obj.AddComponent<DamageStackManager>();
                }
                return instance;
            }
        }

        private bool accessAble = false;
        public Action<bool> access_OnChanged;
        
        GameDataManager gameDataManager;

        [SerializeField] private ushort Level = 1;
        [SerializeField][ReadOnly] private float Damage;
        
        private Dictionary<ushort,float> DamageList;
        private DamageStackRatio stackRatio;
        
        private readonly SyncVar<int> DestroyerAmount = new SyncVar<int>();
        private readonly SyncVar<float> DamageAmount = new SyncVar<float>();
        
        public override void OnStartServer()
        {
            Damage = 0;
            DamageAmount.Value = Damage;
            
            //1
            Set_DestroyerAmount();
            //2
            Set_DamageStackList();
        }

        public override void OnStartClient()
        {
            PlayerSettingManager.PlayerSettings setting = PlayerSettingManager.Instance.GetLocalPlayerSettings();
            if (setting.role == PlayerRoleType.Destroyer)
            {
                accessAble = true;
                access_OnChanged?.Invoke(accessAble);
                DamageAmount.OnChange += Damage_OnChanged;
            }
            else
            {
                accessAble = false;
                access_OnChanged?.Invoke(accessAble);
            }
        }


        #region OnlyServer

        private void Set_DamageStackList()
        {
            var Data = GameDataManager.Instance.GetDamageStackData();
            stackRatio = GameDataManager.Instance.GetDamageStackRatio();
            DamageList = new Dictionary<ushort,float>();
            foreach (var damageStack in Data)
            {
                DamageList.Add(damageStack.Key, damageStack.Value.stack_amount * DestroyerAmount.Value);
            }
        }
        
        private void Set_DestroyerAmount()
        {
            DestroyerAmount.Value = GameSettingManager.Instance.GetCurrentSettings()
                .PlayerRoleSettings[PlayerRoleType.Destroyer].RoleAmount;
        }
        
        [ServerRpc(RequireOwnership = false)]
        private void DamageApply_ServerRPC(float amount)
        {
            Damage += amount;
            DamageAmount.Value = Damage;
            DamageAmount_Check();
        }
        
        private void DamageAmount_Check()
        {
            if (DamageList.TryGetValue(Level, out var target))
            {
                if (target <= Damage)
                {
                    Damage = 0;
                    DamageAmount.Value = 0;
                    Level_Update();
                    
                    // 다음 레벨 존재 안함
                    if (!DamageList.ContainsKey(Level))
                    {
                        DamageStackSystem_Lock();
                    }
                }                
            }
        }
        
        private void Level_Update()
        {
            Level++;
            SpawnerManager.instance.EnemyLevel_Up();
        }

        private void DamageStackSystem_Lock()
        {
            DamageStackSystem_Lock_ClientRPC();
        }

        [TargetRpc]
        public void DamageApply_Request_TargetRPC(NetworkConnection conn, string target_tag,float amount)
        {
            switch (target_tag)
            {
                case "Player":
                    DamageApply_isPlayer_Request(amount);
                    break;
                case "Enemy":
                    DamageApply_isEnemy_Request(amount);
                    break;
            }
        }
        
        public void DamageApply_Quest_Request(int amount)
        {
            if(!IsServerInitialized)
                return;
            DamageApply_Request(amount);
        }
        
        #endregion

        #region OnlyClient

        
        [ObserversRpc]
        private void DamageStackSystem_Lock_ClientRPC()
        {
            accessAble = false;
            access_OnChanged?.Invoke(accessAble);
        }

        #endregion

        #region Both

        
        // 데미지 누적 요청 (플레이어)
        public void DamageApply_isPlayer_Request(float amount)
        {
            DamageApply_Request(amount * stackRatio.isPlayerRatio);
        }
        
        // 데미지 누적 요청 ( 적군 )
        public void DamageApply_isEnemy_Request(float amount)
        {
            DamageApply_Request(amount * stackRatio.isEnemyRatio);
        }

        // 데미지 누적 요청 ( 통합 함수 )
        private void DamageApply_Request(float amount)
        {
            if(!accessAble) return;
            DamageApply_ServerRPC(amount);
        }

        // 값 변경 콜백
        public Action<float,float> Damage_OnChanged_Callback;
        private void Damage_OnChanged(float oldValue, float newValue, bool isOwner)
        {
            Damage_OnChanged_Callback?.Invoke(DamageList[Level],newValue);
            // UI 최신화 작업 진행
        }
        
        #endregion


    }
}