using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Conditions
{
    [TaskCategory("Enemy")]
    [TaskDescription("타겟이 존재하는지 확인")]
    public class HasTarget : Conditional
    {
        public SharedGameObject CurrentTarget;

        public override TaskStatus OnUpdate()
        {
            return CurrentTarget.Value != null ? TaskStatus.Success : TaskStatus.Failure;
        }

        public override void OnReset()
        {
            CurrentTarget = null;
        }
    }
}
