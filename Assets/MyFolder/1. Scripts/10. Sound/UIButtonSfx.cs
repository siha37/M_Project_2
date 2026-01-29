using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._10._Sound
{
    [RequireComponent(typeof(Button))]
    public sealed class UIButtonSfx : MonoBehaviour, IPointerEnterHandler,IPointerClickHandler
    {
        [SerializeField] private EventReference clickSfx;
        [SerializeField] private EventReference hoverSfx;
        [SerializeField] private EventReference failSfx;

        private Button cachedButton;

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
        }

        private void OnEnable()
        {
            cachedButton.onClick.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            cachedButton.onClick.RemoveListener(PlayClick);
        }

        private void PlayClick()
        {
            if (clickSfx.IsNull == false)
                RuntimeManager.PlayOneShot(clickSfx);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (hoverSfx.IsNull == false)
                RuntimeManager.PlayOneShot(hoverSfx);
        }


        public void OnPointerClick(PointerEventData eventData)
        {
            if (cachedButton.interactable) return;
            if (failSfx.IsNull == false)
                RuntimeManager.PlayOneShot(failSfx);

        }
    }
}