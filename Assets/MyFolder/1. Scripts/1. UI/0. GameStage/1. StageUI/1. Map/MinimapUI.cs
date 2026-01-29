using FishNet;
using MyFolder._1._Scripts._3._SingleTone;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    public class MinimapUI : MapUI
    {

        [SerializeField] private Image mapMask;


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
                if (MapMarkManager.instance.MapMark[i] != null && MapMarkManager.instance.MapMark[i].miniMark)
                {
                    MapMarkContext context = MapMarkManager.instance.MapMark[i];
                    Vector2 marknormalPos = pivotBox.NormalPos(context.Point());

                    //위치 설정
                    Vector2 markPos =
                        new Vector2(mapImage.rectTransform.sizeDelta.x * marknormalPos.x,
                            mapImage.rectTransform.sizeDelta.y * marknormalPos.y);

                    MapMark mark;
                    MapMarkManager.instance.MapMark[i].miniMark.TryGetComponent(out mark);

                    mark?.SetPosition(markPos);

                    if (context.Type == MapMarkType.Area)
                    {
                        Vector2 areaDistance = pivotBox.Distance(new Vector2(context.size.x, context.size.y));
                        areaDistance *= mapImage.rectTransform.sizeDelta;
                        mark?.SetDistance(areaDistance);
                    }
                }
            }

            mapImage.rectTransform.anchoredPosition =
                new Vector2(
                    Mathf.Clamp(
                        mapPlayerImage.rectTransform.anchoredPosition.x - (mapMask.rectTransform.sizeDelta.x / 2),
                        0, mapImage.rectTransform.sizeDelta.x - mapMask.rectTransform.sizeDelta.x),
                    Mathf.Clamp(
                        mapPlayerImage.rectTransform.anchoredPosition.y - (mapMask.rectTransform.sizeDelta.y / 2),
                        0, mapImage.rectTransform.sizeDelta.y - mapMask.rectTransform.sizeDelta.y)
                ) * -1;
        }

        protected override void MarkObjectSetting(MapMarkContext context, MapMark mark)
        {
            context.miniMark = mark.gameObject;
        }
        protected override void DeleteMark(MapMarkContext context)
        {
            Destroy(context.miniMark);
        }
    }
}
