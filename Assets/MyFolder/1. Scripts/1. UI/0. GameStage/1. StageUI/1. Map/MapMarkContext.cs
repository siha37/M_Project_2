using System;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    [Serializable]
    public class MapMarkContext
    {
        public MapMarkContext()
        {
        }
        public MapMarkContext(MapMarkType markType,MarkType targetType,Vector2 point,Color c,Vector2 size)
        {
            Type = markType;
            staticPoint = point;
            TargetType = targetType;
            color = c;
            this.size = size;
        }

        public Vector2 Point()
        {
            return staticPoint;
        }

        public int MarkId;
        public MapMarkType Type;
        public MarkType TargetType;
        public Color color;
        public Vector2 staticPoint;
        public Vector2 size;
        public GameObject openMark;
        public GameObject miniMark;
    }
}