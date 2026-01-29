using System;
using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using UnityEngine;
using MyFolder._1._Scripts._0._Object._2._Projectile;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using Object = UnityEngine.Object;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy
{
    /// <summary>
    /// 적 네트워크 동기화 컴포넌트 (리팩토링 버전)
    /// 기존의 여러 개별 SyncVar을 하나의 EnemyStateData로 통합
    /// 새로운 컴포넌트 기반 구조와 연동
    /// </summary>
    public class EnemyNetworkSync : AgentNetworkSync
    {
        #region Fields & Properties
        public Action ReloadCompleteEvent;
        [SerializeField] private EnemyControll controll;
        [SerializeField] private EnemySfx sfx;
        [SerializeField] private EnemyStatus status;
        EnemySkeletonAnimation skeletonAnimation;
        
        protected readonly SyncVar<int> syncAnimationDirection = new SyncVar<int>();
        protected readonly SyncVar<bool> syncIsMoving = new SyncVar<bool>();
        protected readonly SyncVar<Vector2> syncMoveDirection = new SyncVar<Vector2>();
        
        #endregion

        #region Initialization

        
        protected override void RegisterSyncVarCallbacks()
        {
            base.RegisterSyncVarCallbacks();
            skeletonAnimation = controll.GetEnemyActiveComponent(typeof(EnemySkeletonAnimation)) as EnemySkeletonAnimation;
            syncAnimationDirection.OnChange += AnimationDirectionChanged;
            syncIsMoving.OnChange += isMovingChanged;
        }

        #endregion
        #region Unity Lifecycle

        public override void OnStartClient()
        {
            base.OnStartClient();
            if(IsServerInitialized)
                sfx.Play(EnemySfxType.EnemySpawned);
        }
        
        #endregion

        #region Network Synchronization

        public override bool RequestTakeDamage(float damage, Vector2 hitDirection, NetworkConnection attacker = null)
        {
            bool result = base.RequestTakeDamage(damage, hitDirection, attacker);
            if (status.isDead) status.OnDeathSequence();
            else status.AttakerTargeting(attacker?.FirstObject.gameObject);
            return result;
        }
        public void RequestUpdateLookAngleForEnemy(float angle)
        {
            syncLookAngle.Value = angle;
        }
        
        // AI 전용 재장전 처리
        public void RequestReload()
        {
            if (syncIsReloading.Value) return;
        
            Log($"{gameObject.name} 서버에서 재장전 시작", this);
            RequestSetReloadingState(true);
            StartCoroutine(ServerReloadProcess());
        }
    
        private IEnumerator ServerReloadProcess()
        {
            float reloadTimer = 0f;
        
            while (reloadTimer < AgentStatus.GetShootingData.reloadTime)
            {
                reloadTimer += Time.deltaTime;
                yield return null;
            }
        
            // 재장전 완료
            RequestUpdateBulletCount(AgentStatus.GetShootingData.magazineCapacity);
            RequestSetReloadingState(false);
            ReloadCompleteEvent?.Invoke();
        }
    

        // 적 전용 발사 처리
        [Server]
        public void ShootEnemyBullet(float angle, Vector3 shotPosition)
        {
            if (!BulletManager.Instance)
            {
                StartCoroutine(WaitForBulletManagerAndShootEnemy(angle, shotPosition));
                return;
            }

            BulletManager.Instance.FireBulletForEnemy(
                shotPosition,
                angle,
                AgentStatus.GetShootingData.bulletSpeed,
                AgentStatus.GetShootingData.bulletDamage,
                AgentStatus.GetShootingData.lifeCycle,
                AgentStatus.GetShootingData.bulletSize,
                AgentStatus.GetShootingData.piercingCount,
                gameObject
            );
            ShotAnimationStart_Client();

            RequestUpdateBulletCount(-1);
            OnShootEffect(angle, shotPosition);
        }

        private IEnumerator WaitForBulletManagerAndShootEnemy(float angle, Vector3 shotPosition)
        {
            float waitTime = 0f;
            const float maxWaitTime = 5f;

            while (!BulletManager.Instance && waitTime < maxWaitTime)
            {
                yield return WaitForSecondsCache.Get(0.1f);
                waitTime += 0.1f;
            }

            if (BulletManager.Instance)
            {
                ShootEnemyBullet(angle, shotPosition);
            }
            else
                LogManager.LogError(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 타임아웃! 발사 취소", this);
        }
        #endregion
        
        #region SyncVar Callbacks
        protected override void OnIsDeadChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (AgentStatus && asServer)
            {
                if(!IsServerInitialized)
                    AgentStatus.isDead = newValue;   
            }
        }
        #endregion

        #region Virtual Methods
        protected override void ApplyLookRotation(float angle)
        {
            if (!controll)
                TryGetComponent(out controll);
            EnemyCombat combat = (EnemyCombat)controll.GetEnemyAllComponent(typeof(EnemyCombat));
            if (combat != null)
                combat.ShotObjectAngleUpdate(angle);
        }
        
        [ObserversRpc]
        protected override void OnShootEffect(float angle, Vector3 position)
        {
            sfx.Play(EnemySfxType.EnemyShooting);
        }
        
        protected override void OnDamagedEffect(float damage, Vector2 hitDirection)
        {
        }
        #endregion

        #region Utility Methods
        protected override void Log(string message,Object obj)
        {
            LogManager.Log(LogCategory.Enemy,message,obj);
        }
        protected override void LogError(string message,Object obj)
        {
            LogManager.LogError(LogCategory.Enemy,message,obj);
        }
        #endregion

        #region Pooling Methods

        public void DieResetSync()
        {
            OnReady = false;
        }
        
        /// <summary>
        /// 풀링을 위한 네트워크 동기화 상태 리셋
        /// - 데이터 로드 완료 후 호출되므로 안전하게 초기화 가능
        /// - SyncVar 초기화 (서버만)
        /// - 보간 변수 초기화
        /// - 이벤트 정리
        /// - UI 초기화 (호스트/게스트 동기화)
        /// </summary>
        public void ResetSync()
        {
            Log($"{gameObject.name} NetworkSync 리셋 시작", this);

            // 1. SyncVar 초기화 (서버에서만, 데이터 로드 완료 보장됨)
            if (IsServerInitialized && AgentStatus)
            {
                if (AgentStatus.Data != null && AgentStatus.GetShootingData != null)
                {
                    // 데이터 기반 초기화
                    syncCurrentHp.Value = AgentStatus.Data.hp;
                    syncBulletCurrentCount.Value = AgentStatus.GetShootingData.magazineCapacity;
                    
                    Log($"{gameObject.name} SyncVar 초기화: HP={syncCurrentHp.Value}, Bullet={syncBulletCurrentCount.Value}", this);
                }
                else
                {
                    // 이 경우는 발생하지 않아야 함 (데이터 로드 완료 후 호출되므로)
                    LogError($"{gameObject.name} 데이터가 없음! 기본값 사용", this);
                    syncCurrentHp.Value = 100f;
                    syncBulletCurrentCount.Value = 30f;
                }

                syncIsDead.Value = false;
                syncIsCanSee.Value = true;
                syncIsReloading.Value = false;
                syncLookAngle.Value = 0f;
            }

            // 2. 보간 변수 초기화
            targetLookAngle = 0f;
            currentLookAngle = 0f;
            shouldInterpolateRotation = false;

            // 3. 이벤트 정리 (메모리 누수 방지)
            if (ReloadCompleteEvent != null)
            {
                foreach (var d in ReloadCompleteEvent.GetInvocationList())
                {
                    ReloadCompleteEvent -= (Action)d;
                }
            }

            // 4. 진행 중인 코루틴 정리 (재장전 등)
            StopAllCoroutines();

            // 5. UI 초기화 (호스트/게스트 동기화)
            if (agentUI && AgentStatus)
            {
                if (AgentStatus.Data != null && AgentStatus.GetShootingData != null)
                {
                    // 서버와 클라이언트 모두에서 UI 초기화
                    agentUI.InitializeUI(
                        AgentStatus.Data.hp,
                        AgentStatus.Data.hp,
                        (int)AgentStatus.GetShootingData.magazineCapacity,
                        (int)AgentStatus.GetShootingData.magazineCapacity,
                        IsOwner
                    );
                    
                    Log($"{gameObject.name} UI 초기화 완료 (HP={AgentStatus.Data.hp}, Ammo={AgentStatus.GetShootingData.magazineCapacity})", this);
                }
            }

            // 6. OnReady 플래그 설정 (네트워크 동기화 준비 완료)
            OnReady = true;

            Log($"{gameObject.name} NetworkSync 리셋 완료", this);
        }

        #endregion

        #region Animation Methods

        public void SetAnimationDirection(int value)
        {
            syncAnimationDirection.Value = value;
        }

        public int GetAnimationDirection()
        {
            return syncAnimationDirection.Value;
        }

        public void SetIsMoving(bool value)
        {
            syncIsMoving.Value = value;
        }

        public bool GetIsMoving()
        {
            return syncIsMoving.Value;
        }

        private void AnimationDirectionChanged(int oldValue, int newValue, bool asServer)
        {
            if (!IsServerInitialized)
            {
                if (oldValue != newValue)
                {
                    if(skeletonAnimation == null)
                        skeletonAnimation = controll.GetEnemyActiveComponent(typeof(EnemySkeletonAnimation)) as EnemySkeletonAnimation;
                    if (skeletonAnimation != null) skeletonAnimation.Direction = newValue;
                }
            }
        }

        private void isMovingChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (!IsServerInitialized)
            {
                if (oldValue != newValue)
                {
                    if(skeletonAnimation == null)
                        skeletonAnimation = controll.GetEnemyActiveComponent(typeof(EnemySkeletonAnimation)) as EnemySkeletonAnimation;
                    if (skeletonAnimation != null) skeletonAnimation.OnMove = newValue;
                }
            }
        }

        [ObserversRpc(ExcludeOwner = false)]
        public void ShotAnimationStart_Client()
        {
            if(skeletonAnimation == null)
                skeletonAnimation = controll.GetEnemyActiveComponent(typeof(EnemySkeletonAnimation)) as EnemySkeletonAnimation;
            skeletonAnimation?.ShotStart();
        }

        #endregion
    }
}