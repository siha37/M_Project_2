using System.Collections;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public sealed class PlayerNetworkSync : AgentNetworkSync
    {
        #region Fields & Properties
        // 플레이어 전용 동기화
        private readonly SyncVar<int> syncReviveCurrentCount = new SyncVar<int>();
        private readonly SyncVar<bool> syncIsReviving = new SyncVar<bool>();
        private readonly SyncVar<Vector2> syncMoveDirection = new SyncVar<Vector2>();
        private readonly SyncVar<bool> syncIsMoving = new SyncVar<bool>();
        private readonly SyncVar<bool> syncIsAttacking = new SyncVar<bool>();
        private readonly SyncVar<float> syncDefenceAngle = new SyncVar<float>();
        private readonly SyncVar<float> syncCurrentDefence = new SyncVar<float>();
        private readonly SyncVar<bool> syncIsCrackDefence = new SyncVar<bool>();
        private readonly SyncVar<bool> syncIsQuesting = new SyncVar<bool>();
        private readonly SyncVar<int> syncActiveQuestId = new SyncVar<int>(-1);
        
        
        private readonly SyncVar<int> syncOwnerID = new SyncVar<int>(-1);
        private readonly SyncVar<string> syncSkin = new SyncVar<string>("");
        
        
        
        // 플레이어 전용 컴포넌트
        [SerializeField] private PlayerContext context;
        PlayerSkeletonAnimationComponent skeletonAnimation;
        // 방어 보간용 타겟 값
        private float defencetLookAngle;
        private float currentDefenceLookAngle;
        private bool shouldInterpolateDefenceRotation = false;
        
        public bool OnClient = false;

        private ushort targetEnemyId;
        
        #endregion


        #region Events & Delegates
        // 플레이어 전용 이벤트
        public delegate void OnPlayerRevivedHandler();
        public event OnPlayerRevivedHandler OnPlayerRevived;
        
        public delegate void OnPlayerDefenceHandler();
        public event OnPlayerDefenceHandler OnPlayerDefence;
        #endregion
        
        private Coroutine currentReviveCoroutine;
    
        #region Initialization

        public override void OnStartClient()
        {
            base.OnStartClient();
            OnClient = true;
            
            // ✅ 동기화 대기 후 스킨 적용 (나중에 접속한 클라이언트용)
            StartCoroutine(WaitForSyncAndApplySkin());
        }
        public override void OnStopClient()
        {
            OnClient = false;
            base.OnStopClient();
        }

        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            
            Owner.SetFirstObject(NetworkObject);
            // 데이터 로딩 상태 확인 후 초기화
            if (IsAgentDataLoaded())
            {
                context.AgentUI.InitializePlayerUI(
                    AgentStatus.Data.hp,
                    AgentStatus.Data.hp,
                    AgentStatus.GetShootingData.magazineCapacity,
                    AgentStatus.GetShootingData.magazineCapacity,
                    context.Status.PlayerData.defence,
                    context.Status.PlayerData.defence,
                    context.Status.PlayerData.revival,
                    IsOwner);
            }
            else
            {
                // 기본값으로 임시 초기화
                context.AgentUI.InitializePlayerUI(
                    100f,
                    100f,
                    30,
                    30,
                    100,
                    100,
                    3,
                    IsOwner);
                LogManager.LogWarning(LogCategory.Player, $"{gameObject.name} 데이터 로딩 중 - 기본값으로 UI 임시 초기화", this);
                // 데이터 로딩 완료 시 재초기화 콜백 등록
                RegisterDataRefreshCallback();
            }
        }
    
        protected override void RegisterSyncVarCallbacks()
        {
            base.RegisterSyncVarCallbacks();
            
            
            syncSkin.OnChange += OnOwnerIDChanged;
            syncReviveCurrentCount.OnChange += OnReviveCountChanged;
            syncIsReviving.OnChange += OnIsRevivingChanged;
            syncMoveDirection.OnChange += OnMoveDirectionChanged;
            syncIsMoving.OnChange += OnIsMovingChanged;
            syncIsAttacking.OnChange += OnIsAttackingChanged;
            syncDefenceAngle.OnChange += OnDefenceAngleChanged;
            syncCurrentDefence.OnChange += OnDefenceChanged;
            syncIsCrackDefence.OnChange += OnIsCrackDefenceChanged;
        }
        protected override void UnregisterSyncVarCallbacks()
        {
            syncSkin.OnChange -= OnOwnerIDChanged;
            syncReviveCurrentCount.OnChange -= OnReviveCountChanged;
            syncIsReviving.OnChange -= OnIsRevivingChanged;
            syncMoveDirection.OnChange -= OnMoveDirectionChanged;
            syncIsMoving.OnChange -= OnIsMovingChanged;
            syncIsAttacking.OnChange -= OnIsAttackingChanged;
            syncDefenceAngle.OnChange -= OnDefenceAngleChanged;
            syncCurrentDefence.OnChange -= OnDefenceChanged;
            syncIsCrackDefence.OnChange -= OnIsCrackDefenceChanged;
        }
        
        /// <summary>
        /// SyncVar 동기화를 기다린 후 스킨 적용 (나중에 접속한 클라이언트용)
        /// </summary>
        private IEnumerator WaitForSyncAndApplySkin()
        {
            float timeout = 5f;
            float elapsed = 0f;
            
            // ✅ syncSkin.Value가 동기화될 때까지 대기
            while (string.IsNullOrEmpty(syncSkin.Value) && elapsed < timeout)
            {
                yield return WaitForSecondsCache.Get(0.1f);
                elapsed += 0.1f;
            }
            
            // ✅ 동기화 완료 후 스킨 적용
            if (!string.IsNullOrEmpty(syncSkin.Value))
            {
                LogManager.Log(LogCategory.Player, 
                    $"{gameObject.name} 초기 스킨 동기화 완료: {syncSkin.Value} (소요시간: {elapsed:F1}초)", this);
                ApplySkinInternal(syncSkin.Value);
            }
            else
            {
                LogManager.LogWarning(LogCategory.Player, 
                    $"{gameObject.name} syncSkin 동기화 타임아웃 ({timeout}초)", this);
            }
        }
        
        /// <summary>
        /// 실제 스킨 적용 로직 (중복 제거)
        /// </summary>
        private void ApplySkinInternal(string skinName)
        {
            // ✅ Context 및 Component null 체크
            if (!context?.Component)
            {
                LogManager.LogWarning(LogCategory.Player, 
                    $"{gameObject.name} ApplySkinInternal: Component not ready yet, will retry", this);
                StartCoroutine(ApplySkinWhenReady(skinName));
                return;
            }

            PlayerSkeletonAnimationComponent skeleton = 
                context.Component.GetPComponent<PlayerSkeletonAnimationComponent>() as PlayerSkeletonAnimationComponent;
            
            if(skeleton != null)
            {
                skeleton.SkinChange(skinName);
                LogManager.Log(LogCategory.Player, $"{gameObject.name} Skin applied: {skinName}", this);
            }
            else
            {
                // ✅ Skeleton component가 null이면 재시도
                LogManager.LogWarning(LogCategory.Player, 
                    $"{gameObject.name} Skeleton component not ready yet, will retry", this);
                StartCoroutine(ApplySkinWhenReady(skinName));
            }
        }
        
        /// <summary>
        /// Component가 준비될 때까지 기다렸다가 스킨 적용 (Fallback용)
        /// </summary>
        private IEnumerator ApplySkinWhenReady(string skinName)
        {
            float timeout = 5f;
            float elapsed = 0f;
    
            while (elapsed < timeout)
            {
                if (context?.Component)
                {
                    PlayerSkeletonAnimationComponent skeleton = 
                        context.Component.GetPComponent<PlayerSkeletonAnimationComponent>() as PlayerSkeletonAnimationComponent;
                
                    if (skeleton != null)
                    {
                        skeleton.SkinChange(skinName);
                        LogManager.Log(LogCategory.Player, 
                            $"{gameObject.name} Skin applied (delayed): {skinName} (소요시간: {elapsed:F1}초)", this);
                        yield break;
                    }
                }
        
                yield return WaitForSecondsCache.Get(0.1f);
                elapsed += 0.1f;
            }
            
            LogManager.LogWarning(LogCategory.Player, 
                $"{gameObject.name} ApplySkinWhenReady 타임아웃 - Skeleton component를 찾을 수 없음", this);
        }
        /// <summary>
        /// 스킨 변경 감지 (기존 클라이언트가 값 변경 감지용)
        /// </summary>
        private void OnOwnerIDChanged(string prev, string next, bool asserver)
        {
            // ✅ 서버는 자기 변경사항 재처리 안함
            if (IsServerInitialized)
            {
                return;
            }
            
            // ✅ 빈 문자열 체크
            if (string.IsNullOrEmpty(next))
            {
                LogManager.LogWarning(LogCategory.Player, 
                    $"{gameObject.name} OnOwnerIDChanged: Skin name is empty", this);
                return;
            }
            
            LogManager.Log(LogCategory.Player, 
                $"{gameObject.name} OnOwnerIDChanged: {prev} → {next}", this);
            
            // ✅ 공통 적용 로직 호출
            ApplySkinInternal(next);
        }

        protected override void OnAgentDataRefreshed()
        {
            // 부모 클래스의 재초기화 먼저 실행
            base.OnAgentDataRefreshed();
            
            // 플레이어 전용 SyncVar 재초기화 (서버에서만)
            if (IsServerInitialized && context.Status  && IsAgentDataLoaded())
            {
                syncReviveCurrentCount.Value = context.Status .PlayerData.revival;
                
                context.AgentUI.InitializePlayerUI(
                    AgentStatus.Data.hp,
                    AgentStatus.Data.hp,
                    AgentStatus.GetShootingData.magazineCapacity,
                    AgentStatus.GetShootingData.magazineCapacity,
                    context.Status.PlayerData.defence,
                    context.Status.PlayerData.defence,
                    context.Status.PlayerData.revival,
                    IsOwner);
            }
        }
        protected override void InitializeSyncVars()
        {
            base.InitializeSyncVars();
            if (context.Status )
            {
                // ✅ 데이터 로딩 상태 확인 후 초기화 - 더 안전한 방식
                if (IsAgentDataLoaded() && context.Status .PlayerData != null)
                {
                    syncReviveCurrentCount.Value = context.Status .PlayerData.revival;
                }
                else
                {
                    // ✅ 데이터가 아직 로딩되지 않은 경우 - 나중에 재초기화 예정
                    syncReviveCurrentCount.Value = 3; // 기본 부활 횟수
                    
                    // ✅ 데이터 로딩 완료를 기다리는 코루틴 시작
                    if (IsServerInitialized)
                    {
                        StartCoroutine(WaitForDataAndReinitialize());
                    }
                }

                syncOwnerID.Value = Owner.ClientId;
                int skinIndex = (Owner.ClientId-1) % PlayerSettingManager.Instance.SkinName.Count;
                syncSkin.Value = PlayerSettingManager.Instance.SkinName[skinIndex];
                syncIsReviving.Value = false;
                syncMoveDirection.Value = Vector2.zero;
                syncIsMoving.Value = false;
                syncIsAttacking.Value = false;
                syncDefenceAngle.Value = 0f;
            }
        }
        #endregion

        #region Unity Lifecycle
        protected override void Update()
        {
            base.Update();
            if (!IsOwner && shouldInterpolateDefenceRotation)
            {
                // 부드러운 각도 보간
                float angleDifference = Mathf.DeltaAngle(currentDefenceLookAngle, defencetLookAngle);
            
                if (Mathf.Abs(angleDifference) > 0.5f) // 0.5도 이상 차이날 때만 보간
                {
                    currentDefenceLookAngle = Mathf.LerpAngle(currentDefenceLookAngle, defencetLookAngle, 
                        Time.deltaTime * rotationLerpSpeed);
                
                    // 실제 회전 적용
                    ApplyDefenceLookRotation(currentDefenceLookAngle);
                }
                else
                {
                    // 거의 도달했으면 정확한 값으로 설정
                    currentDefenceLookAngle = defencetLookAngle;
                    ApplyDefenceLookRotation(currentDefenceLookAngle);
                    shouldInterpolateDefenceRotation = false;
                }
            }
        }


        #endregion
        
        #region Network Synchronization
        /// <summary>
        /// ✅ 데이터 로딩 완료를 기다리고 SyncVar 재초기화
        /// </summary>
        private IEnumerator WaitForDataAndReinitialize()
        {
            float timeout = 30f; // 30초 타임아웃
            float elapsed = 0f;
            
            LogManager.Log(LogCategory.Player, $"{gameObject.name} PlayerNetworkSync 데이터 로딩 대기 시작", this);
            
            while (elapsed < timeout)
            {
                // 데이터가 로딩되었는지 확인
                if (IsAgentDataLoaded() && context.Status  && context.Status.PlayerData != null)
                {
                    // SyncVar 재초기화
                    syncReviveCurrentCount.Value = context.Status.PlayerData.revival;
                    
                    yield break;
                }
                
                // 주기적으로 상태 로그 (10초마다)
                if (elapsed % 10f < 0.1f)
                {
                    LogManager.LogWarning(LogCategory.Player, 
                        $"{gameObject.name} PlayerNetworkSync 데이터 로딩 대기 중... (경과: {elapsed:F1}초)", this);
                }
                
                yield return WaitForSecondsCache.Get(0.1f);
                elapsed += 0.1f;
            }
            
            // 타임아웃 경고
            LogManager.LogError(LogCategory.Player, 
                $"{gameObject.name} PlayerNetworkSync 데이터 로딩 타임아웃 ({timeout}초) - 기본값 유지", this);
        }
        
        protected override void OnIsDeadChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (context.Status)
            {
                context.Status.isDead = newValue;
            
                // ✅ 사망 상태 UI 업데이트 (필요시)
                if (newValue)
                {
                    if (agentUI)
                    {
                        context.Controller.MoveStop();
                        if (syncReviveCurrentCount.Value != 0)
                            context.Status.OnClientDeathSequence();
                        else
                            context.Status.OnClientDeath();
                        // TODO: 사망 시 UI 변경 로직 (체력바 숨김, 사망 표시 등)   
                    }
                }         
                else if(oldValue) // 부활 시 ✅
                {
                    // Owner만 컴포넌트 활성화
                    if (IsOwner)
                    {
                        context.Status.ComponentRevive();
                    }
                }
#if UNITY_EDITOR
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 사망 상태 동기화: {oldValue} -> {newValue}", this);
#endif
            }
        }
        // 플레이어 전용 부활 처리
        [ServerRpc(RequireOwnership = false)]
        public void RequestRevive()
        {
            if (context.Status  && context.Status.IsDead)
            {
                LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 부활 처리", this);
            
                context.Status.Revive();
                context.Sfx.Play(PlayerSfxType.PlayerAlive);
                
                //초기화 값 동기화
                syncCurrentHp.Value = context.Status.currentHp;
                syncCurrentDefence.Value = context.Status.currentDefence;
                syncReviveCurrentCount.Value = context.Status.reviveCurrentCount;
                syncIsDead.Value = context.Status.IsDead;
                syncIsCrackDefence.Value = context.Status .IsCrackDefence;
            
                // 모든 클라이언트에 부활 효과 전송
                OnRevivedEffect();
                OnPlayerRevived?.Invoke();
            }
        }
    
    
        // 플레이어 전용 이동 동기화
        [ServerRpc]
        public void RequestUpdateMovement(Vector2 direction, bool isMoving)
        {
            syncMoveDirection.Value = direction;
            syncIsMoving.Value = isMoving;
        }
    
        // 플레이어 전용 공격 상태 동기화
        [ServerRpc]
        public void RequestUpdateAttackState(bool isAttacking)
        {
            syncIsAttacking.Value = isAttacking;
        }
        
        // 플레이어 전용 재장전 처리
        [ServerRpc]
        public void RequestReload()
        {
            if (syncIsReloading.Value) return;
        
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 서버에서 재장전 시작", this);
            RequestSetReloadingState(true);
            StartCoroutine(ServerReloadProcess());
        }
    
        
        // 플레이어 전용 방향 처리
        [ServerRpc]
        public void RequestDefenceLookAngle(float angle)
        {
            syncDefenceAngle.Value = angle;
        }
        #endregion

        
        private IEnumerator ServerReloadProcess()
        {
            float reloadTimer = 0f;
        
            while (reloadTimer < AgentStatus.GetShootingData.reloadTime)
            {
                reloadTimer += Time.deltaTime;
                float progress = reloadTimer / AgentStatus.GetShootingData.reloadTime;
                OnReloadProgress_Local(progress);
                OnReloadProgress_Observer(progress);
                yield return null;
            }
        
            // 재장전 완료
            RequestUpdateBulletCount(AgentStatus.GetShootingData.magazineCapacity);
            RequestSetReloadingState(false);
            OnReloadComplete();
        }
        
        
        [ServerRpc(RequireOwnership = false)]
        public void OnRevivedStart()
        {
            OnPlayerRevived_Observer();
        }

        [ObserversRpc]
        private void OnPlayerRevived_Observer()
        {
            if (context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() is PlayerSkeletonAnimationComponent animComponent)
            {
                animComponent.RevivalAttemptAnimationShot();
            }
            
            if(IsOwner)
                return;
            
            // 이전 코루틴 정리
            if (currentReviveCoroutine != null)
            {
                StopCoroutine(currentReviveCoroutine);
            }
    
            currentReviveCoroutine = StartCoroutine(LocalReviveProgressAnimation());
        }
        private IEnumerator LocalReviveProgressAnimation()
        {
            float elapsedTime = 0f;
    
            while (elapsedTime < PlayerStatus.reviveDelay)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / PlayerStatus.reviveDelay;
                context.AgentUI.UpdateReviveProgress(progress);
                yield return null;
            }
    
            currentReviveCoroutine = null;
        }

        [ServerRpc(RequireOwnership = false)]
        public void OnRevivedEnd()
        {
            OnRevivedEnd_Observer();
        }

        [ObserversRpc]
        private void OnRevivedEnd_Observer()
        {
            if(IsOwner)
                return;
            // ✅ 타이머 강제 종료
            if (currentReviveCoroutine != null)
            {
                StopCoroutine(currentReviveCoroutine);
                currentReviveCoroutine = null;
            }
    
            // ✅ UI 즉시 완료 처리
            context.AgentUI.UpdateReviveProgress(0f);
        }

        public void OnQuestingStarted(int questId)
        {
            syncIsQuesting.Value = true;
            syncActiveQuestId.Value = questId;
        }

        public void OnQuestingFinished()
        {
            syncIsQuesting.Value = false;
            syncActiveQuestId.Value = -1;
        }

        public bool IsQuesting()
        {
            return syncIsQuesting.Value;
        }

        public int GetActiveQuestId()
        {
            return syncActiveQuestId.Value;
        }
    
        #region Effects & Visuals
        [ObserversRpc]
        private void OnRevivedEffect()
        {
            context.Status.ClientReviveEffect();
            Log($"{gameObject.name} 부활 효과 재생", this);
        }
    
        // 플레이어 전용 발사 효과
        
        [ObserversRpc]
        protected override void OnShootEffect(float angle, Vector3 position)
        {
            context.Sfx.Play(PlayerSfxType.PlayerShooting);
            if (skeletonAnimation == null)
                skeletonAnimation = context.Component.GetPComponent<PlayerSkeletonAnimationComponent>() as PlayerSkeletonAnimationComponent;
            if(skeletonAnimation != null)
                skeletonAnimation.BulletShot();
        }
        
        // 플레이어 전용 사망 효과
        protected override void OnDeathEffect()
        {
            context.Sfx.Play(PlayerSfxType.PlayerDie);
        }
        #endregion

        #region SyncVar Callbacks
        private void OnReloadProgress_Local(float progress)
        {
            if(agentUI)
                agentUI.UpdateReloadProgress(progress);   
        }
        [ObserversRpc]
        private void OnReloadProgress_Observer(float progress)
        {
            if(agentUI)
                agentUI.UpdateReloadProgress(progress);
        }
    
        [ObserversRpc]
        private void OnReloadComplete()
        {
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 재장전 완료", this);
        }
    
        private void OnReviveCountChanged(int oldValue, int newValue, bool asServer)
        {
            if (context)
            {
                context.Status.reviveCurrentCount = newValue;
                context.AgentUI.UpdateReviveAmount(newValue);
            }
        }
    
        private void OnIsRevivingChanged(bool oldValue, bool newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 부활 진행 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        private void OnReviveProgressChanged(float oldValue, float newValue, bool asServer)
        {
            context.AgentUI.UpdateReviveProgress(newValue);
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 부활 진행률 동기화: {oldValue * 100:F1}% -> {newValue * 100:F1}%", this);
#endif
        }
    
        private void OnMoveDirectionChanged(Vector2 oldValue, Vector2 newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 이동 방향 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        private void OnIsMovingChanged(bool oldValue, bool newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 이동 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        private void OnIsAttackingChanged(bool oldValue, bool newValue, bool asServer)
        {
#if UNITY_EDITOR
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 공격 상태 동기화: {oldValue} -> {newValue}", this);
#endif
        }
    
        protected override void ApplyLookRotation(float angle)
        {
            context.Shooter.ClientUpdateLookAngle(angle);
        }
    
        protected override void OnLookAngleChanged(float oldValue, float newValue, bool asServer)
        {
            base.OnLookAngleChanged(oldValue, newValue, asServer);
        
        }


        #region Defence

        private void OnDefenceAngleChanged(float oldValue, float newValue, bool asServer)
        {
            if (!IsOwner)
            {
                defencetLookAngle = newValue;
            
                // 첫 번째 값이면 즉시 설정
                if (Mathf.Abs(oldValue) < 0.01f)
                {
                    currentDefenceLookAngle = newValue;
                    ApplyDefenceLookRotation(currentDefenceLookAngle);
                    shouldInterpolateDefenceRotation = false;
                }
                else
                {
                    // 일반적인 경우: 보간 처리 시작
                    shouldInterpolateDefenceRotation = true;
                }
            }
        }

        private void ApplyDefenceLookRotation(float angle)
        {
            if (context.Input && context.DefencePivot)
            {
                context.DefencePivot.rotation = Quaternion.Euler(0, 0, angle);
            }
        }
        
        public void RequestTakeDefence(float damage, Vector2 hitDirection, NetworkConnection attacker = null)
        {
            if (context.Component)
            {
                ((PlayerDefenceComponent)context.Component.GetPComponent<PlayerDefenceComponent>()).TakeDefence(damage, hitDirection);
                
                //동기화 및 UI 업데이트
                UpdateDefenceSyncVars();
                DefenceEffectClient();
            }
        }
        
        /// <summary>
        ///방어 스탯 동기화 진행
        /// </summary>
        public void UpdateDefenceSyncVars()
        {
            syncCurrentDefence.Value = context.Status.currentDefence;
            syncIsCrackDefence.Value = context.Status.IsCrackDefence;
        }

        /// <summary>
        /// 방어 이펙트 동기화
        /// </summary>
        [ObserversRpc]
        private void DefenceEffectClient()
        {
            OnPlayerDefence?.Invoke();
            context.Sfx.Play(PlayerSfxType.PlayerDefence);
        }

        private void OnDefenceChanged(float oldValue, float newValue, bool asServer)
        {
            if (context.Status)
            {
                context.Status.currentDefence = newValue;

                if (context.AgentUI)
                {
                    LogManager.Log(LogCategory.Player,$"now : {newValue} - max : {context.Status?.PlayerData?.defence ?? 100}");
                    context.AgentUI.UpdateShieldUI(newValue, context.Status?.PlayerData?.defence ?? 100);
                }
            }
        }

        private void OnIsCrackDefenceChanged(bool oldValue, bool newValue, bool asServer)
        {
            if (context.Status)
            {
                context.Status.IsCrackDefence = newValue;
            }
        }
        
        #endregion
        
        protected override void OnIsReloadingChanged(bool oldValue, bool newValue, bool asServer)
        {
            base.OnIsReloadingChanged(oldValue, newValue, asServer);
            if (context.Input)
            {
                context.Shooter.SetReloadingstate(newValue);
            }
        }


        public void ServerRequestDisguiseActivation()
        {
            ApplyDisguiseDeactivationObservers();
        }
        
        #endregion

        /// <summary>
        /// 서버에서 후방(크리티컬) 판정 계산 후, 기본 데미지 처리 호출 + 전용 효과 브로드캐스트
        /// </summary>
        /// <param name="damage"></param>
        /// <param name="hitDirection"></param>
        /// <param name="attacker"></param>
        public override bool RequestTakeDamage(float damage, Vector2 hitDirection, NetworkConnection attacker = null)
        {
            if (!IsServerInitialized)
                return false;

            // 기본 처리(데미지 적용, SyncVar 갱신, 이벤트/기본 효과)는 부모에서 수행
            bool isCritical = base.RequestTakeDamage(damage, hitDirection, attacker);

            // 추가: 크리티컬 여부 포함 SFX/VFX 브로드캐스트
            OnDamagedEffect_Player(damage, hitDirection, isCritical);
            return isCritical;
        }
    
        public void UpdateHealSyncVars()
        {
            if(AgentStatus)
                syncCurrentHp.Value = AgentStatus.currentHp;
        }
        // 서버 계산된 크리티컬 여부 포함 데미지 효과 브로드캐스트
        private void OnDamagedEffect_Player(float damage, Vector2 hitDirection, bool isCritical)
        {
            context.Sfx.Play(isCritical ? PlayerSfxType.PlayerCriticalHit : PlayerSfxType.PlayerHit);
        }
        
        #region Disguise System
        /// <summary>
        /// 위장 활성화 요청 (클라이언트 → 서버)
        /// </summary>
        [ServerRpc]
        public void RequestDisguiseActivation(ushort enemyTypeId)
        {
            ApplyDisguiseActivationObservers(enemyTypeId);
        }

        /// <summary>
        /// 위장 활성화 적용 (서버 → 모든 클라이언트)
        /// </summary>
        [ObserversRpc]
        private void ApplyDisguiseActivationObservers(ushort enemyTypeId)
        {
            targetEnemyId = enemyTypeId;
            if (context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() is PlayerSkeletonAnimationComponent animComponent)
            {
                animComponent.CamouflageShot();
            }
        }

        /// <summary>
        /// 위장 애니메이션 종료 후 진행
        /// </summary>
        public void ApplyDisguiseActivation()
        {
            var camouflageComponent = context.Component?.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            camouflageComponent?.ApplyDisguise(targetEnemyId);
        }

        /// <summary>
        /// 위장 해제 요청 (클라이언트 → 서버)
        /// </summary>
        [ServerRpc]
        public void RequestDisguiseDeactivation()
        {
            ApplyDisguiseDeactivationObservers();
        }

        /// <summary>
        /// 위장 해제 적용 (서버 → 모든 클라이언트)
        /// </summary>
        [ObserversRpc]
        private void ApplyDisguiseDeactivationObservers()
        {
            var camouflageComponent = context.Component?.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            camouflageComponent?.ApplyDeactivateDisguise();
        }
        #endregion

        #region Virtual Methods
        protected override void HandleAgentDeath(NetworkConnection killer)
        {
            base.HandleAgentDeath(killer);
        
            // ✅ 플레이어 전용 사망 처리
            LogManager.Log(LogCategory.Player, $"{gameObject.name} 플레이어 사망 처리 (킬러: {killer?.ClientId})", this);
        
            // TODO: 플레이어 전용 사망 로직
            // - 리스폰 타이머 시작
            // - 사망 통계 업데이트
            // - 플레이어 UI 처리
        }
        #endregion

        [ServerRpc]
        public void OnHeal_Server()
        {
            OnHeal_Client();
        }

        [ServerRpc]
        public void OffHeal_Server()
        {
            OffHeal_Client();
        }

        [ObserversRpc]
        public void OffHeal_Client()
        {
            if (context.Component?.GetPComponent<PlayerHealComponent>() is PlayerHealComponent healcomponent)
            {
                healcomponent.StopHealing();
            }
        }
        

        [ObserversRpc]
        public void OnHeal_Client()
        {
            if (context.Component?.GetPComponent<PlayerHealComponent>() is PlayerHealComponent healcomponent)
            {
                healcomponent.StartHealing();
            }
        }
        
    }
} 