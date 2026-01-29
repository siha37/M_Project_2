using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyMoveState : EnemyBaseState
    {
        private EnemyMovement movement;
        public override void Init(EnemyControll controll)
        {
            base.Init(controll);
            movement = (EnemyMovement)agent.GetEnemyActiveComponent(typeof(EnemyMovement));
        }

        public override void Update()
        {
        }

        public override void OnStateEnter()
        {
            movement.SetSpeed(agent.Status.EnemyData.speed);
            movement.OnMove = true;
        }

        public override void OnStateExit()
        {
            movement.OnMove = false;
        }
    }
}