using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyAttackState: EnemyBaseState
    {
        private EnemyCombat combat;
        private EnemyMovement movement;
        public override void Init(EnemyControll controll)
        {
            base.Init(controll);
            combat = (EnemyCombat)agent.GetEnemyActiveComponent(typeof(EnemyCombat));
            movement = (EnemyMovement)agent.GetEnemyActiveComponent(typeof(EnemyMovement));
        }

        public override void Update()
        {
        }

        public override void OnStateEnter()
        {
            movement.SetSpeed(agent.Status.EnemyData.attackSpeed);
            movement.OnMove = true;
            combat.AttackOn();
        }

        public override void OnStateExit()
        {
            movement.OnMove = false;
            combat.AttackOff();
        }

    }
}