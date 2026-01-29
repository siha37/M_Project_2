using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public class EnemyBaseState : IEnemyState
    {
        protected EnemyControll agent;
        public override void Init(EnemyControll controll)
        {
            agent = controll;
        }

        public override bool CanStateChange(IEnemyState newState)
        {
            return true;
        }

        public override void Update()
        {
        }

        public override void OnStateEnter()
        {
        }

        public override void OnStateExit()
        {
        }

        public override string GetName()
        {
            return GetType().Name;
        }
    }
}