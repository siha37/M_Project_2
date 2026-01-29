using FishNet.Example.ColliderRollbacks;
using Spine.Unity;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player.Data
{
    /// <summary>
    /// 플레이어 애니메이션 세트
    /// - 플레이어 타입/스킨별로 애니메이션 데이터를 관리
    /// - 위장 시스템에서 백업/복원 시 사용
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerAnimationSet", menuName = "MyFolder/Player/Animation Set", order = 0)]
    public class PlayerAnimationSet : ScriptableObject
    {
        [Header("플레이어 정보")]
        [Tooltip("플레이어 이름 (에디터용)")]
        public string playerName = "Default Player";
        
        [Header("스파인 데이터")]
        [Tooltip("플레이어 스켈레톤 데이터")]
        public SkeletonDataAsset skeletonData;
        
        [Header("Idle 애니메이션 (4방향)")]
        public AnimationReferenceAsset idle_down_;
        public AnimationReferenceAsset idle_down_r;
        public AnimationReferenceAsset idle_up_;
        public AnimationReferenceAsset idle_up_r;
        
        
        [Header("Walk 애니메이션 (4방향)")]
        public AnimationReferenceAsset walk_down_;
        public AnimationReferenceAsset walk_down_r;
        public AnimationReferenceAsset walk_up_;
        public AnimationReferenceAsset walk_up_r;
        
        [Header("전투 애니메이션")]
        public AnimationReferenceAsset aim;
        public AnimationReferenceAsset shoot_down;
        public AnimationReferenceAsset shoot_up;
        public AnimationReferenceAsset death;
        
        [Header("특수 애니메이션")]
        public AnimationReferenceAsset revival;
        public AnimationReferenceAsset revival_attempt;
        public AnimationReferenceAsset stun;
        public AnimationReferenceAsset ear;
        public AnimationReferenceAsset camouflage;
        
        
        [Header("발사 위치")]
        [Tooltip("플레이어의 발사 위치 오프셋")]
        public Vector3 shotPointOffset = new Vector3(0.5f, 0, 0);
        
        /// <summary>
        /// 방향 인덱스에 맞는 Idle 애니메이션 반환
        /// </summary>
        /// <param name="directionIndex">0:down_left, 1:down_right, 2:up_left, 3:up_right</param>
        public AnimationReferenceAsset GetIdleAnimation(int directionIndex,bool isReverse)
        {
            if (!isReverse)
            {
                switch (directionIndex)
                {
                    case 0:
                        return idle_down_;
                    case 1:
                        return idle_up_;
                    default:
                        break;
                }
            }
            else
            {
                switch (directionIndex)
                {
                    case 0:
                        return idle_down_r;
                    case 1:
                        return idle_up_r;
                    default:
                        break;
                }
            }
            return idle_up_r;
        }
        
        /// <summary>
        /// 방향 인덱스에 맞는 Walk 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetWalkAnimation(int directionIndex,bool isReverse)
        {
            if (!isReverse)
            {
                switch (directionIndex)
                {
                    case 0:
                        return walk_down_;
                    case 1:
                        return walk_up_;
                    default:
                        break;
                }
            }
            else
            {
                switch (directionIndex)
                {
                    case 0:
                        return walk_down_r;
                    case 1:
                        return walk_up_r;
                }
            }
            return idle_up_r;
        }

        public AnimationReferenceAsset GetAimAnimation()
        {
            return aim;
        }
        
        /// <summary>
        /// Shoot 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetShootAnimation(int directionIndex)
        {
            switch (directionIndex)
            {
                case 0:
                    return shoot_down;
                case 1:
                    return shoot_up;
            }
            return shoot_down;
        }
        
        /// <summary>
        /// Death 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetDeathAnimation()
        {
            return death;
        }
        
        /// <summary>
        /// Revival 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetRevivalAnimation()
        {
            return revival;
        }
        
        /// <summary>
        /// Revival Attempt 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetRevivalAttemptAnimation()
        {
            return revival_attempt;
        }
        
        public AnimationReferenceAsset GetCamouflage()
        {
            return camouflage;
        }
        
        /// <summary>
        /// Ear 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetEarAnimation()
        {
            return ear;
        }
    }
}

