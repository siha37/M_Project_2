using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using MyFolder._1._Scripts._0._Object._2._Projectile;
using MyFolder._1._Scripts._1._UI._0._GameStage._0._Agent;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._0._Object._0._Agent
{
    public class AgentNetworkSync : NetworkBehaviour
    {

        #region Fields & Properties
        // 공통 상태 동기화
        protected readonly SyncVar<float> syncCurrentHp = new SyncVar<float>();
        protected readonly SyncVar<bool> syncIsDead = new SyncVar<bool>();
        protected readonly SyncVar<bool> syncIsCanSee = new SyncVar<bool>();
        protected readonly SyncVar<float> syncBulletCurrentCount = new SyncVar<float>();
        protected readonly SyncVar<bool> syncIsReloading = new SyncVar<bool>();
        protected readonly SyncVar<float> syncLookAngle = new SyncVar<float>();
    
        [Header("Interpolation Settings")]
        [SerializeField] protected float rotationLerpSpeed = 20f; // 회전 보간 속도
    
        // 보간용 타겟 값들
        protected float targetLookAngle;
        protected float currentLookAngle;
        protected bool shouldInterpolateRotation = false;

        protected bool OnReady = false;

        [SerializeField] protected Animator animator;
    
        // 공통 컴포넌트 참조
        protected AgentStatus AgentStatus;
        protected AgentUI agentUI;
        #endregion

        #region Events & Delegates
        // 공통 이벤트
        public delegate void OnAgentDamagedHandler(float damage, Vector2 hitDirection, NetworkConnection attacker);
        public delegate void OnAgentDeathHandler(NetworkConnection killer);
    
        #endregion
    
        #region Unity Lifecycle
        public override void OnStartServer()
        {
            InitializeComponents();
            InitializeSyncVars();
        }
    
        public override void OnStartClient()
        {
            InitializeComponents();
            RegisterSyncVarCallbacks();
            OnReady = true;
        }
    
        public override void OnStopClient()
        {
            // ✅ SyncVar 콜백 해제 (게임 종료/재시작 시 정리)
            UnregisterSyncVarCallbacks();
            OnReady = false;
        }
        protected virtual void Update()
        {
            if (!IsOwner && shouldInterpolateRotation)
            {
                // 부드러운 각도 보간
                float angleDifference = Mathf.DeltaAngle(currentLookAngle, targetLookAngle);
            
                if (Mathf.Abs(angleDifference) > 0.5f) // 0.5도 이상 차이날 때만 보간
                {
                    currentLookAngle = Mathf.LerpAngle(currentLookAngle, targetLookAngle, 
                        Time.deltaTime * rotationLerpSpeed);
                
                    // 실제 회전 적용
                    ApplyLookRotation(currentLookAngle);
                }
                else
                {
                    // 거의 도달했으면 정확한 값으로 설정
                    currentLookAngle = targetLookAngle;
                    ApplyLookRotation(currentLookAngle);
                    shouldInterpolateRotation = false;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            // 데이터 새로고침 콜백 해제
            if (AgentStatus is PlayerStatus playerStatus)
            {
                playerStatus.OnDataRefreshed -= OnAgentDataRefreshed;
            }
            else if (AgentStatus is EnemyStatus enemyStatus)
            {
                enemyStatus.OnDataRefreshed -= OnAgentDataRefreshed;
            }
        }
        #endregion
    
        #region Initialization
        protected virtual void RegisterSyncVarCallbacks()
        {
            syncCurrentHp.OnChange += OnCurrentHpChanged;
            syncIsDead.OnChange += OnIsDeadChanged;
            syncBulletCurrentCount.OnChange += OnBulletCountChanged;
            syncIsReloading.OnChange += OnIsReloadingChanged;
            syncLookAngle.OnChange += OnLookAngleChanged;
        }
    
        protected virtual void UnregisterSyncVarCallbacks()
        {
            syncCurrentHp.OnChange -= OnCurrentHpChanged;
            syncIsDead.OnChange -= OnIsDeadChanged;
            syncBulletCurrentCount.OnChange -= OnBulletCountChanged;
            syncIsReloading.OnChange -= OnIsReloadingChanged;
            syncLookAngle.OnChange -= OnLookAngleChanged;
        }

        protected virtual void InitializeComponents()
        {
            AgentStatus = GetComponent<AgentStatus>();
            agentUI = GetComponent<AgentUI>();
            if (!AgentStatus)
            {
                LogManager.LogError(LogCategory.Player, $"{gameObject.name} AgentState 컴포넌트를 찾을 수 없습니다.", this);
                return;
            }
        
            if (!agentUI)
            {
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} AgentUI 컴포넌트를 찾을 수 없습니다.", this);
            }
            else
            {
                // 데이터 로딩 상태 확인 후 초기화
                if (IsAgentDataLoaded())
                {
                    agentUI.InitializeUI(AgentStatus.Data.hp, AgentStatus.Data.hp, (int)AgentStatus.GetShootingData.magazineCapacity, (int)AgentStatus.GetShootingData.magazineCapacity, IsOwner);
                }
                else
                {
                    // 기본값으로 임시 초기화
                    agentUI.InitializeUI(100f, 100f, 30, 30, IsOwner);
                    LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} 데이터 로딩 중 - 기본값으로 UI 임시 초기화", this);
                    
                    // 데이터 로딩 완료 시 재초기화 콜백 등록
                    RegisterDataRefreshCallback();
                }
            }
        }
        
        /// <summary>
        /// Agent 데이터가 로딩되었는지 확인
        /// </summary>
        protected virtual bool IsAgentDataLoaded()
        {
            return AgentStatus.Data is { IsData: true };
        }
        
        
        
        /// <summary>
        /// 데이터 로딩 완료 시 재초기화 콜백 등록
        /// </summary>
        protected virtual void RegisterDataRefreshCallback()
        {
            if (AgentStatus is PlayerStatus playerStatus)
            {
                playerStatus.OnDataRefreshed += OnAgentDataRefreshed;
            }
            else if (AgentStatus is EnemyStatus enemyStatus)
            {
                enemyStatus.OnDataRefreshed += OnAgentDataRefreshed;
            }
            else
            {
                // 기타 Status 타입은 주기적 체크
                StartCoroutine(CheckDataLoadingPeriodically());
            }
        }
        
        /// <summary>
        /// 데이터 로딩 완료 후 UI 재초기화
        /// </summary>
        protected virtual void OnAgentDataRefreshed()
        {
            if (agentUI && AgentStatus && IsAgentDataLoaded())
            {
                agentUI.InitializeUI(AgentStatus.Data.hp, AgentStatus.Data.hp, 
                                   (int)AgentStatus.GetShootingData.magazineCapacity, 
                                   (int)AgentStatus.GetShootingData.magazineCapacity, IsOwner);
                
                // SyncVars도 재초기화 (서버에서만)
                if (IsServerInitialized)
                {
                    syncCurrentHp.Value = AgentStatus.Data.hp;
                    syncBulletCurrentCount.Value = AgentStatus.GetShootingData.magazineCapacity;
                }
                
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 데이터 로딩 완료 - UI 및 SyncVar 재초기화", this);
            }
        }
        
        /// <summary>
        /// 주기적으로 데이터 로딩 상태 체크 (Enemy용)
        /// </summary>
        private IEnumerator CheckDataLoadingPeriodically()
        {
            int maxChecks = 20; // 최대 10초 체크 (0.5초 * 20)
            int checkCount = 0;
            
            while (checkCount < maxChecks && !IsAgentDataLoaded())
            {
                yield return WaitForSecondsCache.Get(0.1f);
                checkCount++;
            }
            
            OnAgentDataRefreshed();
        }
    
        protected virtual void InitializeSyncVars()
        {
            if (AgentStatus)
            {
                // 데이터 로딩 상태 확인 후 초기화
                if (IsAgentDataLoaded())
                {
                    syncCurrentHp.Value = AgentStatus.Data.hp;
                    syncBulletCurrentCount.Value = AgentStatus.GetShootingData.magazineCapacity;
                }
                else
                {
                    // 데이터가 아직 로딩되지 않은 경우 기본값 사용
                    syncCurrentHp.Value = 100f;
                    syncBulletCurrentCount.Value = 30f;
                    LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} SyncVar 초기화 시 데이터 로딩 중 - 기본값 사용", this);
                }
                
                syncIsDead.Value = false;
                syncIsCanSee.Value = true;
                syncIsReloading.Value = false;
                syncLookAngle.Value = 0f;
            }
        }
        #endregion

        #region Network Synchronization
        // ✅ NetworkConnection 정보를 포함한 데미지 처리
        public virtual bool RequestTakeDamage(float damage, Vector2 hitDirection, NetworkConnection attacker = null)
        {
            bool criticalHit = false;
            if (!IsServerInitialized || !OnReady || syncIsDead.Value)
            {
                return false;
            }
            
            if (AgentStatus)
            {
                Log($"{gameObject.name} 서버에서 데미지 처리: {damage} (공격자: {attacker?.ClientId})", this);

                if (AgentStatus.TakeDamage(damage, hitDirection))
                    criticalHit = true;
                UpdateDamageSyncVars();
                OnDamagedEffect(damage, hitDirection);
                
                // 사망 시 이벤트 호출
                if (AgentStatus.IsDead)
                {
                    HandleAgentDeath(attacker);
                    OnDeathEffect();
                }
            }

            return criticalHit;
        }
        
        public void UpdateDamageSyncVars()
        {
            if (AgentStatus)
            {
                syncCurrentHp.Value = AgentStatus.currentHp;
                syncIsDead.Value = AgentStatus.IsDead;
            }
        }

        public void RequestUpdateBulletCount(float count)
        {
            if (AgentStatus)
            {
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 탄약 업데이트: {count}", this);
            
                AgentStatus.UpdateBulletCount(count);
                syncBulletCurrentCount.Value = AgentStatus.bulletCurrentCount;
            }
        }
        
        /// <summary>
        /// HP 업데이트 (위장 시스템 등에서 사용)
        /// </summary>
        public void RequestUpdateHP(float newHp)
        {
            if (AgentStatus)
            {
                LogManager.Log(LogCategory.Player, $"{gameObject.name} HP 업데이트: {AgentStatus.currentHp} -> {newHp}", this);
                
                AgentStatus.currentHp = newHp;
                syncCurrentHp.Value = newHp;
            }
        }
    
        // 공통 조준 방향 처리
        [ServerRpc]
        public void RequestUpdateLookAngle(float angle)
        {
            syncLookAngle.Value = angle;
        }
    
        // 공통 재장전 상태 처리
        [ServerRpc(RequireOwnership = false)]
        public virtual void RequestSetReloadingState(bool isReloading)
        {
            syncIsReloading.Value = isReloading;
        }
    
        // ✅ FishNet 공식 권장: Pool 시스템 + NetworkConnection 결합
        [ServerRpc]
        public virtual void RequestShoot(float angle, Vector3 shotPosition)
        {
            // ✅ BulletManager 초기화 확인 및 대기
            if (!BulletManager.Instance)
            {
                StartCoroutine(WaitForBulletManagerAndShoot(angle, shotPosition));
                return;
            }
        
            // ✅ BulletManager Pool 시스템 활용 (성능 최적화)
            BulletManager.Instance.FireBulletWithConnection(
                shotPosition,
                angle, 
                AgentStatus.GetShootingData.bulletSpeed,
                AgentStatus.GetShootingData.bulletDamage,
                AgentStatus.GetShootingData.lifeCycle,
                AgentStatus.GetShootingData.bulletSize,
                AgentStatus.GetShootingData.piercingCount,
                base.Owner  // ✅ NetworkConnection 전달
            );
        
            // 탄약 감소
            RequestUpdateBulletCount(-1);
        
            // 발사 효과
            OnShootEffect(angle, shotPosition);
        }
        // ✅ BulletManager 초기화 대기 코루틴
        private IEnumerator WaitForBulletManagerAndShoot(float angle, Vector3 shotPosition)
        {
            float waitTime = 0f;
            const float maxWaitTime = 5f; // 최대 5초 대기
        
            while (!BulletManager.Instance && waitTime < maxWaitTime)
            {
                yield return WaitForSecondsCache.Get(0.1f);
                waitTime += 0.1f;
            }
        
            if (BulletManager.Instance)
            {
                // ✅ 초기화 완료 후 정상 발사 처리
                LogManager.Log(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 완료 - 발사 재시도", this);
                RequestShoot(angle, shotPosition);
            }
            else
            {
                LogManager.LogError(LogCategory.Projectile, $"{gameObject.name} BulletManager 초기화 타임아웃! 발사 취소", this);
            }
        }
        #endregion

        #region SyncVar Callbacks
    
        protected void OnCurrentHpChanged(float oldValue, float newValue, bool asServer)
        {
            if (AgentStatus)
            {
                AgentStatus.currentHp = newValue;
            
                // ✅ AgentUI로 직접 업데이트
                if (agentUI)
                {
                    agentUI.UpdateHealthUI(newValue, AgentStatus.Data?.hp ?? 100);
                }  
            }
        }
    
        protected void OnBulletCountChanged(float oldValue, float newValue, bool asServer)
        {
            if (AgentStatus)
            {
                AgentStatus.bulletCurrentCount = newValue;
            
                // ✅ AgentUI로 직접 업데이트
                if (agentUI)
                {
                    agentUI.UpdateAmmoUI((int)AgentStatus.bulletCurrentCount,
                        AgentStatus.GetShootingData.magazineCapacity);
                }
            }
        }
    
        protected virtual void OnIsDeadChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (AgentStatus)
            {
                if(!IsServerInitialized)
                    AgentStatus.isDead = newValue;
                
                // ✅ 사망 상태 UI 업데이트 (필요시)
                if (agentUI && newValue)
                {
                    // TODO: 사망 시 UI 변경 로직 (체력바 숨김, 사망 표시 등)
                }
            }
        }
    
        protected virtual void OnIsReloadingChanged(bool oldValue, bool newValue, bool asServer)
        {
            // ✅ 재장전 UI 업데이트
            if (agentUI)
            {
                if (newValue)
                {
                    agentUI.StartReloadUI();
                }
                else
                {
                    agentUI.EndReloadUI();
                }
            }
        }
    
        protected virtual void OnLookAngleChanged(float oldValue, float newValue, bool asServer)
        {
            if (!IsOwner)
            {
                targetLookAngle = newValue;
            
                // 첫 번째 값이면 즉시 설정
                if (Mathf.Abs(oldValue) < 0.01f)
                {
                    currentLookAngle = newValue;
                    ApplyLookRotation(currentLookAngle);
                    shouldInterpolateRotation = false;
                }
                else
                {
                    // 일반적인 경우: 보간 처리 시작
                    shouldInterpolateRotation = true;
                }
                
            }
        }
        #endregion

        #region Effects & Visuals
        
        [ObserversRpc]
        protected virtual void OnShootEffect(float angle, Vector3 position)
        {
        }

        protected virtual void OnDamagedEffect(float damage, Vector2 hitDirection)
        {
        }
    
        [ObserversRpc]
        protected virtual void OnDeathEffect()
        {
            if(animator)
            {
                animator.SetTrigger("DIE");
            }
        }
        #endregion

        #region Utility Methods
        public float GetCurrentHp()
        {
            return syncCurrentHp.Value;
        }
    
        public bool IsDead()
        {
            return syncIsDead.Value;
        }

        public bool IsCanSee()
        {
            return syncIsCanSee.Value;
        }

        [ServerRpc]
        public void SetCanSee(bool canSee)
        {
            syncIsCanSee.Value = canSee;
        }
        public float GetBulletCount()
        {
            return syncBulletCurrentCount.Value;
        }
    
        public bool IsReloading()
        {
            return syncIsReloading.Value;
        }
    
        public float GetLookAngle()
        {
            return syncLookAngle.Value;
        }
        #endregion

        #region Virtual Methods
        protected virtual void ApplyLookRotation(float angle)
        {
            // 자식 클래스에서 구현
        }

        // ✅ 공격자 정보를 포함한 사망 처리 (상속 클래스에서 오버라이드 가능)
        protected virtual void HandleAgentDeath(NetworkConnection killer)
        {
            // 기본 사망 처리 - 상속 클래스에서 필요에 따라 오버라이드
            Log($"{gameObject.name} 사망 처리 (킬러: {killer?.ClientId})", this);
        
            // TODO: 킬/데스 통계, 리스폰 로직 등 구현
        }

        protected virtual void Log(string message,Object obj)
        {
            LogManager.Log(LogCategory.Player,message,obj);
        }
        protected virtual void LogError(string message,Object obj)
        {
            LogManager.LogError(LogCategory.Player,message,obj);
        }
        #endregion
    }
} 