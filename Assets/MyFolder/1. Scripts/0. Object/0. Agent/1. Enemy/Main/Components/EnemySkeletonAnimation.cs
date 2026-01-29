using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;
using Spine;
using Spine.Unity;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components
{
    public class EnemySkeletonAnimation : IEnemyUpdateComponent
    {
        #region Variables
        
        // Components
        private EnemyControll agent;
        private EnemyAnimationSet enemyAnimationSet;
        private SkeletonAnimation skeletonAnimation;
        private EnemyMovement movement;

        private string currenty_Status_Name;

        
        // Control
        private bool onMove = false;
        private int lastdirection = 0;
        
        public bool OnMove { set { onMove = value; } }
        public int Direction { set { lastdirection = value; } }
        
        // Timing
        private float lastPathUpdateTime;
        private float pathUpdateInterval;
        
        #endregion
        public void FixedUpdate()
        {
        }

        public void LateUpdate()
        {
        }

        public void Init(EnemyControll agent)
        {
            this.agent = agent;
            enemyAnimationSet = agent.EnemyAnimationSet;
            skeletonAnimation = agent.SkeletonAnimation;
            movement = agent.GetEnemyActiveComponent(typeof(EnemyMovement)) as EnemyMovement;
            
        }

        public void OnEnable()
        {
            if (agent)
                Init(agent);
        }

        public void OnDisable()
        {
        }

        public void OnDestroy()
        {
        }

        public void ChangedState(IEnemyState oldstate, IEnemyState newstate)
        {
        }

        public void Reset()
        {
        }

        void IEnemyUpdateComponent.Update()
        {
            if (agent.IsServerInitialized)
            {
                if (movement == null)
                    movement = agent.GetEnemyActiveComponent(typeof(EnemyMovement)) as EnemyMovement;
                if (movement == null)
                    return;
                //이동속도가 없을 경우
                if (movement.moveDirection == Vector3.zero)
                {
                    if (onMove)
                    {
                        OnMove = false;
                        agent.NetworkSync.SetIsMoving(false);
                    }
                    int newdirection = GetDirection(agent.TargetDirection);
                    SetFlipX(newdirection);
                    
                    if (newdirection != lastdirection)
                        agent.NetworkSync.SetAnimationDirection(newdirection);
                    lastdirection = newdirection;
                    
                    AnimationReferenceAsset idle = enemyAnimationSet.GetIdleAnimation(lastdirection);
                    SetAnimation(0, idle, true);
                }
                //이동 중일 때
                else
                {
                    int newdirection = GetDirection(agent.TargetDirection);
                    SetFlipX(newdirection);

                    if (!onMove)
                        agent.NetworkSync.SetIsMoving(true);

                    if (newdirection != lastdirection)
                        agent.NetworkSync.SetAnimationDirection(newdirection);
                    lastdirection = newdirection;
                    
                    AnimationReferenceAsset walk = enemyAnimationSet.GetWalkAnimation(lastdirection);
                    SetAnimation(0, walk, true);
                }
            }
            else
            {
                //이동 안함
                if (!agent.NetworkSync.GetIsMoving())
                {
                    int newdirection = agent.NetworkSync.GetAnimationDirection();
                    SetFlipX(newdirection);
                    lastdirection = newdirection;
                    AnimationReferenceAsset idle = enemyAnimationSet.GetIdleAnimation(lastdirection);
                    SetAnimation(0, idle, true);
                }
                else
                {
                    int newdirection = agent.NetworkSync.GetAnimationDirection();
                    SetFlipX(newdirection);
                    lastdirection = newdirection;
                    AnimationReferenceAsset walk = enemyAnimationSet.GetWalkAnimation(lastdirection);
                    SetAnimation(0, walk, true);   
                }
            }
        }

        
        public void ShotStart()
        {
            var Shot = enemyAnimationSet.GetShootAnimation(lastdirection);
            TrackEntry trackEntry = SetAnimation(1, Shot, false);

            // 공격 애니메이션이 끝나면 트랙1을 비워서 마지막 프레임에 머물지 않도록 함
            if (trackEntry != null)
            {
                trackEntry.Complete += (entry) =>
                {
                    skeletonAnimation.AnimationState.SetEmptyAnimation(1, 0f);
                };
            }
        }
        
        private int GetDirection(Vector3 direction)
        {
            // 0 좌하 1 우하 2 좌상 3 우상
            int directionIndex = 0;
            // 이동 방향 우측
            if (direction.x > 0)
            {
                // 하
                if (direction.y < 0)
                {
                    directionIndex = 1;
                }
                // 상
                else
                {
                    directionIndex = 3;
                }
            }
            // 이동 방향 좌측
            else
            {
                
                // 하
                if (direction.y < 0)
                {
                    directionIndex = 0;
                }
                // 상
                else
                {
                    directionIndex = 2;
                }
            }
            return directionIndex;
        }

        private void SetFlipX(int index)
        {
            if (index is 1 or 3)
            {
                skeletonAnimation.skeleton.ScaleX = -1;
            }
            else
            {
                skeletonAnimation.skeleton.ScaleX = 1;
            }
        }
        

        private TrackEntry SetAnimation(int Track, AnimationReferenceAsset anim, bool loop, float timeScale = 1)
        {
            // NULL 체크
            if (!anim)
            {
                return null;
            }
            
            if (anim.Animation == null)
            {
                return null;
            }
            
            // Spine Animation 이름으로 비교
            string animName = anim.Animation.Name;
            
            if (currenty_Status_Name == animName)
            {
                return null;
            }
            
            // Spine API 호출
            TrackEntry trackEntry = skeletonAnimation.AnimationState.SetAnimation(Track, anim, loop);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = timeScale;
                currenty_Status_Name = anim.Animation.Name;  // 실제 재생된 애니메이션 이름 저장
            }
            
            return trackEntry;
        }
    }
}
