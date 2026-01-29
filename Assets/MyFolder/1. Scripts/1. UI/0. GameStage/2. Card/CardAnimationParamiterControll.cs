using System;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._2._Card
{
    public class CardAnimationParamiterControll : MonoBehaviour
    {
        [SerializeField] CardSelectionUI cardSelectionUI;
        
        [SerializeField] CardMaterialController[] cardMaterialControllers = new CardMaterialController[3];
        
        public void cardReset()
        {
            cardSelectionUI.AnimIndex_Reset();
        }

        private void OnDisable()
        {
            foreach(var cardMaterialController in cardMaterialControllers)
                cardMaterialController.CardDissolveReset();
        }

        public void DissolveOnTrigger(int index)
        {
            cardMaterialControllers[index-1].CardDissolveStart();
        }

        public void DissolveOffTrigger(int index)
        {
            cardMaterialControllers[index-1].CardDissolveEnd();
        }
    }
}
