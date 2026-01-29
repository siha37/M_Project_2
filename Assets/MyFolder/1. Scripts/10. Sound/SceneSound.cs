using System;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using UnityEngine.Rendering;
using STOP_MODE = FMOD.Studio.STOP_MODE;

namespace MyFolder._1._Scripts._10._Sound
{
    public class SceneSound : MonoBehaviour
    {
        [Header("FMOD Events")]
        [SerializeField] private EventReference EnterSound;
        //[SerializeField] private EventReference ExitSound;
        
        EventInstance EnterSoundInstance;

        private void Start()
        {
            EnterSoundInstance = RuntimeManager.CreateInstance(EnterSound);
            EnterSoundInstance.start();
        }

        private void OnDestroy()
        {
            EnterSoundInstance.stop(STOP_MODE.ALLOWFADEOUT);
        }
    }
}
