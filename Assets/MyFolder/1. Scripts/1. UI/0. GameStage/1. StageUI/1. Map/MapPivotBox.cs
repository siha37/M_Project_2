using System;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    public class MapPivotBox : MonoBehaviour
    {
        public float top,bottom,left,right;
        public Vector2 pivot;

        public float xDistance => left + right;
        public float yDistance => top + bottom;
        public float xTDistance(Vector2 point) => point.x - (pivot.x - left);
        public float yTDistance(Vector2 point) => point.y - (pivot.y - bottom);

        public Vector2 NormalPos(Vector2 point) => new (xTDistance(point) / xDistance, yTDistance(point) / yDistance);
        public Vector2 Distance(Vector2 point) => new (point.x/xDistance, point.y/yDistance);
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(new Vector3(pivot.x+((right - left)/2),pivot.y+((top - bottom)/2)),new Vector3(left+right,top+bottom,0));
        }
    }
}
