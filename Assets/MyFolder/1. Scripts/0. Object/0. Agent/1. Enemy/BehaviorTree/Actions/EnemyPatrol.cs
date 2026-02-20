using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Actions
{
    [TaskCategory("Enemy")]
    [TaskDescription("타겟이 없을 때 대기 (순찰 대기 상태)")]
    public class EnemyPatrol : Action
    {
        private EnemyControll agent;
        private EnemyMovement movement;

        public override void OnAwake()
        {
            agent = Owner.GetComponent<EnemyControll>();
        }

        public override void OnStart()
        {
            movement = agent.GetEnemyAllComponent(typeof(EnemyMovement)) as EnemyMovement;

            if (movement != null)
                movement.OnMove = false;
        }

        public override TaskStatus OnUpdate()
        {
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
        }
    }
}
