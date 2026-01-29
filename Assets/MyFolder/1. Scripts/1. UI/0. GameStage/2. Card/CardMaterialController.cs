using System.Collections;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._2._Card
{
    public class CardMaterialController : MonoBehaviour
    {
        private static readonly int DissolveAmount = Shader.PropertyToID("_DissolveAmount");
        
        [SerializeField] Image dissolveImage;
        [SerializeField] TextMeshProUGUI cardDescription;
        [SerializeField] TextMeshProUGUI cardName;
        
        Material CardMaterial;
        Material CardTextMaterial_1;
        Material CardTextMaterial_2;
        const float dissolveRate =0.03f;
        const float refreshRate =0.05f;

        public void Start()
        {
            CardMaterial = new Material(dissolveImage.material);
            CardTextMaterial_1 = new Material(cardDescription.material);
            CardTextMaterial_2 = new Material(cardName.material);
            dissolveImage.material = CardMaterial;
            cardDescription.material = CardTextMaterial_1;
            cardName.material = CardTextMaterial_2;
            
            CardMaterial.SetFloat(DissolveAmount, 0);
            CardTextMaterial_1.SetFloat(DissolveAmount, 0.01f);
            CardTextMaterial_2.SetFloat(DissolveAmount, 0.01f);
            
            //CardMaterial = dissolveImage.material;
            //CardTextMaterial_1 = cardName.material;
            //CardTextMaterial_2 = cardDescription.material;
        }

        public void CardDissolveReset()
        {
            if(CardMaterial)
                CardMaterial.SetFloat(DissolveAmount, 0);
            if(CardTextMaterial_1)
                CardTextMaterial_1.SetFloat(DissolveAmount, 0.01f);
            if(CardTextMaterial_2)
                CardTextMaterial_2.SetFloat(DissolveAmount, 0.01f);
        }
        public void CardDissolveStart()
        {
            StartCoroutine(nameof(DissolveCo));
        }

        public void CardDissolveEnd()
        {
            StopCoroutine(nameof(DissolveCo));
        }

        private IEnumerator DissolveCo()
        {
            float counter = 0;
            while (CardMaterial.GetFloat(DissolveAmount) < 1)
            {
                counter += dissolveRate;
                CardMaterial.SetFloat(DissolveAmount, counter);
                CardTextMaterial_1.SetFloat(DissolveAmount, counter);
                CardTextMaterial_2.SetFloat(DissolveAmount, counter);
                yield return WaitForSecondsCache.Get(refreshRate);
            }
        }
    }
}
