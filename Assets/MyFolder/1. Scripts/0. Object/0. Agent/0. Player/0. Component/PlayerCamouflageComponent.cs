using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy;
using MyFolder._1._Scripts._0._Object._4._Shooting;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using Spine.Unity;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    /// <summary>
    /// 플레이어 위장 시스템
    /// - 플레이어가 적군으로 변장하는 기능 제공
    /// - 외형, 스탯, 애니메이션, Tag/Layer 변경
    /// - 일정 시간 후 자동 해제, 쿨타임 적용
    /// </summary>
    public class PlayerCamouflageComponent : IPlayerUpdateComponent
    {
        #region 설정값

        private float disguiseDuration = 10f; // 위장 지속 시간
        private float cooldownTime = 15f; // 위장 쿨타임

        #endregion

        #region 상태

        private PlayerContext context;
        private PlayerStatus status;
        private PlayerNetworkSync sync;
        private GameObject gameObject;
        private PlayerHealComponent heal;

        private bool isDisguised = false;
        private float currentDisguiseTime;
        private float currentCooldownTime;
        private bool isOnCooldown = false;

        #endregion

        #region 백업 데이터

        private PlayerData originalPlayerData; // 버프 포함된 원본 PlayerData
        private ShootingData originalShootingData; // 원본 ShootingData
        private float originalHp;
        private string originalTag;
        private int originalLayer;

        #endregion

        #region 적군 데이터

        private EnemyData currentEnemyData;
        private ShootingData currentEnemyShootingData;
        private ushort currentDisguisedEnemyTypeId;

        // 플레이어 애니메이션 → 적군 애니메이션 매핑
        private Dictionary<string, AnimationReferenceAsset> animationMapping;

        #endregion

        #region Properties

        public bool IsDisguised => isDisguised;
        public bool IsOnCooldown => isOnCooldown;
        public float RemainingDisguiseTime => currentDisguiseTime;
        public float RemainingCooldownTime => currentCooldownTime;

        #endregion

        #region IPlayerUpdateComponent 구현

        public void Start(PlayerContext _context)
        {
            context = _context;
            status = context.Status;
            sync = context.Sync;
            gameObject = context.gameObject;
            status.OnDataUpdate += ValueUpdate;
        }

        public void Stop()
        {
            status.OnDataUpdate -= ValueUpdate;
        }

        public void SetKeyEvent(PlayerInputControll inputControll)
        {
            if (inputControll)
            {
                inputControll.skill_1StartCallback += KeyEnter;
                inputControll.skill_1StopCallback += KeyExit;
            }
        }

        public void KeyEnter()
        {
            // 회복 진행중엔 사용 불가
            if (heal == null)
                heal = context.Component.GetPComponent<PlayerHealComponent>() as PlayerHealComponent;
            if (heal is { headling: true })
                return;
            if (isDisguised)
                DeactivateDisguise();
            else
                ActivateDisguise();
        }

        public void KeyPress()
        {
        }

        public void KeyExit()
        {
        }

        public void Update()
        {
            // 쿨타임 처리
            if (isOnCooldown)
            {
                currentCooldownTime -= Time.deltaTime;
                context.AgentUI.UpdateCamouflageCooldownUI(currentCooldownTime, cooldownTime);
                if (currentCooldownTime <= 0)
                {
                    isOnCooldown = false;
                }
            }

            // 위장 상태 처리
            if (isDisguised)
            {
                currentDisguiseTime -= Time.deltaTime;
                context.AgentUI.UpdateCamouflageDisguiseUI(currentDisguiseTime, disguiseDuration);
                // 시간 만료 시 자동 해제
                if (currentDisguiseTime <= 0)
                {
                    DeactivateDisguise();
                    return;
                }

            }
        }

        private void ValueUpdate()
        {
            disguiseDuration = status.PlayerData.camouflageHoldingTime;
            cooldownTime = status.PlayerData.camouflageCooldown;
        }

        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        #endregion

        #region 위장 활성화

        /// <summary>
        /// 위장 활성화 요청
        /// </summary>
        public void ActivateDisguise()
        {
            if (isDisguised)
            {
                Debug.LogWarning("[PlayerCamouflage] 이미 위장 중입니다");
                return;
            }

            if (isOnCooldown)
            {
                Debug.LogWarning($"[PlayerCamouflage] 위장 쿨타임 중입니다 (남은 시간: {currentCooldownTime:F1}초)");
                return;
            }

            if (!SpawnerManager.instance)
            {
                Debug.LogError("[PlayerCamouflage] SpawnerManager를 찾을 수 없습니다");
                return;
            }

            ushort enemyTypeId = SpawnerManager.instance.EnemyLevel;

            if (sync.IsOwner)
            {
                sync.RequestDisguiseActivation(enemyTypeId);
            }
        }

        /// <summary>
        /// 위장 적용 (네트워크 동기화 후 호출)
        /// </summary>
        public void ApplyDisguise(ushort enemyTypeId)
        {
            context.StartCoroutine(ApplyDisguiseCoroutine(enemyTypeId));
        }

        private IEnumerator ApplyDisguiseCoroutine(ushort enemyTypeId)
        {
            // 1. 원본 데이터 백업
            BackupOriginalData();

            // 2. 적군 데이터 로드
            yield return LoadEnemyData(enemyTypeId);

            if (currentEnemyData == null)
            {
                Debug.LogError($"[PlayerCamouflage] 적군 데이터 로드 실패 (TypeID: {enemyTypeId})");
                yield break;
            }



            // 3. 애니메이션 매핑 구축
            BuildAnimationMapping();

            // 4. 스탯 교체
            SwapToEnemyData();

            // 5. 비주얼 교체
            // 5. 임시 SpriteRenderer 변경
            SwapVisuals();

            // 5-1. 비주얼 전환 대기 (Initialize 완료 대기)
            yield return null;

            // 6. Tag/Layer 변경
            gameObject.tag = "Enemy";
            gameObject.layer = LayerMask.NameToLayer("Enemy");

            // 7. 방패 비활성화
            DisableShield();

            // 8. HP 즉시 반영 (로컬)
            status.currentHp = currentEnemyData.hp;

            // 9. HP 네트워크 동기화 (다른 클라이언트용)
            if (sync.IsServerInitialized)
            {
                sync.RequestUpdateHP(currentEnemyData.hp);
            }

            // 10. 상태 업데이트
            isDisguised = true;
            currentDisguisedEnemyTypeId = enemyTypeId;
            currentDisguiseTime = disguiseDuration;

            // 11. 애니메이션 재생 강제
            if (context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() is PlayerSkeletonAnimationComponent
                animComponent)
            {
                animComponent.ResetAnimationState();

                // 현재 상태에 맞는 애니메이션 재생
                if (context.Controller && context.Controller.IsMoving)
                    animComponent.MoveStart(context.Controller.MoveDirection);
                else
                    animComponent.IdleStart();
            }

            // 12. 적군 추적 불가 설정
            context.Sync.SetCanSee(false);

            // 13. UI 비활성화
            DisableUI();

            Debug.Log($"[PlayerCamouflage] 위장 활성화 완료 (TypeID: {enemyTypeId}, HP: {currentEnemyData.hp})");
        }

        #endregion

        #region 위장 해제

        /// <summary>
        /// 위장 해제 요청
        /// </summary>
        public void DeactivateDisguise()
        {
            if (!isDisguised) return;

            if (sync.IsOwner)
            {
                sync.RequestDisguiseDeactivation();
            }
        }

        public void ServerDeactivateDisguise()
        {
            if (!isDisguised) return;
            sync.ServerRequestDisguiseActivation();
        }

        /// <summary>
        /// 위장 해제 적용 (네트워크 동기화 후 호출)
        /// </summary>
        public void ApplyDeactivateDisguise()
        {
            // 1. 스탯 복원
            RestoreOriginalData();

            // 2. 비주얼 복원
            RestoreVisuals();

            // 3. Tag/Layer 복원
            gameObject.tag = originalTag;
            gameObject.layer = originalLayer;

            // 4. 방패 활성화 (깨지지 않은 경우만)
            EnableShield();

            // 5. HP 즉시 복원 (로컬)
            status.currentHp = originalHp;

            // 6. HP 네트워크 동기화 (다른 클라이언트용)
            if (sync.IsServerInitialized)
            {
                sync.RequestUpdateHP(originalHp);
            }


            // 8. 매핑 테이블 정리
            animationMapping?.Clear();
            animationMapping = null;


            // 7. 상태 업데이트
            isDisguised = false;
            isOnCooldown = true;
            currentCooldownTime = cooldownTime;

            // 9. 애니메이션 재생 강제
            var animComponent =
                context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() as
                    PlayerSkeletonAnimationComponent;
            if (animComponent != null)
            {
                animComponent.ResetAnimationState();

                if (context.Controller && context.Controller.IsMoving)
                    animComponent.MoveStart(context.Controller.MoveDirection);
                else
                    animComponent.IdleStart();
            }

            // 10. 적군 추적 활성화
            context.Sync.SetCanSee(true);

            // 11. UI 관리
            EnableUI();

            Debug.Log("[PlayerCamouflage] 위장 해제 완료");
        }

        #endregion

        #region 데이터 관리

        private void BackupOriginalData()
        {
            originalPlayerData = status.PlayerData;
            originalShootingData = status.GetShootingData;
            originalHp = status.currentHp;
            originalTag = gameObject.tag;
            originalLayer = gameObject.layer;
        }

        private IEnumerator LoadEnemyData(ushort enemyTypeId)
        {
            while (!GameDataManager.Instance || !GameDataManager.Instance.IsDataInitialized)
            {
                yield return WaitForSecondsCache.Get(0.1f);
            }

            currentEnemyData = GameDataManager.Instance.GetEnemyDataById(enemyTypeId);

            if (currentEnemyData != null && currentEnemyData.shootingDataId > 0)
            {
                currentEnemyShootingData = GameDataManager.Instance.GetShootingDataById(
                    currentEnemyData.shootingDataId);
            }
        }

        private void SwapToEnemyData()
        {
            // ✅ EnemyData의 스탯으로 새로운 PlayerData 인스턴스 생성
            // (PlayerData 타입 유지 + 버프 없는 깨끗한 상태)
            if (currentEnemyData != null)
            {
                var disguisePlayerData = new PlayerData(
                    typeId: currentEnemyData.typeId,
                    _hp: currentEnemyData.hp,
                    _speed: currentEnemyData.speed,
                    _attackSpeed: currentEnemyData.speed,
                    _shootingDataId: currentEnemyData.shootingDataId,
                    _name: $"Disguised_",
                    defence: 0f, // 방어력 없음
                    defenceRecoverDelay: 999f, // 방어력 회복 안 됨
                    defenceRecoverAmountForFrame: 0f,
                    _revival: originalPlayerData?.Baserevival ?? 0, // 부활 횟수는 원본 유지
                    skinID: currentEnemyData.typeId,
                    VisionRadius: originalPlayerData.visionRadius,
                    CamouflageHoldingTime: originalPlayerData.camouflageHoldingTime,
                    CamouflageCooldown: originalPlayerData.camouflageCooldown,
                    HealAmount: originalPlayerData.healAmount
                );

                status.SetData = disguisePlayerData;
                // ✅ ShootingData 교체
                status.SetShootingData = currentEnemyShootingData;

                // ✅ 런타임 스탯 변경
                status.currentHp = currentEnemyData.hp;
                status.currentDefence = 0;
            }
        }

        private void RestoreOriginalData()
        {
            status.SetData = originalPlayerData;
            status.SetShootingData = originalShootingData;
            status.currentHp = originalHp;

            if (originalPlayerData != null)
            {
                // ✅ 방어력을 위장 전 방어력으로 복원 (깨지지 않은 상태로 초기화)
                status.currentDefence = originalPlayerData.defence;
                status.IsCrackDefence = false;
            }
        }

        #endregion

        #region 비주얼 관리

        private void SwapVisuals()
        {
            var enemySet = context.EnemyAnimationSet;

            if (context.Skeleton && enemySet && enemySet.skeletonData)
            {
                PlayerSkeletonAnimationComponent animComponent =
                    context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() as
                        PlayerSkeletonAnimationComponent;
                bool wasEnabled = context.Skeleton.enabled;
                context.Skeleton.transform.localScale = new Vector3(0.8f, 0.8f, 1f);
                context.Skeleton.transform.localPosition = Vector3.zero;
                context.Skeleton.enabled = false;
                context.Skeleton.ClearState();
                context.Skeleton.skeletonDataAsset = enemySet.skeletonData;
                context.Skeleton.initialSkinName = "default";
                context.Skeleton.Initialize(true);

                context.Skeleton.skeleton.SetSkin("default");
                context.Skeleton.Skeleton.SetSlotsToSetupPose();

                if (context.Skeleton.AnimationState != null)
                {
                    context.Skeleton.AnimationState.ClearTracks();
                }

                context.Skeleton.enabled = wasEnabled;
                // TODO: 여기에 위장 전환 이펙트 재생 (깜빡임 방지)
                // PlayDisguiseEffect();

                context.ShotPoint.localPosition = enemySet.shotPointOffset;
            }
        }

        private void RestoreVisuals()
        {
            var playerSet = context.PlayerAnimationSet;

            PlayerSkeletonAnimationComponent animComponent =
                context.Component?.GetPComponent<PlayerSkeletonAnimationComponent>() as
                    PlayerSkeletonAnimationComponent;

            if (context.Skeleton && playerSet && playerSet.skeletonData)
            {
                bool wasEnabled = context.Skeleton.enabled;

                context.Skeleton.transform.localScale = Vector3.one;
                context.Skeleton.transform.localPosition = new Vector3(0, -2.2f, 0);
                context.Skeleton.enabled = false;
                // ✅ SkeletonDataAsset 복원 및 초기화
                context.Skeleton.ClearState();
                context.Skeleton.skeletonDataAsset = playerSet.skeletonData;
                animComponent?.SkinRevival();

                context.Skeleton.Initialize(true);

                animComponent?.SkinReset();


                if (context.Skeleton.AnimationState != null)
                {
                    context.Skeleton.AnimationState.ClearTracks();
                }

                context.Skeleton.enabled = wasEnabled;
                // TODO: 여기에 위장 해제 이펙트 재생 (깜빡임 방지)
                // PlayDisguiseEffect();

                context.ShotPoint.localPosition = playerSet.shotPointOffset;
            }
        }


        #endregion

        #region 애니메이션 매핑

        /// <summary>
        /// 플레이어 애니메이션 → 적군 애니메이션 매핑 테이블 구축
        /// </summary>
        private void BuildAnimationMapping()
        {
            animationMapping = new Dictionary<string, AnimationReferenceAsset>();

            var playerSet = context.PlayerAnimationSet;
            var enemySet = context.EnemyAnimationSet;

            if (!playerSet || !enemySet) return;

            // Idle 애니메이션 매핑
            MapAnimation(playerSet.idle_down_, enemySet.idle_down_);
            MapAnimation(playerSet.idle_down_r, enemySet.idle_down_);
            MapAnimation(playerSet.idle_up_, enemySet.idle_up_);
            MapAnimation(playerSet.idle_up_r, enemySet.idle_up_);

            // Walk 애니메이션 매핑
            MapAnimation(playerSet.walk_down_, enemySet.walk_down_);
            MapAnimation(playerSet.walk_down_r, enemySet.walk_down_);
            MapAnimation(playerSet.walk_up_, enemySet.walk_up_);
            MapAnimation(playerSet.walk_up_r, enemySet.walk_up_);

            // Death 애니메이션 매핑
            MapAnimation(playerSet.death, enemySet.death);

            // Shoot, Revival 등은 적군에 없으므로 자동으로 NULL 매핑됨

            Debug.Log($"[PlayerCamouflage] 애니메이션 매핑 테이블 생성 완료 ({animationMapping.Count}개)");
        }

        /// <summary>
        /// 개별 애니메이션 매핑
        /// </summary>
        private void MapAnimation(AnimationReferenceAsset playerAnim, AnimationReferenceAsset enemyAnim)
        {
            if (!playerAnim || playerAnim.Animation == null)
                return;

            // Spine Animation 이름을 키로 사용
            string animName = playerAnim.name;

            // enemyAnim이 NULL이거나 Animation이 NULL이면 매핑하지 않음
            // → GetMappedAnimation()에서 NULL 반환됨
            if (enemyAnim && enemyAnim.Animation != null)
            {
                animationMapping[animName] = enemyAnim;
            }
        }

        /// <summary>
        /// 플레이어 애니메이션을 적군 애니메이션으로 변환
        /// </summary>
        /// <param name="playerAnim">플레이어 애니메이션</param>
        /// <returns>적군 애니메이션 (없으면 NULL)</returns>
        public AnimationReferenceAsset GetMappedAnimation(AnimationReferenceAsset playerAnim)
        {
            if (!isDisguised || animationMapping == null)
                return playerAnim;

            if (!playerAnim || playerAnim.Animation == null)
                return null;

            // Spine Animation 이름으로 조회
            string animName = playerAnim.name;

            if (animationMapping.TryGetValue(animName, out var enemyAnim))
            {
                return enemyAnim; // 매핑된 적군 애니메이션 반환
            }

            // 매핑 없음 → 애니메이션 재생 억제
            return null;
        }

        #endregion

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
                if (!status.IsCrackDefence)
                {
                    context.DefenceBall.gameObject.SetActive(true);
                }
            }
        }

        #endregion

        #region UI관리

        private void DisableUI()
        {
            context.DefenceBarUI.SetActive(false);
        }

        private void EnableUI()
        {
            context.DefenceBarUI.SetActive(true);
        }

        #endregion
    }
}

