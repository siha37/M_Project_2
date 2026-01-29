using System;
using FishNet;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    public class MapUI : MonoBehaviour
    {
        [SerializeField] protected Image mapPlayerImage;
        [SerializeField] protected Image mapImage;
        protected MapPivotBox pivotBox;

        [FormerlySerializedAs("MarkPrefab")] [SerializeField] protected MapMark Quest_MarkPrefab;
        [SerializeField] protected MapMark Player_MarkPrefab;
        
        protected Transform playerTransform;
        
        
        void Start()
        {
            StartCoroutine(nameof(InitializeWithStateCheck));
            pivotBox = FindFirstObjectByType<MapPivotBox>();
        }
        

        protected System.Collections.IEnumerator InitializeWithStateCheck()
        {
            // NetworkManager 초기화 대기
            while (!InstanceFinder.NetworkManager || !InstanceFinder.ClientManager)
            {
                yield return WaitForSecondsCache.Get(0.1f);
            }
            playerTransform = NetworkPlayerManager.Instance.GetLocalOwnedPlayer()?.transform;
            while (!playerTransform)
            {
                yield return WaitForSecondsCache.Get(0.1f);
                playerTransform = NetworkPlayerManager.Instance.GetLocalOwnedPlayer()?.transform;
            }
            LogManager.Log(LogCategory.UI, "Server- MarkEvent Connecting", this);
            MapMarkManager.instance.CreateMark+=CreateMark;
            MapMarkManager.instance.DeleteMark+=DeleteMark;
            
        }

        protected virtual void Update()
        {
            
        }

        private void OnDisable()
        {
            MapMarkManager.instance.CreateMark-=CreateMark;
            MapMarkManager.instance.DeleteMark-=DeleteMark;
        }

        private void CreateMark(MapMarkContext context)
        {
            MapMark prefab = null;
            if (context.TargetType == MarkType.PLAYER)
            {
                prefab = Player_MarkPrefab;
            }
            else if (context.TargetType == MarkType.QUEST)
            {
                prefab = Quest_MarkPrefab;
            }
            MapMark mark = Instantiate(prefab, mapImage.rectTransform);
            MarkObjectSetting(context, mark);
            mark.gameObject.SetActive(true);
            
            mark.SetMarkType(context.Type);
            mark.SetMarkColor(context.color);
            mark.SetAreaColor(context.color);
            
            LogManager.Log(LogCategory.UI, "Mark Created", mark);
        }

        protected virtual void MarkObjectSetting(MapMarkContext context, MapMark mark)
        {
            
        }

        protected virtual void DeleteMark(MapMarkContext context)
        {
        }
    }
}