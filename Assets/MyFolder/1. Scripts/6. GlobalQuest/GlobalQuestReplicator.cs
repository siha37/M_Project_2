using System;
using System.Collections;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest
{
    public sealed class GlobalQuestReplicator : NetworkBehaviour
    {
        // 여러 퀘스트 동시 관리용 식별자
        public readonly SyncVar<int> QuestId = new SyncVar<int>();
        public readonly SyncVar<bool> ResetComplete = new SyncVar<bool>(false);
        
        private GlobalQuestUIController globalQuestUIController;

        // 메타/진행도
        public readonly SyncVar<GlobalQuestType> QuestType = new SyncVar<GlobalQuestType>();
        public readonly SyncVar<string> QuestName = new SyncVar<string>();
        public readonly SyncVar<float> WaitingTime = new SyncVar<float>();
        public readonly SyncVar<float> Progress = new SyncVar<float>();
        public readonly SyncVar<float> Target = new SyncVar<float>();
        public readonly SyncVar<float> LimitTime = new SyncVar<float>();
        public readonly SyncVar<float> ElapsedTime = new SyncVar<float>();
        public readonly SyncVar<Vector2> Size = new SyncVar<Vector2>();
        public readonly SyncVar<Vector3> Position = new SyncVar<Vector3>();
        public readonly SyncVar<bool> IsActive = new SyncVar<bool>();
        public readonly SyncVar<bool> IsEnd = new SyncVar<bool>();
        public readonly SyncVar<float> MinusProgress = new SyncVar<float>();
        public readonly SyncVar<float> MinusTiming = new SyncVar<float>();
        public readonly SyncVar<float> MinusMutiple = new SyncVar<float>();

        public override void OnStartClient()
        {
            globalQuestUIController = FindFirstObjectByType<GlobalQuestUIController>();
            StartCoroutine(nameof(InitWait));
        }

        public override void OnStopClient()
        {
            globalQuestUIController?.OnReplicatorDespawned(this);
        }

        private IEnumerator InitWait()
        {
            while (!ResetComplete.Value)
                yield return new WaitForEndOfFrame();
            if (globalQuestUIController)
            {
                globalQuestUIController.OnReplicatorSpawned(this);
            }
        }
    }
}


