using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    public class MapMark : MonoBehaviour
    {
        [SerializeField] private Image markImage;
        [SerializeField] private RectTransform markRect;
        [SerializeField] private Image areaImage;

        public void SetMarkColor(Color color)
        {
            markImage.color = color;
        }

        public void SetAreaColor(Color color)
        {
            areaImage.color = color;
        }

        public void SetDistance(Vector2 distance)
        {
            areaImage.rectTransform.sizeDelta = distance;
        }

        public void SetPosition(Vector2 position)
        {
            markRect.anchoredPosition = position;
        }

        public void SetMarkType(MapMarkType markType)
        {
            switch (markType)
            {
                case MapMarkType.Area:
                    areaImage.gameObject.SetActive(true);
                    break;
            }
        }
    }
}