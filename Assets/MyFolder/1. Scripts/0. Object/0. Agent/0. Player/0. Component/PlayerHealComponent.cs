using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    public class PlayerHealComponent : IPlayerUpdateComponent
    {
        private PlayerContext context;
        private PlayerCamouflageComponent camouflage;
        private PlayerInteractController interaction;
        private float healAmount;
        private bool Healing =false;
        private const float healLimit = 1;
        private float currentHealTime = 0;
        private float entryDelay = 2;
        private float exitDelay = 2;
        private float currentEntryDelay = 0;
        private float currentExitDelay = 0;
        private bool isInExitDelay = false;
        private bool isInEnterDelay = false;

        public bool headling => Healing;
        public void Start(PlayerContext context)
        {
            this.context = context;
            if (context.Status.DataLoaded)
                healAmount = this.context.Status.PlayerData.healAmount;
        }

        public void Stop()
        {
            context.Input.heal_Callback -= KeyEnter;
        }

        public void SetKeyEvent(PlayerInputControll inputControll)
        {
            inputControll.heal_Callback += KeyEnter;
        }

        public void KeyEnter()
        {
            //위장 중일 시 사용 불가
            if (camouflage == null)
                camouflage = context.Component.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            if(camouflage is { IsDisguised: true })
                return;
            // 부활 시도 중 사용 불가
            if(context.PlayerInteract.isActive)
                return;
            
            // 딜레이 중에는 입력 무시
            if (isInExitDelay)
                return;
            
            
            // 서버
            if (context.Sync.IsServerInitialized)
            {
                if (Healing)
                {
                    context.Sync.OffHeal_Client();
                }
                else
                {
                    context.Sync.OnHeal_Client();
                }
            }
            // 클라이언트
            else
            {
                if (Healing)
                {
                    context.Sync.OffHeal_Server();
                }
                else
                {
                    context.Sync.OnHeal_Server();
                }
            }
        }
        
        
        public void StartHealing()
        {
            if (context.Status.DataLoaded)
                healAmount = context.Status.PlayerData.healAmount;
            context.AgentUI.StartOnHealProgress();
            Healing = true;
            currentEntryDelay = 0;
            isInEnterDelay = true;
            isInExitDelay = false;
            OnHeal();
        }

        public void StopHealing()
        {
            Healing = false;
            isInExitDelay = true;
            context.AgentUI.StartOffHealProgress();
            currentExitDelay = 0;
            context.HealVfx.Stop();
        }
        private void OnHeal()
        {
            // 제약 활성화
            currentHealTime = 0;
            context.Controller.IsMovable = false;
            context.Shooter.OnAttack = false;
            context.Controller.MoveStop();
            
            //
            context.HealVfx.Play();
            DisableShield();
        }

        private void OffHeal()
        {
            // 제약 비활성화
            context.Controller.IsMovable = true;
            context.Shooter.OnAttack = true;
            
            EnableShield();
        }
        #region 방패 관리
        
        private void DisableShield()
        {
            if (context.DefenceBall)
            {
                context.DefenceBall.gameObject.SetActive(false);
            }
        }
        
        private void EnableShield()
        {
            if (context.DefenceBall)
            {
                // ✅ 방패가 깨지지 않았을 경우에만 활성화
                if (!context.Status.IsCrackDefence)
                {
                    context.DefenceBall.gameObject.SetActive(true);
                }
            }
        }
        
        #endregion
        
        public void Update()
        {
            // 종료 딜레이 (모든 클라이언트 로컬 처리)
            if (isInExitDelay)
            {
                currentExitDelay += Time.deltaTime;
                context.AgentUI.UpdateOffHealProgress(currentExitDelay/exitDelay);
                if (currentExitDelay >= exitDelay)
                {
                    isInExitDelay = false;
                    context.AgentUI.EndOffHealProgress();
                    OffHeal();
                }
                return;
            }

            // 힐링 중
            if (Healing)
            {
                // 시작 딜레이 (모든 클라이언트 로컬 처리)
                if (currentEntryDelay < entryDelay)
                {
                    currentEntryDelay += Time.deltaTime;
                    context.AgentUI.UpdateOnHealProgress(currentEntryDelay/entryDelay);
                    return; // ✅ 딜레이 중 치유 안 함
                }
                
                if(isInEnterDelay)
                {
                    isInEnterDelay = false;
                    context.AgentUI.EndOnHealProgress();
                }

                // ✅ 실제 치유는 서버에서만
                if (context.Sync.IsServerInitialized)
                {
                    if (currentHealTime >= healLimit)
                    {
                        currentHealTime = 0;
                        if(!context.Status.Heal(healAmount))
                        {
                            context.Sync.OffHeal_Client();
                        }
                    }
                    else
                        currentHealTime += Time.deltaTime;
                }
            }
        }

        #region 안씀

        public void KeyPress()
        {
        }

        public void KeyExit()
        {
        }
        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        #endregion


    }
}
