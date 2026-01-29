using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States
{
    public abstract class IEnemyState
    {
        public abstract void Init(EnemyControll controll);
        public abstract bool CanStateChange(IEnemyState newState);
        public abstract void Update();
        public abstract void OnStateEnter();
        public abstract void OnStateExit();
        public abstract string GetName();
    }
}