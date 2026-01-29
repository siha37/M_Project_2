using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace MyFolder._1._Scripts._8._Time
{
    public class TimeManager : NetworkBehaviour
    {
        public static TimeManager instance;
        public delegate void voidDelegate();
        public voidDelegate OnTimeChange;

        private void Awake()
        {
            if(!instance)
                instance = this;
        }


        //시간은 S(초) 단위
        private readonly SyncVar<float> syncTime = new ();
        private float currentTime;
        private float endTime = 1200;
        
        public float CurrentTime => currentTime;
        public float EndTime => endTime;
        public bool IsEnd => currentTime >= endTime;
        public float EndTimeSet { get => endTime; set => endTime = value; }
        public override void OnStartServer()
        {
            currentTime = 0;
        }

        public override void OnStartClient()
        {
            if(IsHostInitialized)
                return;
            syncTime.OnChange += SyncTime_OnChange;
        }
        public void Update()
        {
            if(!IsHostInitialized)
                return;
            currentTime += Time.deltaTime;
            syncTime.Value = currentTime;
            OnTimeChange?.Invoke();
        }

        private void SyncTime_OnChange(float oldValue, float newValue,bool isServer)
        {
            currentTime = newValue;
            OnTimeChange?.Invoke();
        }
        
        
    }
}