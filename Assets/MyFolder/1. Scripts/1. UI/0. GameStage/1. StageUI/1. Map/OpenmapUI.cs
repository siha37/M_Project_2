using UnityEngine;
using UnityEngine.InputSystem;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    public class OpenmapUI : MapUI
    {
        protected override void Update()
        {
            if(!playerTransform)
                return;
            
            //노말 값 받아오기
            Vector2 normalPos = pivotBox.NormalPos(playerTransform.position);
            
            //위치 설정
            mapPlayerImage.rectTransform.anchoredPosition =
                new Vector2(mapImage.rectTransform.sizeDelta.x * normalPos.x,
                    mapImage.rectTransform.sizeDelta.y * normalPos.y);
            
            //미크 위치 선정
            for (int i = 0; i < MapMarkManager.instance.MapMark.Count; i++)
            {
                //위치 연산
                if (MapMarkManager.instance.MapMark[i] != null && MapMarkManager.instance.MapMark[i].openMark)
                {
                    MapMarkContext context = MapMarkManager.instance.MapMark[i];
                    Vector2 marknormalPos = pivotBox.NormalPos(context.Point());
                    
                    //위치 설정
                    Vector2 markPos =
                        new Vector2(mapImage.rectTransform.sizeDelta.x * marknormalPos.x,
                            mapImage.rectTransform.sizeDelta.y * marknormalPos.y);

                    MapMark mark;
                    MapMarkManager.instance.MapMark[i].openMark.TryGetComponent(out mark);
                    
                    mark?.SetPosition(markPos);

                    if (context.Type == MapMarkType.Area)
                    {
                        Vector2 areaDistance = pivotBox.Distance(new Vector2(context.size.x, context.size.y));
                        areaDistance *= mapImage.rectTransform.sizeDelta;
                        mark?.SetDistance(areaDistance);
                    }
                }
            }
        }
        
        protected override void MarkObjectSetting(MapMarkContext context, MapMark mark)
        {
            context.openMark = mark.gameObject;
        }
        protected override void DeleteMark(MapMarkContext context)
        {
            Destroy(context.openMark);
        }
    }
}
