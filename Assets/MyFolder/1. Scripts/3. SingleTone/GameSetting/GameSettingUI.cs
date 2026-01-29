using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._3._SingleTone.GameSetting
{
    public class GameSettingUI : MonoBehaviour
    {
        //역할 UI
        [SerializeField] private TextMeshProUGUI normalText;
        [SerializeField] private TextMeshProUGUI destroyerText;

        [SerializeField] private Button destroyerUpButton; 
        [SerializeField] private Button destroyerDownButton; 
        
        // 시각화
        [SerializeField] private Color baseColor;
        [SerializeField] private Color limitColor;
        public void Start()
        {
            // UI 상태 초기화
            destroyerText.color = baseColor;
            
            //Button 이벤트 등록
            destroyerUpButton.onClick.AddListener(DestroyerUpCount);
            destroyerDownButton.onClick.AddListener(DestroyerDownCount);
            
            // 상태 변경 콜백 등록
            GameSettingManager.Instance.A_RoleStateChanged += RoleStateChanged;
            RoleStateChanged();
        }

        private void RoleStateChanged()
        { 
            // 현 수량 업데이트
            int _destroyerMaxAmount = GameSettingManager.Instance.GetDestoryerMaxAmount();
            int _normalCurrentAmount = GameSettingManager.Instance.GetNormalCurrentAmount(); 
            int _destroyerCurrentAmount = GameSettingManager.Instance.GetDestroyerCurrentAmount(); 
            
            // UI 업데이트
            normalText.text = _normalCurrentAmount.ToString();
            destroyerText.text = _destroyerCurrentAmount.ToString();

            
            //제거자 역할 최대 가능 수가 1이하일 시
            if (_destroyerMaxAmount <= 1)
            {
                ShowDestroyerCountLimit();
                DestroyerUpCountDisActive();
                DestroyerDownCountDisActive();
            }
            //제거자 역할 최대 수량에 도달함 (위 비활성화, 아래 활성화)
            else if(_destroyerCurrentAmount >= _destroyerMaxAmount)
            {
                ShowDestroyerCountLimit();
                DestroyerUpCountDisActive();
                DestroyerDownCountActive();
            }
            //제거자 역할 최소 수량에 도달함 (위 활성화, 아래 비활성화)
            else if (_destroyerCurrentAmount == 1)
            {
                ShowDestroyerCountLimit();
                DestroyerDownCountDisActive();
                DestroyerUpCountActive();
            }
            // (위/아래 활성화)
            else
            {
                ShowDestroyerCountUnLimit();
                DestroyerDownCountActive();
                DestroyerUpCountActive();
            }
        }
        
        /// <summary>
        /// 제거자 수 증가 버튼 활성화
        /// </summary>
        private void DestroyerUpCountActive()
        {
            destroyerUpButton.interactable = true;
        }

        /// <summary>
        /// 제거자 수 감소 버튼 활성화
        /// </summary>
        private void DestroyerDownCountActive()
        {
            destroyerDownButton.interactable = true;
        }

        /// <summary>
        /// 제거자 수 증가 버튼 비활성화
        /// </summary>
        private void DestroyerUpCountDisActive()
        {
            destroyerUpButton.interactable = false;
        }
        
        /// <summary>
        /// 제거자 수 감소 버튼 비활성화
        /// </summary>
        private void DestroyerDownCountDisActive()
        {
            destroyerDownButton.interactable = false;
        }

        /// <summary>
        /// 제거자 수량 제한에 도달 표기
        /// </summary>
        private void ShowDestroyerCountLimit()
        {
            destroyerText.color = limitColor;
        }
        
        /// <summary>
        /// 제거자 수량 제한에 도달 안함 표기
        /// </summary>
        private void ShowDestroyerCountUnLimit()
        {
            destroyerText.color = baseColor;
        }

        /// <summary>
        /// 제거자 수량 증가 함수
        /// </summary>
        private void DestroyerUpCount()
        {
            DestroyerCount(1);   
        }

        /// <summary>
        /// 제거자 수량 감소 함수
        /// </summary>
        private void DestroyerDownCount()
        {
            DestroyerCount(-1);
        }

        /// <summary>
        /// 제거자 수치 증감
        /// </summary>
        private void DestroyerCount(int count)
        {
            int _destroyerCurrentAmount = GameSettingManager.Instance.GetDestroyerCurrentAmount(); 
            GameSettingManager.Instance.SetDestroyerAmount(_destroyerCurrentAmount+count);
        }
    }
}