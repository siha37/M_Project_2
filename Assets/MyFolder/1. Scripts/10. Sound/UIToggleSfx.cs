using FMODUnity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._10._Sound
{
    [RequireComponent(typeof(Toggle))]
    public class UIToggleSfx : MonoBehaviour, IPointerEnterHandler,IPointerClickHandler
    {
        [SerializeField] private EventReference clickSfx;
        [SerializeField] private EventReference hoverSfx;
        [SerializeField] private EventReference failSfx;

        private Toggle cachedToggle;
        private void Awake()
        {
            cachedToggle = GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            cachedToggle.onValueChanged.AddListener(PlayClick);
        }

        private void OnDisable()
        {
            cachedToggle.onValueChanged.RemoveListener(PlayClick);
        }

        private void PlayClick(bool value)
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
            if (cachedToggle.interactable) return;
            if (failSfx.IsNull == false)
                RuntimeManager.PlayOneShot(failSfx);
        }
    }
}