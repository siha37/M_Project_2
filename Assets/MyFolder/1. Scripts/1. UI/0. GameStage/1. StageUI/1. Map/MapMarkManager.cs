using System.Collections.Generic;
using FishNet.Object;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{

    public enum MapMarkType
    {
        Area,
        Mark,
        Count
    }
    public enum MarkType{QUEST,PLAYER}

    public class MapMarkManager : NetworkBehaviour
    {
        public static MapMarkManager instance;
        public int markSeed = 0;
        
        private List<MapMarkContext> mapMark = new();
        public List<MapMarkContext> MapMark => mapMark;
        public delegate void Markevent(MapMarkContext context);

        public Markevent CreateMark;
        public Markevent DeleteMark;

        public void Awake()
        {
            instance = this;
        }

        #region Register

        //서버에서만 호출
        public int Register(MapMarkContext context)
        {
            mapMark.Add(context);
            context.MarkId = markSeed;
            RegisterClient(context);
            //Callback
            CreateMark?.Invoke(context);
            return markSeed++;
        }
        
        [ObserversRpc]
        private void RegisterClient(MapMarkContext context)
        {
            if (IsServerInitialized)
                return;
            mapMark.Add(context);
            context.MarkId = markSeed++;
            //Callback
            CreateMark?.Invoke(context);
        }

        public void Unregister(int Markid)
        {
            int index = GetMarkIndex(Markid);
            if (index == -1)
            {
                LogManager.LogError(LogCategory.UI, "Mark not found", this);
                return;
            }
            DeleteMark?.Invoke(mapMark[index]);
            
            //Callback
            mapMark.RemoveAt(index);
            UnregisterClient(index);
        }

        [ObserversRpc]
        private void UnregisterClient(int Markindex)
        {
            if(IsServerInitialized)
                return;
            DeleteMark?.Invoke(mapMark[Markindex]);
            mapMark.RemoveAt(Markindex);
        }
        
        #endregion

        #region Find

        public int GetMarkIndex(int Markid)
        {
            for (int i = mapMark.Count - 1; i >= 0; i--)
            {
                if(mapMark[i].MarkId == Markid)
                    return i;
            }
            return -1;   
        }
        
        public MapMarkContext GetMark(int Markid)
        {
            for (int i = mapMark.Count - 1; i >= 0; i--)
            {
                if(mapMark[i].MarkId == Markid)
                    return mapMark[i];
            }
            return null;
        }
        
        #endregion

    }
}