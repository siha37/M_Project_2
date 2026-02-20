using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Conditions
{
    [TaskCategory("Enemy")]
    [TaskDescription("타겟이 공격 사거리 안에 있는지 확인")]
    public class InAttackRange : Conditional
    {
        public SharedFloat DistanceToTarget;
        public SharedFloat AttackRange;

        public override TaskStatus OnUpdate()
        {
            return DistanceToTarget.Value <= AttackRange.Value
                ? TaskStatus.Success
                : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            DistanceToTarget = 0f;
            AttackRange = 5f;
        }
    }
}
