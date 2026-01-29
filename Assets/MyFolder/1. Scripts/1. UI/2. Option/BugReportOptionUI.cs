using MyFolder._1._Scripts._3._SingleTone;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._2._Option
{
    /// <summary>
    /// 옵션 메뉴의 버그 리포트 버튼 UI
    /// - 버튼 클릭 시 BugReporter 호출
    /// - 사용자 설명 입력 필드 (선택사항)
    /// </summary>
    public class BugReportOptionUI : MonoBehaviour
    {
        [Header("UI 참조")]
        [SerializeField] private Button bugReportButton;
        [SerializeField] private TMP_InputField descriptionInputField;
        
        [Header("디버그")]
        [SerializeField] private bool showDebugLogs = true;
        
        private void Start()
        {
            if (bugReportButton == null)
            {
                bugReportButton = GetComponent<Button>();
            }
            
            if (bugReportButton != null)
            {
                bugReportButton.onClick.AddListener(OnBugReportButtonClicked);
            }
            else
            {
                LogManager.LogError(LogCategory.UI, 
                    "BugReportOptionUI: Button 컴포넌트를 찾을 수 없습니다!", this);
            }
        }
        
        private void OnDestroy()
        {
            if (bugReportButton != null)
            {
                bugReportButton.onClick.RemoveListener(OnBugReportButtonClicked);
            }
        }
        
        /// <summary>
        /// 버그 리포트 버튼 클릭 이벤트
        /// </summary>
        private void OnBugReportButtonClicked()
        {
            if (showDebugLogs)
            {
                LogManager.Log(LogCategory.UI, "버그 리포트 버튼 클릭됨", this);
            }
            
            // BugReporter 싱글톤 호출
            if (BugReporter.Instance != null)
            {
                // InputField에서 사용자 설명 가져오기 (없으면 빈 문자열)
                string userDescription = "";
                if (descriptionInputField != null && !string.IsNullOrEmpty(descriptionInputField.text))
                {
                    userDescription = descriptionInputField.text;
                }
                
                if (showDebugLogs)
                {
                    LogManager.Log(LogCategory.UI, 
                        $"버그 리포트 전송: '{userDescription}'", this);
                }
                
                BugReporter.Instance.SendBugReport(userDescription);
                
                // 전송 후 InputField 초기화 (선택사항)
                if (descriptionInputField != null)
                {
                    descriptionInputField.text = "";
                }
            }
            else
            {
                LogManager.LogError(LogCategory.UI, 
                    "BugReportOptionUI: BugReporter 인스턴스를 찾을 수 없습니다!", this);
                
                // 에러 알림 표시
                if (AlertManager.Instance != null)
                {
                    AlertManager.Instance.ShowAlert(
                        "시스템 오류",
                        "버그 리포트 시스템을 초기화할 수 없습니다.",
                        AlertManager.AlertType.Error
                    );
                }
            }
        }
    }
}
