using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Conditions
{
    [TaskCategory("Enemy")]
    [TaskDescription("타겟에 대한 시야가 확보되었는지 확인")]
    public class HasLineOfSight : Conditional
    {
        public SharedBool LineOfSight;

        public override TaskStatus OnUpdate()
        {
            return LineOfSight.Value ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            LineOfSight = false;
        }
    }
}
