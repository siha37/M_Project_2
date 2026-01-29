using System;
using System.Collections;
using FishNet;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component;
using MyFolder._1._Scripts._0._Object._4._Shooting;
using MyFolder._1._Scripts._0._Object._5._ModifiableStat;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._2._View._1._ScreenMark;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player
{
    public class PlayerStatus : AgentStatus
    {
        private const float BACK_ATTACK_ANGLE_THRESHOLD = 135f;
        public const float reviveDelay = 4f;
        public const float reviveRange = 5f;
        public int reviveCurrentCount;
        public float currentDefence = 100;
        public bool IsCrackDefence = false;

        [SerializeField] private PlayerContext context;
        
        private Color originalColor;
        
        private PlayerHealComponent playerHeal;

        public Action OnReviveAbleDeathEvent;
        public Action OnRevive;
        private int MarkId;
        
        public event Action OnDataRefreshed;
        public event Action OnDataUpdate;

        // 타입 안전한 프로퍼티
        public PlayerData PlayerData => data as PlayerData;
        
        protected override void InitializeData()
        {
            StartCoroutine(nameof(InitializePlayerData));
        }

        IEnumerator InitializePlayerData()
        {

            while (!context.Sync.OnClient)
            {
                yield return WaitForSecondsCache.Get(0.05f);
            }
            // ✅ 모든 클라이언트에서 이벤트 구독
                PlayerSettingManager.OnPlayerSettingsChanged += OnPlayerSettingsChanged;
                if (CanLoadPlayerData())
                {
                    LoadPlayerData();
                }
                else
                {
                    // 기본값으로 초기화
                    data = CreateDefaultPlayerData();
                    LoadDefaultShootingData();
                
                    // 나중에 데이터 로딩을 위한 콜백 등록
                    RegisterDataLoadCallbacks();
                }   
        }
        
        protected override AgentData CreateDefaultAgentData()
        {
            return CreateDefaultPlayerData();
        }
        
        private PlayerData CreateDefaultPlayerData()
        {
            return new PlayerData(); // 기본 생성자 사용
        }
        
        private void LoadDefaultShootingData()
        {
            // 기본값으로 초기화된 ShootingData 인스턴스 생성
            shooting_data = new ShootingData();
        }

        private bool CanLoadPlayerData()
        {
            return GameDataManager.Instance && 
                   PlayerSettingManager.Instance && 
                   GameDataManager.Instance.IsDataInitialized &&
                   GameDataManager.Instance.isDataLocalLoaded &&
                   PlayerSettingManager.Instance.GetLocalPlayerSettings() != null;
        }

        protected override void Start()
        {
            base.Start();
            
            // PlayerData 사용
            if (PlayerData != null)
            {
                reviveCurrentCount = PlayerData.revival;
                currentDefence = PlayerData.defence;
            }
            // Death Event 구독
            OnReviveAbleDeathEvent += GameSystemSound.Instance.Player_DeadAlertSFX;
            OnReviveAbleDeathEvent += RegisterMark;
            OnReviveAbleDeathEvent += ScreenMarkAdd;

            OnRevive += ScreenMarkRemove;
            OnRevive += UnregisterMark;
            
            // 컴포넌트 초기화
            InitializeComponents();
        }



        private void InitializeComponents()
        {
            TryGetComponent(out context);
            
            if (context.Sprite)
            {
                originalColor = context.Sprite.color;
            }
        }
        
        

        private void LoadPlayerData()
        {
            if (_dataLoaded || _isLoadingData) return;
            
            _isLoadingData = true;
            
            try
            {
                var settings = PlayerSettingManager.Instance.GetPlayerSettings(context.Sync.OwnerId);
                if(settings!=null)
                {
                    var playerData = GameDataManager.Instance.GetPlayerDataById(settings.playerDataId);
                    var shootingData = GameDataManager.Instance.GetShootingDataById(playerData?.shootingDataId ?? 1);

                    if (playerData != null)
                    {
                        // 원본 데이터를 복제하여 독립 인스턴스 생성
                        data = CreatePlayerDataInstance(playerData);

                        if (shootingData != null)
                        {
                            shooting_data = CreateShootingDataInstance(shootingData);
                        }

                        _dataLoaded = true;

                        // 값들 새로고침
                        RefreshValuesAfterDataLoad();
                        OnDataUpdate?.Invoke();
                        LogManager.Log(LogCategory.Player, $"{gameObject.name} 플레이어 데이터 로딩 완료", this);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.LogError(LogCategory.Player, $"플레이어 데이터 로딩 실패: {ex.Message}");
            }
            finally
            {
                _isLoadingData = false;
            }
        }

        private void RefreshValuesAfterDataLoad()
        {
            // 런타임 중 데이터가 로딩된 경우 값들 업데이트
            if (PlayerData != null)
            {
                reviveCurrentCount = PlayerData.revival;
                bulletCurrentCount = shooting_data.magazineCapacity;
                currentHp = PlayerData.hp;
                currentDefence = PlayerData.defence;
                
                OnDataRefreshed?.Invoke();
            }
        }

        private void RegisterDataLoadCallbacks()
        {
            // GameDataManager 초기화 완료 시 데이터 로딩 시도
            if (GameDataManager.Instance)
            {
                StartCoroutine(CheckDataPeriodically());
            }
            
        }

        private IEnumerator CheckDataPeriodically()
        {
            while (!_dataLoaded && !_isLoadingData)
            {
                yield return WaitForSecondsCache.Get(0.5f); // 0.5초마다 체크
                if (CanLoadPlayerData())
                {
                    LoadPlayerData();
                }
            }
        }

        public void RefreshPlayerData()
        {
            _dataLoaded = false;
            _isLoadingData = false;
            LoadPlayerData();
        }
        
        /// <summary>
        /// PlayerData 인스턴스 생성 메서드
        /// </summary>
        private PlayerData CreatePlayerDataInstance(PlayerData originalData)
        {
            // 원본 데이터를 복제하여 독립 인스턴스 생성
            var instance = new PlayerData(
                originalData.typeId,
                originalData.hp,
                originalData.speed,
                originalData.attackSpeed,
                originalData.shootingDataId,
                originalData.name,
                originalData.BaseDefence,
                originalData.defenceRecoverDelay,
                originalData.defenceRecoverAmountForFrame,
                originalData.Baserevival,
                originalData.skinId,
                originalData.visionRadius,
                originalData.camouflageHoldingTime,
                originalData.camouflageCooldown,
                originalData.healAmount
            );
            
            LogManager.Log(LogCategory.Player, 
                $"플레이어 데이터 인스턴스 생성: {originalData.name}", this);
            
            return instance;
        }

        /// <summary>
        /// ShootingData 인스턴스 생성 매서드
        /// </summary>
        /// <param name="originalData"></param>
        /// <returns></returns>
        private ShootingData CreateShootingDataInstance(ShootingData originalData)
        {
            var instance = new ShootingData(
                originalData.typeId,
                originalData.shotDelay,
                originalData.burstCount,
                originalData.reloadTime,
                originalData.magazineCapacity,
                originalData.fullAuto,
                originalData.shotAngle,
                originalData.bulletSpeed,
                originalData.bulletSize,
                originalData.lifeCycle,
                originalData.bulletDamage,
                originalData.piercingCount
            );
            
            LogManager.Log(LogCategory.Player, 
                $"플레이어 슈팅 데이터 인스턴스 생성: {instance.typeId}", this);
            
            return instance;

        }
        
        /// <summary>
        /// 스탯 수정자 적용 메서드
        /// </summary>
        public void ApplyStatModifier(StatType statType, StatModifier<float> modifier)
        {
            if (data == null) return;
            PlayerData playerData = PlayerData;
            ShootingData shootingData = shooting_data;
            switch(statType)
            {
                case StatType.Speed:
                    playerData.AddSpeedModifier(modifier);
                    break;
                case StatType.Hp:
                    playerData.AddHpModifier(modifier);
                    break;
                case StatType.Defence:
                    playerData.AddDefenceModifier(modifier);
                    break;
                case StatType.BulletSpeed:
                    shootingData.AddBulletSpeedModifier(modifier);
                    break;
                case StatType.BulletDamage:
                    shootingData.AddBulletDamageModifier(modifier);
                    break;
                case StatType.BulletSize:
                    shootingData.AddBulletSizeModifier(modifier);
                    break;  
                case StatType.MagazineCapacity:
                    var magazineCapacity = new StatModifier<int>(
                        modifier.id,modifier.percentBonus, modifier.description);
                    shootingData.AddMagazineCapacityModifier(magazineCapacity);
                    break;
                case StatType.ShotDelay:
                    shootingData.AddShotDelayModifier(modifier);
                    break;
                case StatType.ReloadTime:
                    shootingData.AddReloadTimeModifier(modifier);
                    break;
                // 추가 스탯들은 나중에 구현
                default:
                    LogManager.LogWarning(LogCategory.Player,
                        $"지원되지 않는 스탯 타입: {statType}", this);
                    break;
            }
            
            LogManager.Log(LogCategory.Player, 
                $"스탯 수정자 적용: {statType} -> {modifier.description}", this);
        }

        protected override void DataLoad()
        {
            // 호환성을 위해 유지하지만 더 이상 사용하지 않음
        }

        // ✅ UI 업데이트 로직 제거, 순수 데미지 계산만
        public override bool TakeDamage(float damage, Vector2 hitDirection)
        {
            bool criticalHit = false;
            if (isDead) return criticalHit;
        
            // 후방 피해 확인
            if (context.Controller)
            {
                float playerAngle = context.Shooter.LookAngle + 180;
                float hitAngle = Mathf.Atan2(hitDirection.y, hitDirection.x) * Mathf.Rad2Deg;
            
                // 각도 차이 계산 (절대값)
                float angleDifference = Mathf.Abs(Mathf.DeltaAngle(playerAngle, hitAngle));
            
                // 후방 피해 확인 (각도 차이가 135도 이상일 때)
                if (angleDifference >= BACK_ATTACK_ANGLE_THRESHOLD)
                {
                    damage *= 2f;
                    criticalHit = true;
                    LogManager.Log(LogCategory.Player, $"{gameObject.name} 후방 피해! 데미지 2배 증폭: {damage}", this);
                }
            }
            
            // Heal 중 피해 받을 시 Heal 취소 루틴 진입
            if(playerHeal == null)
                playerHeal = context.Component.GetPComponent<PlayerHealComponent>() as PlayerHealComponent;
            if (playerHeal is { headling: true })
            {
                playerHeal.StopHealing();
                context.Sync.OffHeal_Client();
            }
        
            // ✅ 기본 데미지 처리만 수행 (UI 업데이트 제거)
            currentHp -= damage;
            currentHp = Mathf.Clamp(currentHp, 0, PlayerData?.hp ?? 100f);
            OnDeathCheckSequence();
            return criticalHit;
        }


        public bool Heal(float healAmount)
        {
            if(isDead) return false;
            
            float result = currentHp + healAmount;
            currentHp = Mathf.Clamp(result, 0, PlayerData?.hp ?? 100f);
            context.Sync.UpdateHealSyncVars();
            if (result > PlayerData?.hp)
                return false;
            return true;
        }

        // 아직 서버 단계
        public void OnDeathCheckSequence()
        {
            if (currentHp <= 0)
            {
                currentHp = 0;
                currentDefence = 0;
                PlayerCamouflageComponent camouflageComponent = context.Component.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
                if (camouflageComponent != null && camouflageComponent.IsDisguised)
                {
                    camouflageComponent.ServerDeactivateDisguise();
                    return;
                }
                if (reviveCurrentCount >= 0)
                {
                    StartCoroutine(DeathSequence());
                    OnReviveAbleDeathEvent?.Invoke();
                }
                else
                {
                    // 부활 횟수가 없으면 바로 사망
                    OnRealDeath();
                }
            }
        }

        public void OnClientDeathSequence()
        {
            StartCoroutine(DeathSequence());   
        }
        /// <summary>
        /// 기절 상태 - 플레이어 전용
        /// </summary>
        /// <returns></returns>
        protected override IEnumerator DeathSequence()
        {
            isDead = true;

            if(context.Input)
                context.Input.enabled = false;
            if (context.PlayerInteract)
                context.PlayerInteract.enabled = false;

            if (context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() is PlayerSkeletonAnimationComponent animComponent)
            {
                animComponent.DeathShot();
            }

            yield return null;
        }

        /// <summary>
        /// 부활 기능 호출
        /// </summary>
        public void Revive()
        {
            if (!isDead) return;

            // 기본 상태값 초기화
            isDead = false;
            IsCrackDefence = false;
            currentHp = PlayerData?.hp ?? 100f;
            currentDefence = PlayerData?.defence ?? 100f;
            reviveCurrentCount--;
            
            OnRevive?.Invoke();
        }

        public void ComponentRevive()
        {
            // 플레이어 컨트롤 활성화
            if (context.Controller)
                context.Controller.enabled = true;
            if(context.Input)
                context.Input.enabled = true;
            if(context.PlayerInteract)
                context.PlayerInteract.enabled = true;
        }

        public void ClientReviveEffect()
        {
            if (context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() is PlayerSkeletonAnimationComponent animComponent)
            {
                animComponent.ReviveShot();
            }
        }
        /// <summary>
        /// 확정적 죽음 호출
        /// </summary>
        public override void OnRealDeath()
        {
            // 부활 횟수가 없을 때의 최종 사망 처리
            if (context.Controller)
            {
                context.Controller.enabled = false;
            }
            if (context.Input)
            {
                context.Input.enabled = false;
            }
            if(context.PlayerInteract)
                context.PlayerInteract.enabled = true;
            
            InstanceFinder.NetworkManager.ServerManager.Despawn(this.gameObject);
            
            base.OnRealDeath();
        }

        public void OnClientDeath()
        {
            OnRealDeath();
        }

        private void ScreenMarkAdd()
        {
            if(ScreenMarkManager.Instance)
                ScreenMarkManager.Instance.AddTarget(context.Sync.NetworkObject, TrackedObject.TrackedObjectType.Player);
        }

        private void ScreenMarkRemove()
        {
            if(ScreenMarkManager.Instance)
                ScreenMarkManager.Instance.RemoveTarget(context.Sync.NetworkObject);
        }
        
        private void RegisterMark()
        {
            MapMarkContext Markcontext =
                new MapMarkContext(MapMarkType.Mark, MarkType.PLAYER,transform.position, Color.magenta, Vector2.zero);
            if(MapMarkManager.instance)
                MarkId = MapMarkManager.instance.Register(Markcontext);
        }
        private void UnregisterMark()
        {
            MapMarkManager.instance?.Unregister(MarkId);
        }

        private void OnPlayerSettingsChanged(int clientId)
        {    
            // ✅ 서버와 해당 플레이어 모두 데이터 새로고침
            bool shouldUpdate = false;
    
            // 서버인 경우: 해당 클라이언트의 플레이어인지 확인
            if (InstanceFinder.IsServerStarted && context.Sync?.Owner != null)
            {
                shouldUpdate = context.Sync.Owner.ClientId == clientId;
                if (shouldUpdate)
                {
                    LogManager.Log(LogCategory.Player, 
                        $"[서버] {gameObject.name} PlayerSettings 변경 감지 - ClientId: {clientId}", this);
                }
            }
            // 클라이언트인 경우: 자신의 플레이어인지 확인  
            else if (InstanceFinder.IsClientStarted && InstanceFinder.ClientManager?.Connection != null)
            {
                shouldUpdate = InstanceFinder.ClientManager.Connection.ClientId == clientId;
                if (shouldUpdate)
                {
                    LogManager.Log(LogCategory.Player,
                       $"[클라이언트] PlayerSettings 변경 감지 - ClientId: {clientId}", this);
                }
            }
    
            if (shouldUpdate)
            {
                RefreshPlayerData();
            }
        }
        
        private bool IsLocalPlayer()
        {
            // PlayerNetworkSync를 통해 소유권 확인
            if(!context)
                return false;
            if (context?.Sync && context.Sync.IsOwner)
            {
                return true;
            }
            
            // NetworkSync가 없거나 아직 초기화되지 않은 경우, 다른 방법으로 확인
            var networkSync = GetComponent<PlayerNetworkSync>();
            return networkSync && networkSync.IsOwner;
        }        
        
        public override void UpdateBulletCount(float count)
        {
            base.UpdateBulletCount(count);
            if(bulletCurrentCount <= 0)
                context.AgentUI.EmptyCursor(true);
        }

        private void OnDestroy()
        {
            PlayerSettingManager.OnPlayerSettingsChanged -= OnPlayerSettingsChanged;
            
            // Death Event 구독
            OnReviveAbleDeathEvent -= GameSystemSound.Instance.Player_DeadAlertSFX;
            OnReviveAbleDeathEvent -= RegisterMark;
            OnReviveAbleDeathEvent -= ScreenMarkAdd;

            OnRevive -= ScreenMarkRemove;
            OnRevive -= UnregisterMark;
            
        }
    }
}
