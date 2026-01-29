using FMODUnity;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound
{
    public class Dis_ActiveSfx : MonoBehaviour
    {
        [SerializeField] private EventReference ActiveSfx;
        [SerializeField] private EventReference DisActiveSfx;

        private void OnEnable()
        {
            if (ActiveSfx.IsNull == false)
                RuntimeManager.PlayOneShot(ActiveSfx);
        }

        private void OnDisable()
        {
            if (DisActiveSfx.IsNull == false)
                RuntimeManager.PlayOneShot(DisActiveSfx);
        }
    }
}
