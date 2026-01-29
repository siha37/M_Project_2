using System;
using System.Collections.Generic;
using FishNet.Object;
using FMODUnity;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound
{
    public class ObjectSfx<T> : NetworkBehaviour where T : struct, Enum
    { 
        
        protected readonly Dictionary<T, float> lastTimes = new Dictionary<T, float>(8);
        
        
        protected bool CanPlayNow(T type)
        {
            float now = Time.time;
            float minInterval = GetMinInterval(type);

            if (lastTimes.TryGetValue(type, out float last))
            {
                if ((now - last) < minInterval)
                    return false;
            }

            lastTimes[type] = now;
            return true;
        }

        protected virtual float GetMinInterval(T type)
        {
            return 0;
        }

        protected virtual EventReference GetEvent(T type)
        {
            return default;
        }

    }
}