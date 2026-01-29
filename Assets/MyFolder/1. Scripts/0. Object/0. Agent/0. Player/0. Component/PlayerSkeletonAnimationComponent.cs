using System.Collections.Generic;
using MyFolder._1._Scripts._10._Sound;
using MyFolder._1._Scripts._3._SingleTone;
using Spine;
using Spine.Unity;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._0._Component
{
    /// <summary>
    /// 플레이어 스파인 애니메이션 관리 컴포넌트
    /// - 4방향 애니메이션 지원
    /// - 위장 시스템 통합
    /// </summary>
    public class PlayerSkeletonAnimationComponent : IPlayerUpdateComponent
    {
        private static readonly int Camouflage = Animator.StringToHash("Camouflage");
        private PlayerContext context;
        private SkeletonAnimation skeletonAnimation;
        
        private Dictionary<int, string> currentAnimationPerTrack = new Dictionary<int, string>();
        private enum AnimState { MOVE,IDLE }

        private AnimState state;
        
        // SkeletonDataAsset 변경 감지
        private SkeletonDataAsset currentSkeletonData;
        
        // 마지막 이동 방향
        private Vector2 lastDirection = Vector2.down;
        
        // 시야 상하 방향
        private int updownCursor = 0;

        public int UpdownCursor
        {
            get {return updownCursor; }
            set {
                updownCursor = value;
                UpdateUpDown();
            }
        }

        private bool animatingAble = true;

        // 귀 움직임 타임
        private float EarMinTime = 1,EarMaxTime = 2, CurrentEarTime = 0;
        private bool EarTime = false;
        
        // 0 - 좌 / 1 - 우
        private int AimDirection = 1;
        
        public int SetAimDirection { set => AimDirection = value; }
        
        #region 초기화
        
        public void Start(PlayerContext context)
        {
            this.context = context;
            this.skeletonAnimation = context.Skeleton;
            this.currentSkeletonData = skeletonAnimation.skeletonDataAsset;
            
            context.Controller.OnMoveCallback += MoveStart;
            context.Controller.OffMoveCallback += MoveEnd;
            
            IdleStart();
            AimStart();
        }
        
        public void Stop() { }
        
        public void SetKeyEvent(PlayerInputControll inputControll) { }
        
        /// <summary>
        /// 애니메이션 상태 초기화 (위장 전환 시 호출)
        /// </summary>
        public void ResetAnimationState()
        {
            if(currentAnimationPerTrack == null)
                currentAnimationPerTrack = new Dictionary<int, string>();
            currentAnimationPerTrack.Clear();
        }
        
        #endregion
        
        #region 방향 계산
        
        /// <summary>
        /// 방향 벡터를 4방향 인덱스로 변환
        /// </summary>
        /// <returns>0:down_left, 1:down_right, 2:up_left, 3:up_right</returns>
        private int GetDirectionIndex(Vector2 direction)
        {
            // 거의 0이면 lastDirection 사용
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = lastDirection;
            }
            else
            {
                lastDirection = direction;
            }
            
            // X, Y 부호로 4방향 결정
            bool isRight = direction.x > 0;  // X가 양수면 오른쪽
            bool isUp = direction.y > 0;     // Y가 양수면 위쪽
            
            if (!isUp && !isRight)
            {
                if (((PlayerCamouflageComponent)context.Component.GetPComponent<PlayerCamouflageComponent>()).IsDisguised)
                {
                    context.Skeleton.Skeleton.ScaleX = 1;   
                }
                // 좌하
                return 0;
            } 
            if (!isUp && isRight)
            {
                // 우하
                if (((PlayerCamouflageComponent)context.Component.GetPComponent<PlayerCamouflageComponent>()).IsDisguised)
                {
                    context.Skeleton.Skeleton.ScaleX = -1;   
                }
                return 1;
            } 
            if (isUp && !isRight)  
            {
                if (((PlayerCamouflageComponent)context.Component.GetPComponent<PlayerCamouflageComponent>()).IsDisguised)
                {
                    context.Skeleton.Skeleton.ScaleX = 1;   
                }
                return 2; 
                // 좌상
            } 
            if (isUp && isRight)  
            {
                if (((PlayerCamouflageComponent)context.Component.GetPComponent<PlayerCamouflageComponent>()).IsDisguised)
                {
                    context.Skeleton.Skeleton.ScaleX = -1;   
                }
                return 3; 
                // 우상
            } 
            return 0; // fallback
        }

        private bool IsReverse(int directionIndex)
        {
            bool isReverse = (directionIndex == 0 || directionIndex == 2)&&AimDirection == 1 ||
                             (directionIndex == 1 || directionIndex == 3)&&AimDirection == 0;
            return isReverse;
        }
        #endregion
        
        #region 애니메이션 선택
        
        
        /// <summary>
        /// 현재 방향에 맞는 Idle 애니메이션 가져오기
        /// </summary>
        private AnimationReferenceAsset GetIdleAnimation()
        {
            var animSet = context.PlayerAnimationSet;
            if (!animSet)
            {
                LogManager.LogWarning(LogCategory.Player,"[PlayerSkeletonAnim] PlayerAnimationSet이 NULL입니다");
                return null;
            }
            
            int dirIndex = GetDirectionIndex(lastDirection);
            bool isReverse = IsReverse(dirIndex);
            return animSet.GetIdleAnimation(updownCursor,isReverse);
        }
        
        /// <summary>
        /// 방향에 맞는 Walk 애니메이션 가져오기
        /// </summary>
        private AnimationReferenceAsset GetWalkAnimation(Vector2 direction)
        {
            var animSet = context.PlayerAnimationSet;
            if (!animSet)
            {
                LogManager.LogWarning(LogCategory.Player,"[PlayerSkeletonAnim] PlayerAnimationSet이 NULL입니다");
                return null;
            }
            
            int dirIndex = GetDirectionIndex(direction);
            bool isReverse = IsReverse(dirIndex);
            return animSet.GetWalkAnimation(updownCursor,isReverse);
        }
        
        /// <summary>
        /// Shoot 애니메이션 가져오기
        /// </summary>
        private AnimationReferenceAsset GetShootAnimation()
        {
            var animSet = context.PlayerAnimationSet;
            if (!animSet)
            {
                LogManager.LogError(LogCategory.Player,"[PlayerSkeletonAnim] PlayerAnimationSet이 NULL입니다");
                return null;
            }
            
            return animSet.GetShootAnimation(updownCursor);
        }
        
        #endregion
        
        #region 애니메이션 재생 (위장 통합)
        
        
        /// <summary>
        /// 애니메이션 재생 (위장 시스템 통합)
        /// </summary>
        private TrackEntry SetAnimation(int Track, AnimationReferenceAsset anim, bool loop, float timeScale = 1)
        {
            if (!animatingAble)
                return null;
            
            if (!skeletonAnimation)
            {
                return null;
            }

            // NULL 체크
            if (!anim)
            {
                LogManager.LogWarning(LogCategory.Player,"[PlayerSkeletonAnim] AnimationReferenceAsset이 NULL입니다");
                return null;
            }
            
            if (anim.Animation == null)
            {
                LogManager.LogWarning(LogCategory.Player,$"[PlayerSkeletonAnim] Animation이 NULL입니다: {anim.name}");
                return null;
            }
            
            string animName = anim.Animation.Name;
    
            // ✅ 트랙별로 중복 체크
            if (currentAnimationPerTrack.TryGetValue(Track, out string currentAnim) && currentAnim == animName)
            {
                return null;
            }
            
            // SkeletonDataAsset 변경 감지 (위장 전환 시)
            if (currentSkeletonData != skeletonAnimation.skeletonDataAsset)
            {
                currentSkeletonData = skeletonAnimation.skeletonDataAsset;
                currentAnimationPerTrack.Clear();
                
                LogManager.Log(LogCategory.Player,"[PlayerSkeletonAnim] SkeletonDataAsset 변경 감지 - 애니메이션 상태 초기화");
            }
            
            
            // 위장 시스템 통합: 매핑된 애니메이션 사용
            AnimationReferenceAsset targetAnim = anim;
            PlayerCamouflageComponent camouflageComponent = context.Component?.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            
            if (camouflageComponent != null && camouflageComponent.IsDisguised)
            {
                targetAnim = camouflageComponent.GetMappedAnimation(anim);
                
                // NULL이면 애니메이션 재생 억제 (현재 애니메이션 유지)
                if (!targetAnim)
                {
                    LogManager.LogError(LogCategory.Player,$"[PlayerSkeletonAnim] 적군 애니메이션 매핑 없음: {animName} - 애니메이션 유지");
                    return null;
                }
            }
            // Spine API 호출
            TrackEntry trackEntry = new TrackEntry();
            if(skeletonAnimation && skeletonAnimation.skeletonDataAsset && targetAnim)
                trackEntry = skeletonAnimation.AnimationState.SetAnimation(Track, targetAnim, loop);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = timeScale;
                currentAnimationPerTrack[Track] = targetAnim.Animation.Name;
            }

            return trackEntry;
        }
        
       private TrackEntry SetAnimationAnyway(int Track, AnimationReferenceAsset anim, bool loop, float timeScale = 1)
        {
            
            if (!skeletonAnimation)
            {
                return null;
            }

            // NULL 체크
            if (!anim)
            {
                LogManager.LogWarning(LogCategory.Player,"[PlayerSkeletonAnim] AnimationReferenceAsset이 NULL입니다");
                return null;
            }
            
            if (anim.Animation == null)
            {
                LogManager.LogWarning(LogCategory.Player,$"[PlayerSkeletonAnim] Animation이 NULL입니다: {anim.name}");
                return null;
            }
            
            string animName = anim.Animation.Name;
    
            // ✅ 트랙별로 중복 체크
            if (currentAnimationPerTrack.TryGetValue(Track, out string currentAnim) && currentAnim == animName)
            {
                return null;
            }
            
            // SkeletonDataAsset 변경 감지 (위장 전환 시)
            if (currentSkeletonData != skeletonAnimation.skeletonDataAsset)
            {
                currentSkeletonData = skeletonAnimation.skeletonDataAsset;
                currentAnimationPerTrack.Clear();
                
                LogManager.Log(LogCategory.Player,"[PlayerSkeletonAnim] SkeletonDataAsset 변경 감지 - 애니메이션 상태 초기화");
            }
            
            
            // 위장 시스템 통합: 매핑된 애니메이션 사용
            AnimationReferenceAsset targetAnim = anim;
            PlayerCamouflageComponent camouflageComponent = context.Component?.GetPComponent<PlayerCamouflageComponent>() as PlayerCamouflageComponent;
            
            if (camouflageComponent != null && camouflageComponent.IsDisguised)
            {
                targetAnim = camouflageComponent.GetMappedAnimation(anim);
                
                // NULL이면 애니메이션 재생 억제 (현재 애니메이션 유지)
                if (!targetAnim)
                {
                    LogManager.LogError(LogCategory.Player,$"[PlayerSkeletonAnim] 적군 애니메이션 매핑 없음: {animName} - 애니메이션 유지");
                    return null;
                }
            }
            // Spine API 호출
            TrackEntry trackEntry = new TrackEntry();
            if(skeletonAnimation && skeletonAnimation.skeletonDataAsset && targetAnim)
                trackEntry = skeletonAnimation.AnimationState.SetAnimation(Track, targetAnim, loop);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = timeScale;
                currentAnimationPerTrack[Track] = targetAnim.Animation.Name;
            }

            return trackEntry;
        }
        
        #endregion
        
        #region 액션 진입점

        private void UpdateUpDown()
        {
            switch (state)
            {
                case AnimState.IDLE:
                    IdleStart();
                    break;
                case AnimState.MOVE:
                    MoveStart(lastDirection);
                    break;
            }
        }
        public void IdleStart()
        {
            var idleAnim = GetIdleAnimation();
            SetAnimation(0, idleAnim, true);
            state = AnimState.IDLE;
        }
        
        public void MoveStart(Vector2 direction)
        {
            var walkAnim = GetWalkAnimation(direction);
            SetAnimation(0, walkAnim, true,1.2f);
            state = AnimState.MOVE;
        }

        public void MoveEnd()
        {
            IdleStart();
        }

        public void AimStart()
        {
            var Aim = context.PlayerAnimationSet.GetAimAnimation();
            SetAnimation(2, Aim, true);
        }
        
        public void BulletShot()
        {
            var shotAnim = GetShootAnimation();
            TrackEntry trackEntry = SetAnimation(1, shotAnim, false);
            // 공격 애니메이션이 끝나면 트랙1을 비워서 마지막 프레임에 머물지 않도록 함
            if (trackEntry != null)
            {
                trackEntry.MixDuration = 0;
                trackEntry.Complete += (entry) =>
                {
                    skeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
                    currentAnimationPerTrack.Remove(1);
                };
            }
        }

        public void EarShot()
        {
            if (!EarTime)
            {
                EarTime = true;
                CurrentEarTime = Random.Range(EarMinTime, EarMaxTime);
            }
            else
            {
                CurrentEarTime -= Time.deltaTime;
                if (!(CurrentEarTime <= 0)) return;
                var earAim = context.PlayerAnimationSet.GetEarAnimation();
                SetAnimation(3, earAim, false);
                EarTime = false;
            }
        }

        public void DeathShot()
        {
            var death = context.PlayerAnimationSet.GetDeathAnimation();
            
            animatingAble = false;
            TrackEntry trackEntry = SetAnimationAnyway(0, death, false);
            skeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
            skeletonAnimation.AnimationState.SetEmptyAnimation(2, 0f);
            skeletonAnimation.AnimationState.SetEmptyAnimation(3, 0f);
        }

        public void ReviveShot()
        {
            var revive = context.PlayerAnimationSet.GetRevivalAnimation();
            TrackEntry trackEntry = SetAnimationAnyway(0, revive, false);
            trackEntry.Complete += (entry) =>
            {
                animatingAble = true;
                AimStart();
            };
        }

        public void RevivalAttemptAnimationShot()
        {
            var revive = context.PlayerAnimationSet.GetRevivalAttemptAnimation();
            TrackEntry trackEntry = SetAnimation(0, revive, true);
            skeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
            skeletonAnimation.AnimationState.SetEmptyAnimation(2, 0f);
            skeletonAnimation.AnimationState.SetEmptyAnimation(3, 0f);
            if (trackEntry != null)
            {
                trackEntry.Complete += (entry) =>
                {
                    AimStart();
                };
            }
        }

        public void RevivalAttemptAnimationEnd()
        {
            var revive = context.PlayerAnimationSet.GetRevivalAttemptAnimation();
            TrackEntry trackEntry = SetAnimation(0, revive, true);
            skeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
            skeletonAnimation.AnimationState.SetEmptyAnimation(2, 0f);
            skeletonAnimation.AnimationState.SetEmptyAnimation(3, 0f);
            if (trackEntry != null)
            {
                trackEntry.Complete += (entry) =>
                {
                    AimStart();
                };
            }
        }

        public void CamouflageShot()
        {
            var camo = context.PlayerAnimationSet.GetCamouflage();
            TrackEntry trackEntry = SetAnimation(1, camo, false);
            trackEntry.Complete += (entry) =>
            {
                context.SmokeAnimator.SetTrigger(Camouflage);
                context.Sync.ApplyDisguiseActivation();
                context.Sfx.Play(PlayerSfxType.PlayerCamouflage);
            };
        }

        #endregion
        
        #region IPlayerUpdateComponent 나머지
        
        public void KeyEnter() { }
        public void KeyPress() { }
        public void KeyExit() { }

        public void Update()
        {
            EarShot();
        }
        public void FixedUpdate() { }
        public void LateUpdate() { }
        
        #endregion

        #region 스킨 변경

        private string baseSkin;

        public void SkinChange(string skinname)
        {
            skeletonAnimation.skeleton.SetSkin(skinname);
            baseSkin = skinname;
        }

        public void SkinRevival()
        {
            context.Skeleton.initialSkinName = baseSkin; 
        }

        public void SkinReset()
        {
            context.Skeleton.skeleton.SetSkin(baseSkin);
            context.Skeleton.Skeleton.SetSlotsToSetupPose();
        }

        #endregion
    }
}
