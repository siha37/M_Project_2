using System;
using System.Numerics;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using UnityEngine;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._10._Sound
{
    public class AmbienceSfx : MonoBehaviour
    {
        [SerializeField] GameSystemSound.SSFXType sfxType;
        
        // 사운드 호출 지연 변수
        [SerializeField] private float intervalTime = 10f;
        [SerializeField] private float currentTime = 0;
        
        [SerializeField] private float updateintervalTimeMin = 2, updateintervalTimeMax = 5;
        [SerializeField] private float updateintervalTime = 0;
        [SerializeField] private float percent = 0.3f;

        [SerializeField] private float radian;
        [SerializeField] private float insideRadian;
        
        private bool isAble = true;
        private bool PlayerEntered = false;
        private Transform player;

        private void Start()
        {
            CircleCollider2D collider = GetComponent<CircleCollider2D>();
            collider.radius = radian;
        }
        private void Update()
        {
            if (isAble && PlayerEntered)
            {
                updateintervalTime -= Time.deltaTime;
                if (updateintervalTime <= 0)
                {
                    float nowP = Random.Range(0, 1);
                    if (percent >= nowP)
                    {
                        PlaySound();
                        Off_Sound();
                    }
                    else
                    {
                        updateintervalTime = Random.Range(updateintervalTimeMin, updateintervalTimeMax);
                    }
                }
            }
            else
            {
                currentTime -= Time.deltaTime;
                if(currentTime <= 0)
                    On_Sound();               
            }
            
        }
        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.TryGetComponent(out PlayerNetworkSync Sync) && Sync.IsOwner)
            {
                updateintervalTime = Random.Range(updateintervalTimeMin, updateintervalTimeMax);
                PlayerEntered = true;
                player = col.gameObject.transform;
            }
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            if (PlayerEntered)
            {
                if (col.gameObject.TryGetComponent(out PlayerNetworkSync Sync) && Sync.IsOwner)
                {
                    PlayerEntered = false;
                    player = null;
                }
            }
        }

        private void PlaySound()
        {
            switch (sfxType)
            {
                case GameSystemSound.SSFXType.ENV_OWL:
                    if(player)
                    {
                        if ((transform.position - player.position).magnitude > insideRadian)
                        {
                            GameSystemSound.Instance.Player_OwlFarSFX(gameObject); 
                        }
                        else
                        {
                            GameSystemSound.Instance.Player_OwlSFX(gameObject);    
                        }
                    }
                    else 
                    {
                        GameSystemSound.Instance.Player_OwlSFX(gameObject);   
                    }
                    break;
                default:
                    GameSystemSound.Instance.Player_Default_SFX3D(sfxType, gameObject);
                    break;
            }
        }

        private void Off_Sound()
        {
            currentTime = intervalTime;
            isAble = false;
        }

        private void On_Sound()
        {
            updateintervalTime = Random.Range(updateintervalTimeMin, updateintervalTimeMax);
            isAble = true;
        }

        private void OnDrawGizmos()
        {
            switch (sfxType)
            {
                case GameSystemSound.SSFXType.ENV_OWL:
                    Gizmos.color = Color.red;
                    break;
                case GameSystemSound.SSFXType.ENV_LEAF:
                    Gizmos.color = Color.green;
                    break;
                case GameSystemSound.SSFXType.ENV_Gravel:
                    Gizmos.color = Color.cyan;
                    break;
                case GameSystemSound.SSFXType.ENV_Pigeon:
                    Gizmos.color = Color.magenta;
                    break;
                case GameSystemSound.SSFXType.ENV_Twig:
                    Gizmos.color = Color.yellow;
                    break;
            }
            Gizmos.DrawWireSphere(transform.position, radian);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, insideRadian);
        }
    }
}