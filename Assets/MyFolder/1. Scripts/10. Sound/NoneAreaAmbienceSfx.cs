using UnityEngine;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._10._Sound
{
    public class NoneAreaAmbienceSfx : MonoBehaviour
    {
        [SerializeField] private float minTime = 15, maxTime = 40;
        private float currentTime;
        private float targetTime;
        [SerializeField] private GameSystemSound.SSFXType sfxType = GameSystemSound.SSFXType.ENV_Cricket;
        void Start()
        {
            targetTime = Random.Range(minTime, maxTime);
            currentTime = 0;
        }

        private void Update()
        {
            if (currentTime >= targetTime)
            {
                targetTime = Random.Range(minTime, maxTime);
                currentTime = 0;
                GameSystemSound.Instance.Player_Default_SFX(sfxType);
            }
            else
            {
                currentTime += Time.deltaTime;
            }
        }
    }
}
