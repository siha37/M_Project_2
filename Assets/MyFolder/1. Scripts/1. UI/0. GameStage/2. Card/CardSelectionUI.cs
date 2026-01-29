using System;
using System.Collections.Generic;
using FishNet.Object;
using MoreMountains.Feedbacks;
using MyFolder._1._Scripts._11._Feel;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._6._GlobalQuest._3._Card;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._2._Card
{
    public class CardSelectionUI : MonoBehaviour
    {
        private static readonly int SelectCard = Animator.StringToHash("SelectCard");
        private static readonly int Confirm = Animator.StringToHash("Confirm");

        /// <summary>
        /// 클래스 상단에 추가
        /// </summary>
        private Queue<CardSelectionRequest> pendingSelections = new Queue<CardSelectionRequest>();
        private bool isShowingCard = false;

        /// <summary>
        /// 요청 데이터 구조
        /// </summary>
        /// <returns></returns>
        private class CardSelectionRequest
        {
            public bool isReward;
            public List<QuestCardManager.RewardCardInstance> rewardCards;
            public List<QuestCardManager.DefeatCardInstance> defeatCards;
            public Action<int> callback;
        }
        
        [Header("UI References")]
        [SerializeField] private GameObject cardSelectionPanel;
        [SerializeField] private GameObject[] cardSlots;           // 카드 슬롯 (고정된 3개 슬롯)
        [SerializeField] private TextMeshProUGUI[] cardNameTexts;
        [SerializeField] private TextMeshProUGUI[] cardDescriptionTexts;
        [SerializeField] private TextMeshProUGUI[] cardValueTexts;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI instructionText; // 조작 안내 텍스트
        [SerializeField] private Animator cardSelectionAnimator;
        [SerializeField] private Image TimeOut;
        
        [Header("Input Settings")]
        [SerializeField] private InputActionReference cardSelectAction_1;    // 카드 선택 액션 (1,2,3 키)
        [SerializeField] private InputActionReference cardSelectAction_2;    // 카드 선택 액션 (1,2,3 키)
        [SerializeField] private InputActionReference cardSelectAction_3;    // 카드 선택 액션 (1,2,3 키)
        [SerializeField] private InputActionReference cancelAction;        // 취소 액션 (ESC 키)
        
        [Header("Settings")]
        [SerializeField] private float displayDuration = 30f; // 카드 선택 제한 시간
        
        // 현재 선택 상태
        private List<QuestCardManager.RewardCardInstance> currentRewardCards;
        private List<QuestCardManager.DefeatCardInstance> currentDefeatCards;
        private int selectedCardIndex = -1;
        private bool isRewardSelection = true;
        public Action<int> onCardSelected;
        public Action CardSelect;
        public Action CardSwich;
        private float selectionStartTime;
        
        public static CardSelectionUI Instance { get; private set; }
        
        private void Awake()
        {
            if (!Instance)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializeUI();
        }
        
        private void InitializeUI()
        {
            // 초기 상태 설정
            if (cardSelectionPanel)
                cardSelectionPanel.SetActive(false);
                
            // Input Action 활성화
            EnableInputActions();
            
            // 조작 안내 텍스트 설정
            if (instructionText)
                instructionText.text = "1,2,3: 카드 선택 | 같은 숫자 다시 누르기: 확정 | ESC: 취소";
        }
        
        /// <summary>
        /// Input Action 활성화
        /// </summary>
        private void EnableInputActions()
        {
            if (cardSelectAction_1 && cardSelectAction_1.action != null)
            {
                cardSelectAction_1.action.Enable();
                cardSelectAction_1.action.performed += OnCardSelectInput;
            }
            if (cardSelectAction_2 && cardSelectAction_2.action != null)
            {
                cardSelectAction_2.action.Enable();
                cardSelectAction_2.action.performed += OnCardSelectInput;
            }
            if (cardSelectAction_3 && cardSelectAction_3.action != null)
            {
                cardSelectAction_3.action.Enable();
                cardSelectAction_3.action.performed += OnCardSelectInput;
            }
        }
        
        /// <summary>
        /// Input Action 비활성화
        /// </summary>
        private void DisableInputActions()
        {
            if (cardSelectAction_1 && cardSelectAction_1.action != null)
            {
                cardSelectAction_1.action.performed -= OnCardSelectInput;
                cardSelectAction_1.action.Disable();
            }
            
            if (cardSelectAction_2 && cardSelectAction_2.action != null)
            {
                cardSelectAction_2.action.performed -= OnCardSelectInput;
                cardSelectAction_2.action.Disable();
            }
            
            if (cardSelectAction_3 && cardSelectAction_3.action != null)
            {
                cardSelectAction_3.action.performed -= OnCardSelectInput;
                cardSelectAction_3.action.Disable();
            }
        }
        

        /// <summary>
        /// 보상 카드 선택 UI 표시 (큐 적용)
        /// </summary>
        public void ShowRewardCards(List<QuestCardManager.RewardCardInstance> rewardCards, Action<int> onSelected = null)
        {
            var request = new CardSelectionRequest
            {
                isReward = true,
                rewardCards = rewardCards,
                defeatCards = null,
                callback = onSelected
            };
    
            if (isShowingCard)
            {
                // 이미 표시 중이면 큐에 추가
                pendingSelections.Enqueue(request);
                LogManager.Log(LogCategory.UI, $"보상 카드 선택 요청이 큐에 추가됨 (대기: {pendingSelections.Count})", this);
            }
            else
            {
                // 즉시 표시
                ShowCardImmediate(request);
            }
        }
        
        /// <summary>
        /// 패배 카드 선택 UI 표시 (큐 적용)
        /// </summary>
        public void ShowDefeatCards(List<QuestCardManager.DefeatCardInstance> defeatCards, Action<int> onSelected = null)
        {
            var request = new CardSelectionRequest
            {
                isReward = false,
                rewardCards = null,
                defeatCards = defeatCards,
                callback = onSelected
            };
    
            if (isShowingCard)
            {
                // 이미 표시 중이면 큐에 추가
                pendingSelections.Enqueue(request);
                LogManager.Log(LogCategory.UI, $"패배 카드 선택 요청이 큐에 추가됨 (대기: {pendingSelections.Count})", this);
            }
            else
            {
                // 즉시 표시
                ShowCardImmediate(request);
            }
        }
        
        /// <summary>
        /// 실제로 카드 UI를 표시
        /// </summary>
        private void ShowCardImmediate(CardSelectionRequest request)
        {
            isShowingCard = true;
    
            
            selectedCardIndex = -1;
            selectionStartTime = Time.time;
            TimeOut.gameObject.SetActive(true);
            onCardSelected = WrapCallback(request.callback);
            
            if (request.isReward)
            {
                currentRewardCards = request.rewardCards;
                currentDefeatCards = null;
                isRewardSelection = true;
        
                UpdateCardDisplay();
                ShowSelectionPanel("보상 카드 선택", "다음 중 하나를 선택하세요:");
        
                LogManager.Log(LogCategory.UI, $"보상 카드 선택 UI 표시: {request.rewardCards.Count}장", this);
            }
            else
            {
                currentRewardCards = null;
                currentDefeatCards = request.defeatCards;
                isRewardSelection = false;
        
                UpdateCardDisplay();
                ShowSelectionPanel("패배 카드 선택", "AI 강화 효과를 선택하세요:");
        
                LogManager.Log(LogCategory.UI, $"패배 카드 선택 UI 표시: {request.defeatCards.Count}장", this);
            }
        }

        /// <summary>
        /// 콜백을 래핑하여 완료 후 다음 요청 처리
        /// </summary>
        private Action<int> WrapCallback(Action<int> originalCallback)
        {
            return (index) =>
            {
                // 원래 콜백 호출
                originalCallback?.Invoke(index);
        
                // 완료 처리
                isShowingCard = false;
        
                // 대기 중인 요청이 있으면 다음 표시
                if (pendingSelections.Count > 0)
                {
                    var nextRequest = pendingSelections.Dequeue();
                    ShowCardImmediate(nextRequest);
                }
            };
        }
        /// <summary>
        /// 카드 선택 패널 표시
        /// </summary>
        private void ShowSelectionPanel(string title, string description)
        {
            if (cardSelectionPanel)
                cardSelectionPanel.SetActive(true);
                
            if (titleText)
                titleText.text = title;
        }
        
        /// <summary>
        /// 카드 표시 업데이트
        /// </summary>
        private void UpdateCardDisplay()
        {
            if (isRewardSelection && currentRewardCards != null)
            {
                for (int i = 0; i < cardSlots.Length && i < currentRewardCards.Count; i++)
                {
                    var card = currentRewardCards[i];
                    UpdateCardUI(i, card.baseData.cardName, card.baseData.description, 
                        $"{card.rewardType}: {card.actualPercentage:F1}%", true);
                }
                
                // 사용하지 않는 카드 슬롯 비활성화
                for (int i = currentRewardCards.Count; i < cardSlots.Length; i++)
                {
                    if (cardSlots[i])
                        cardSlots[i].SetActive(false);
                }
            }
            else if (!isRewardSelection && currentDefeatCards != null)
            {
                for (int i = 0; i < cardSlots.Length && i < currentDefeatCards.Count; i++)
                {
                    var card = currentDefeatCards[i];
                    UpdateCardUI(i, card.baseData.cardName, card.baseData.description, 
                        $"적군 {card.defeatType}: {card.actualPercentage:F1}%", true);
                }
                
                // 사용하지 않는 카드 슬롯 비활성화
                for (int i = currentDefeatCards.Count; i < cardSlots.Length; i++)
                {
                    if (cardSlots[i])
                        cardSlots[i].SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// 개별 카드 UI 업데이트
        /// </summary>
        private void UpdateCardUI(int index, string cardName, string description, string value, bool isActive)
        {
            if (index >= cardSlots.Length) return;
            
            if (cardSlots[index])
            {
                cardSlots[index].SetActive(isActive);
            }
            
            if (index < cardNameTexts.Length && cardNameTexts[index])
                cardNameTexts[index].text = cardName;
                
            if (index < cardDescriptionTexts.Length && cardDescriptionTexts[index])
                cardDescriptionTexts[index].text = description;
                
            if (index < cardValueTexts.Length && cardValueTexts[index])
                cardValueTexts[index].text = value;
        }
        
        /// <summary>
        /// 카드 선택 Input 처리 (1,2,3 키)
        /// </summary>
        private void OnCardSelectInput(InputAction.CallbackContext context)
        {
            if (!cardSelectionPanel.activeSelf) return;
            
            string inputName = context.control.name;
            int cardIndex = -1;
            
            // 키보드 숫자 키 매핑
            switch (inputName)
            {
                case "1": cardIndex = 0; break;
                case "2": cardIndex = 1; break;
                case "3": cardIndex = 2; break;
            }
            
            if (cardIndex >= 0 && IsValidCardIndex(cardIndex))
            {
                // 이미 선택된 카드와 같은 번호를 다시 누르면 확정
                if (selectedCardIndex == cardIndex)
                {
                    // 카드 확정
                    cardSelectionAnimator.SetTrigger(Confirm);
                    ConfirmCardSelection();
                }
                else
                {
                    // 새로운 카드 선택
                    selectedCardIndex = cardIndex;
                    cardSelectionAnimator.SetInteger(SelectCard, selectedCardIndex+1);
                    CardSwich?.Invoke();
                    //UpdateCardDisplay(); // 선택 상태 반영
                    
                    LogManager.Log(LogCategory.UI,$"카드 선택: 인덱스 {cardIndex} (키: {inputName})", this);
                }
            }
        }
        
        
        /// <summary>
        /// 유효한 카드 인덱스인지 확인
        /// </summary>
        private bool IsValidCardIndex(int index)
        {
            if (isRewardSelection && currentRewardCards != null)
                return index >= 0 && index < currentRewardCards.Count;
            if (!isRewardSelection && currentDefeatCards != null)
                return index >= 0 && index < currentDefeatCards.Count;
            return false;
        }
        
        /// <summary>
        /// 카드 선택 확정 처리
        /// </summary>
        private void ConfirmCardSelection()
        {
            if (selectedCardIndex < 0) return;
            
            // 선택 완료 처리
            onCardSelected?.Invoke(selectedCardIndex);
            CardSelect?.Invoke();
            HideSelectionPanel();
            
            Feel_InGame.Instance.CardTimeOutFeel_Stop();
            TimeOut.gameObject.SetActive(false);
            
            LogManager.Log(LogCategory.UI, $"카드 선택 확정: 인덱스 {selectedCardIndex} (숫자키 재입력)", this);
        }
        
        
        /// <summary>
        /// 선택 패널 숨기기
        /// </summary>
        private void HideSelectionPanel()
        {
            currentRewardCards = null;
            currentDefeatCards = null;
            onCardSelected = null;
            selectedCardIndex = -1;
        }
        
        /// <summary>
        /// 시간 제한 체크
        /// </summary>
        private void Update()
        {
            if (cardSelectionPanel && cardSelectionPanel.activeSelf)
            {
                float elapsedTime = Time.time - selectionStartTime;
                if (elapsedTime >= displayDuration)
                {
                    int cardCount = isRewardSelection ? (currentRewardCards?.Count ?? 0) : (currentDefeatCards?.Count ?? 0);
                    // 시간 초과 - 자동으로 랜덤 선택
                    // 자동으로 첫 번째 카드 선택
                    selectedCardIndex = Random.Range(0, cardCount);
                    // 카드 확정
                    cardSelectionAnimator.SetInteger(SelectCard, selectedCardIndex+1);
                    
                    cardSelectionAnimator.SetTrigger(Confirm);
                    ConfirmCardSelection();
                }
                else if (elapsedTime >= displayDuration - 10)
                {
                    Feel_InGame.Instance.CardTimeOutFeel_Start();
                }
            }
        }

        public void AnimIndex_Reset()
        {
            cardSelectionAnimator.SetInteger(SelectCard, 0);
        }
        
        /// <summary>
        /// UI 정리
        /// </summary>
        private void OnDestroy()
        {
            DisableInputActions();
        }
    }
}
