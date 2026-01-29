using FishNet.Object;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace MyFolder._1._Scripts._0._Object._6._DynamicObject
{
    public class DoorObject : NetworkBehaviour
    {
        
        [Header("물리")]
        [SerializeField] BoxCollider2D boxCollider;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] ShadowCaster2D shadowCaster2D;
        
        [Header("화롯불")]
        [SerializeField] Light2D doorLight_1;
        [SerializeField] Light2D doorLight_2;
        [SerializeField] ParticleSystem fireParticle_1;
        [SerializeField] ParticleSystem fireParticle_2;
        
        [Header("열림 컬러")]
        [SerializeField] Color off_doorLight_Color;
        [SerializeField] Color off_fireParticleMain_Color;
        [SerializeField] Color off_fireParticleSpark_Color;
        [SerializeField] Color off_fireParticleFire_Color;
        
        [Header("잠금 컬러")]
        
        [SerializeField] Color on_doorLight_Color;
        [SerializeField] Color on_fireParticleMain_Color;
        [SerializeField] Color on_fireParticleSpark_Color;
        [SerializeField] Color on_fireParticleFire_Color;
        
        [Header("문 자체")]
        [SerializeField] Light2D mainLight;
        [SerializeField] private GameObject doorProtal;
        
        [Header("콜라이더")]
        [SerializeField] CircleCollider2D circleCollider;

        private bool AlphaControll = true;
        private Transform target;
        private const float radius = 7f;
        
        // 문 잠금
        public void DoorClose()
        {
            boxCollider.enabled = true;
            spriteRenderer.color = Color.white;
            shadowCaster2D.enabled = true;
            
            //Light 처리
            doorLight_1.color = on_doorLight_Color;
            doorLight_2.color = on_doorLight_Color;
            
            //파티클 색상 변경
            var p_main_1 = fireParticle_1.main;
            p_main_1.startColor = on_fireParticleMain_Color;
           
            var p_main_2 = fireParticle_2.main;
            p_main_2.startColor = on_fireParticleMain_Color; 
            
            //라이트 활성화
            mainLight.enabled = true;
            if(doorProtal)
                doorProtal.SetActive(true);
            
            //스프라이트 알파 처리 비활성화
            AlphaControllOff();
            spriteRenderer.color = Color.white;

            if(IsServerInitialized)
                DoorCloseObserver();
        }

        [ObserversRpc]
        public void DoorCloseObserver()
        {
            if(!IsServerInitialized)
                DoorClose();   
        }

        //문 열림
        public void DoorOpen()
        {
            boxCollider.enabled = false;
            shadowCaster2D.enabled = false;
            
            //Light 처리
            doorLight_1.color = off_doorLight_Color;
            doorLight_2.color = off_doorLight_Color;
            
            //파티클 색상 변경
            var p_main_1 = fireParticle_1.main;
            p_main_1.startColor = off_fireParticleMain_Color;
           
            var p_main_2 = fireParticle_2.main;
            p_main_2.startColor = off_fireParticleMain_Color; 
            
            //라이트 비활성화
            mainLight.enabled = false;
            if(doorProtal)
                doorProtal.SetActive(false);
            
            //스프라이트 알파 처리 활성
            AlphaControllOn();
            
            
            if(IsServerInitialized)
                DoorOpenObserver();
        }

        [ObserversRpc]
        public void DoorOpenObserver()
        {
            if(!IsServerInitialized)
                DoorOpen();
        }


        #region MainTexture Alpha Controll

        private void AlphaControllOff()
        {
            circleCollider.enabled = false;
            AlphaControll = false;
        }

        private void AlphaControllOn()
        {
            circleCollider.enabled = true;
            AlphaControll = true;
        }

        private void Update()
        {
            AlphaControllUpdate();
        }

        private void AlphaControllUpdate()
        {
            if (AlphaControll && target)
            {
                float distance = Vector3.Distance(transform.position, target.position);
                float percentage = distance / radius;
                if (percentage < 1)
                {
                    spriteRenderer.color = Color.Lerp(new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), percentage);
                }  
            }
        }
        public void GetTarget(Transform transformPosition)
        {
            target = transformPosition;
        }

        public void OutTarget(Transform transformPosition)
        {
            target = null;
        }

        #endregion

    }
}
