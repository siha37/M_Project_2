using Spine.Unity;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Data
{
    /// <summary>
    /// 적군 애니메이션 세트
    /// - 적군 타입별로 애니메이션 데이터를 관리
    /// - 위장 시스템에서 사용
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyAnimationSet", menuName = "MyFolder/Enemy/Animation Set", order = 0)]
    public class EnemyAnimationSet : ScriptableObject
    {
        [Header("적군 정보")]
        [Tooltip("적군 타입 ID (EnemyData의 TypeId와 매칭)")]
        public ushort enemyTypeId;
        
        [Tooltip("적군 이름 (에디터용)")]
        public string enemyName;
        
        [Header("스파인 데이터")]
        [Tooltip("적군 스켈레톤 데이터")]
        public SkeletonDataAsset skeletonData;
        
        [Header("Idle 애니메이션 (4방향)")]
        public AnimationReferenceAsset idle_down_;
        public AnimationReferenceAsset idle_up_;
        
        [Header("Walk 애니메이션 (4방향)")]
        public AnimationReferenceAsset walk_down_;
        public AnimationReferenceAsset walk_up_;
        
        [Header("Shot 애니메이션 (4방향")]
        public AnimationReferenceAsset shot_down_;
        public AnimationReferenceAsset shot_up_;
        
        [Header("전투 애니메이션")]
        public AnimationReferenceAsset death;
        
        [Header("발사 위치")]
        [Tooltip("적군의 발사 위치 오프셋")]
        public Vector3 shotPointOffset = new Vector3(0.5f, 0, 0);
        
        /// <summary>
        /// 방향 인덱스에 맞는 Idle 애니메이션 반환
        /// </summary>
        /// <param name="directionIndex">0:down_left, 1:down_right, 2:up_left, 3:up_right</param>
        public AnimationReferenceAsset GetIdleAnimation(int directionIndex)
        {
            switch (directionIndex)
            {
                case 0: 
                case 1: 
                    return idle_down_;
                case 2: 
                case 3: 
                    return idle_up_;
                default: return idle_down_;
            }
        }
        
        /// <summary>
        /// 방향 인덱스에 맞는 Walk 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetWalkAnimation(int directionIndex)
        {
            switch (directionIndex)
            {
                case 0: 
                case 1: 
                    return walk_down_;
                case 2: 
                case 3: 
                    return walk_up_;
                default: return walk_down_;
            }
        }
        
        /// <summary>
        /// Death 애니메이션 반환
        /// </summary>
        public AnimationReferenceAsset GetDeathAnimation()
        {
            return death;
        }
        
        /// <summary>
        /// Shoot 애니메이션 반환 (적군은 없음)
        /// </summary>
        public AnimationReferenceAsset GetShootAnimation(int directionIndex)
        {
            switch (directionIndex)
            {
                case 0: 
                case 1: 
                    return shot_down_;
                case 2: 
                case 3: 
                    return shot_up_;
                default: return shot_down_;
            }
        }
    }
}

