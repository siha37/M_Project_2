using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._8999._Utility.Corutin{
    public static class WaitForSecondsCache{
        
        private static readonly Dictionary<float, WaitForSeconds> waitForSecondsCache = new Dictionary<float, WaitForSeconds>();
        private static readonly Dictionary<float, WaitForSecondsRealtime> waitForSecondsRealtimeCache = new Dictionary<float, WaitForSecondsRealtime>();

        public static void Initialize(){
            Application.quitting += Clear;
        }

        public static WaitForSeconds Get(float seconds){
            if(!waitForSecondsCache.TryGetValue(seconds, out WaitForSeconds waitForSeconds)){
                waitForSeconds = new WaitForSeconds(seconds);
                waitForSecondsCache[seconds] = waitForSeconds;
            }
            return waitForSeconds;
        }

        public static WaitForSecondsRealtime GetRealtime(float seconds){
            if(!waitForSecondsRealtimeCache.TryGetValue(seconds, out WaitForSecondsRealtime waitForSecondsRealtime)){
                waitForSecondsRealtime = new WaitForSecondsRealtime(seconds);
                waitForSecondsRealtimeCache[seconds] = waitForSecondsRealtime;
            }
            return waitForSecondsRealtime;
        }

        public static void Remove(float seconds){
            waitForSecondsCache.Remove(seconds);
            waitForSecondsRealtimeCache.Remove(seconds);
        }

        public static void Clear(){
            waitForSecondsCache.Clear();
            waitForSecondsRealtimeCache.Clear();
        }
        
    }
}