using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.BehaviorTree.Actions
{
    [TaskCategory("Enemy")]
    [TaskDescription("타겟을 향해 공격 (이동 + 사격)")]
    public class EnemyAttack : Action
    {
        public SharedGameObject CurrentTarget;

        private EnemyControll agent;
        private EnemyCombat combat;
        private EnemyMovement movement;

        public override void OnAwake()
        {
            agent = Owner.GetComponent<EnemyControll>();
        }

        public override void OnStart()
        {
            combat = agent.GetEnemyAllComponent(typeof(EnemyCombat)) as EnemyCombat;
            movement = agent.GetEnemyAllComponent(typeof(EnemyMovement)) as EnemyMovement;

            if (combat != null)
                combat.AttackOn();

            if (movement != null)
            {
                movement.SetSpeed(agent.Status.EnemyData.speed);
                movement.OnMove = true;
            }
        }

        public override TaskStatus OnUpdate()
        {
            if (!CurrentTarget.Value)
                return TaskStatus.Failure;

            float distance = Vector2.Distance(agent.transform.position, CurrentTarget.Value.transform.position);
            if (distance > agent.Status.EnemyData.attackRange)
                return TaskStatus.Failure;

            if (movement != null)
                movement.MoveTo(CurrentTarget.Value.transform.position);

            return TaskStatus.Running;
        }

        public override void OnEnd()
        {
            if (combat != null)
                combat.AttackOff();

            if (movement != null)
                movement.OnMove = false;
        }

        public override void OnReset()
        {
            CurrentTarget = null;
        }
    }
}
