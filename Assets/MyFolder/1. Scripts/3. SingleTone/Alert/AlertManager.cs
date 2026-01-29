using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._1._UI;
using MyFolder._1._Scripts._4._Network;
using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    /// <summary>
    /// 전역 경고창 관리자
    /// 로비, 인증, 네트워크 에러 등을 사용자에게 표시
    /// </summary>
    public class AlertManager : SingleTone<AlertManager>
    {
        [Header("경고창 프리팹")]
        [SerializeField] private AlertPopup alertPopupPrefab;
        [SerializeField] private Transform alertCanvas;
        
        [Header("디버그")]
        [SerializeField] private bool showDebugLogs = true;
        
        private Queue<AlertData> alertQueue = new Queue<AlertData>();
        private AlertPopup currentPopup;
        private bool isShowingAlert = false;

        public enum AlertType
        {
            Info,       // 정보
            Warning,    // 경고
            Error,      // 에러
            Success     // 성공
        }

        protected override void Awake()
        {
            base.Awake();
            
            // Canvas 자동 설정 (없으면 찾기)
            if (alertCanvas == null)
            {
                alertCanvas = transform;
            }
        }

        private void Start()
        {
            // NetworkStateManager 이벤트 구독
            if (NetworkStateManager.Instance != null)
            {
                NetworkStateManager.Instance.OnErrorOccurred += ShowNetworkError;
                
                if (showDebugLogs)
                {
                    LogManager.Log(LogCategory.System, "AlertManager: NetworkStateManager 이벤트 구독 완료", this);
                }
            }
            else
            {
                LogManager.LogWarning(LogCategory.System, "AlertManager: NetworkStateManager를 찾을 수 없습니다.", this);
            }
        }

        private void OnDestroy()
        {
            if (NetworkStateManager.Instance != null)
            {
                NetworkStateManager.Instance.OnErrorOccurred -= ShowNetworkError;
            }
        }

        /// <summary>
        /// 경고창 표시 (큐에 추가)
        /// </summary>
        public void ShowAlert(string title, string message, AlertType type = AlertType.Info, Action onConfirm = null)
        {
            var alertData = new AlertData
            {
                title = title,
                message = message,
                type = type,
                onConfirm = onConfirm
            };

            alertQueue.Enqueue(alertData);
            
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.UI, $"AlertManager: 경고창 추가 [{type}] {title}: {message}", this);
            }
            
            if (!isShowingAlert)
            {
                ShowNextAlert();
            }
        }

        /// <summary>
        /// 네트워크 에러 자동 표시
        /// </summary>
        private void ShowNetworkError(string errorMessage)
        {
            ShowAlert("네트워크 오류", errorMessage, AlertType.Error);
        }

        /// <summary>
        /// 다음 경고창 표시
        /// </summary>
        private void ShowNextAlert()
        {
            if (alertQueue.Count == 0)
            {
                isShowingAlert = false;
                return;
            }

            isShowingAlert = true;
            var alertData = alertQueue.Dequeue();

            // 프리팹이 설정되지 않은 경우 콘솔 로그로 대체
            if (!alertPopupPrefab)
            {
                LogManager.LogWarning(LogCategory.UI, 
                    $"AlertManager: 프리팹 미설정. 콘솔 출력 - [{alertData.type}] {alertData.title}: {alertData.message}", this);
                
                alertData.onConfirm?.Invoke();
                ShowNextAlert();
                return;
            }

            if (currentPopup)
            {
                Destroy(currentPopup.gameObject);
            }

            currentPopup = Instantiate(alertPopupPrefab, alertCanvas);
            currentPopup.Show(alertData, OnAlertClosed);
        }

        private void OnAlertClosed()
        {
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.UI, "AlertManager: 경고창 닫힘", this);
            }
            
            ShowNextAlert();
        }

        /// <summary>
        /// 현재 표시 중인 알람 즉시 닫기
        /// </summary>
        public void CloseCurrentAlert()
        {
            if (currentPopup != null)
            {
                currentPopup.Close();
            }
        }

        #region 간편 메서드
        
        /// <summary>
        /// 에러 메시지 표시
        /// </summary>
        public void ShowError(string message, Action onConfirm = null) 
            => ShowAlert("오류", message, AlertType.Error, onConfirm);
        
        /// <summary>
        /// 경고 메시지 표시
        /// </summary>
        public void ShowWarning(string message, Action onConfirm = null) 
            => ShowAlert("경고", message, AlertType.Warning, onConfirm);
        
        /// <summary>
        /// 정보 메시지 표시
        /// </summary>
        public void ShowInfo(string message, Action onConfirm = null) 
            => ShowAlert("알림", message, AlertType.Info, onConfirm);
        
        /// <summary>
        /// 성공 메시지 표시
        /// </summary>
        public void ShowSuccess(string message, Action onConfirm = null) 
            => ShowAlert("성공", message, AlertType.Success, onConfirm);
        
        #endregion

        #region 인스펙터 테스트 메서드

        [ContextMenu("테스트: 에러 메시지")]
        private void TestError()
        {
            ShowError("테스트 에러 메시지입니다.");
        }

        [ContextMenu("테스트: 경고 메시지")]
        private void TestWarning()
        {
            ShowWarning("테스트 경고 메시지입니다.");
        }

        [ContextMenu("테스트: 정보 메시지")]
        private void TestInfo()
        {
            ShowInfo("테스트 정보 메시지입니다.");
        }

        [ContextMenu("테스트: 성공 메시지")]
        private void TestSuccess()
        {
            ShowSuccess("테스트 성공 메시지입니다.");
        }

        [ContextMenu("테스트: 여러 메시지 큐잉")]
        private void TestMultipleAlerts()
        {
            ShowInfo("첫 번째 메시지");
            ShowWarning("두 번째 메시지");
            ShowError("세 번째 메시지");
            ShowSuccess("네 번째 메시지");
        }

        #endregion
    }

    /// <summary>
    /// 경고창 데이터
    /// </summary>
    [Serializable]
    public class AlertData
    {
        public string title;
        public string message;
        public AlertManager.AlertType type;
        public Action onConfirm;
    }
}

