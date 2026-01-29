using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.States;

namespace MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main.Components.Interface
{
    public interface IEnemyComponent
    {
        public void Init(EnemyControll agent);
        public void OnEnable();
        public void OnDisable();
        public void OnDestroy();
        public void ChangedState(IEnemyState oldstate, IEnemyState newstate);
        public void Reset();
    }
}