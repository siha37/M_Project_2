using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Conditions
{
    [TaskCategory("Enemy")]
    [TaskDescription("엄폐가 필요한 상황인지 확인 (체력 비율 낮음 + 쿨다운)")]
    public class NeedCover : Conditional
    {
        [UnityEngine.Tooltip("현재 체력 비율 (0~1)")]
        public SharedFloat HealthRatio;

        [UnityEngine.Tooltip("이 비율 이하이면 엄폐 필요")]
        public SharedFloat CoverThreshold = 0.5f;

        [UnityEngine.Tooltip("엄폐 후 재진입 쿨다운 종료 시각 (Blackboard)")]
        public SharedFloat CoverCooldownEndTime;

        public override TaskStatus OnUpdate()
        {
            if (CoverCooldownEndTime != null && Time.time < CoverCooldownEndTime.Value)
                return TaskStatus.Failure;

            return HealthRatio.Value <= CoverThreshold.Value
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            HealthRatio = 1f;
            CoverThreshold = 0.5f;
            CoverCooldownEndTime = 0f;
        }
    }
}
