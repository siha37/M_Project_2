using System;
using FMOD.Studio;
using FMODUnity;
using MyFolder._1._Scripts._1._UI._0._GameStage._2._Card;
using UnityEngine;

namespace MyFolder._1._Scripts._10._Sound
{
    public class CardUISfx : MonoBehaviour
    {
        [SerializeField] private EventReference cardSwitch;
        [SerializeField] private EventReference cardSelect;

        private CardSelectionUI cardSelectionUI;
        private void Start()
        {
            if (TryGetComponent(out cardSelectionUI))
            {
                cardSelectionUI.CardSelect += CardSelect;
                cardSelectionUI.CardSwich += CardSwitch;
            }
        }

        private void CardSelect()
        {
            RuntimeManager.PlayOneShot(cardSelect);
        }

        private void CardSwitch()
        {
            RuntimeManager.PlayOneShot(cardSwitch);
        }
    }
}