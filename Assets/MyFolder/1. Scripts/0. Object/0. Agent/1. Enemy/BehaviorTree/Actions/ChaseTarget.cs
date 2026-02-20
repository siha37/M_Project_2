using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Actions
{
    [TaskCategory("Enemy")]
    [TaskDescription("타겟을 향해 추적 이동")]
    public class ChaseTarget : Action
    {
        public SharedGameObject CurrentTarget;

        private EnemyControll agent;
        private EnemyMovement movement;

        public override void OnAwake()
        {
            agent = Owner.GetComponent<EnemyControll>();
        }

        public override void OnStart()
        {
            movement = agent.GetEnemyAllComponent(typeof(EnemyMovement)) as EnemyMovement;

            if (movement == null || CurrentTarget.Value == null) return;

            movement.SetSpeed(agent.Status.EnemyData.speed);
            movement.OnMove = true;
            movement.MoveTo(CurrentTarget.Value.transform.position);
        }

        public override TaskStatus OnUpdate()
        {
            if (CurrentTarget.Value == null || movement == null)
                return TaskStatus.Failure;

            movement.MoveTo(CurrentTarget.Value.transform.position);
            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            if (movement != null)
                movement.OnMove = false;
        }

        public override void OnReset()
        {
            CurrentTarget = null;
        }
    }
}
