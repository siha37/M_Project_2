using FishNet.Connection;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._0._Agent._0._Player._1._SubObject._0._Shield
{
    public class Shield : MonoBehaviour
    {
        public PlayerContext context;
        [SerializeField] private ParticleSystem shield;

        public void Start()
        {
            context.Sync.OnPlayerDefence += OnEffect;
        }
        
        public bool shieldActive()
        {
            if (!context || !context.Status)
                return false;
            return !context.Status.IsCrackDefence;
        }

        public void OnDefence(float damage, Vector2 hitDirection, NetworkConnection attacker = null)
        {
            context.Sync.RequestTakeDefence(damage, hitDirection, attacker);
            //OnEffect();
        }
        
        
        private void OnEffect()
        {
            var ep = new ParticleSystem.EmitParams { };
            if(shield)
                shield.Emit(ep, 1);  // 나머지 값은 전부 인스펙터 그대로 사용
        }
    }
}