using System.Collections.Generic;
using System.Globalization;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._2._Card
{
    public class CardRewardRecordBoard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI CardTextPrefab;
        [SerializeField] private GameObject CardTextParent;
        
        private Dictionary<StatType,TextMeshProUGUI> RewardRecordBoard = new Dictionary<StatType, TextMeshProUGUI>(); 
        private Dictionary<StatType,float> RewardRecordBoardAmount = new Dictionary<StatType, float>(); 
        
        
        private Dictionary<StatType,TextMeshProUGUI> ERewardRecordBoard = new Dictionary<StatType, TextMeshProUGUI>(); 
        private Dictionary<StatType,float> ERewardRecordBoardAmount = new Dictionary<StatType, float>(); 
        public void AddRecord(StatType rewardType,string cardName,float rewardAmount)
        {
            if (!RewardRecordBoard.TryGetValue(rewardType, out var newCard1))
            {
                TextMeshProUGUI newCard = Instantiate(CardTextPrefab, CardTextParent.transform);
                float cardAmount = rewardAmount;
                newCard.text = cardName + "-" + cardAmount.ToString(CultureInfo.InvariantCulture) + "%";
                newCard.gameObject.SetActive(true);
                RewardRecordBoard.Add(rewardType,newCard);
                RewardRecordBoardAmount.Add(rewardType,cardAmount);
            }
            else
            {
                float cardAmount =RewardRecordBoardAmount[rewardType]+ rewardAmount;
                RewardRecordBoardAmount[rewardType] = cardAmount;
                newCard1.text = cardName + "-" + cardAmount.ToString(CultureInfo.InvariantCulture) + "%";
            }
        }
        public void AddERecord(StatType rewardType,string cardName,float rewardAmount)
        {
            if (!ERewardRecordBoard.TryGetValue(rewardType, out var newCard1))
            {
                TextMeshProUGUI newCard = Instantiate(CardTextPrefab, CardTextParent.transform);
                float cardAmount = rewardAmount;
                newCard.text = cardName + "-" + cardAmount.ToString(CultureInfo.InvariantCulture) + "%";
                newCard.gameObject.SetActive(true);
                ERewardRecordBoard.Add(rewardType,newCard);
                ERewardRecordBoardAmount.Add(rewardType,cardAmount);
            }
            else
            {
                float cardAmount =ERewardRecordBoardAmount[rewardType]+ rewardAmount;
                ERewardRecordBoardAmount[rewardType] = cardAmount;
                newCard1.text = cardName + "-" + cardAmount.ToString(CultureInfo.InvariantCulture) + "%";
            }
        }
        
    }
}
