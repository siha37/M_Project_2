using System;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._3._Cursor
{
    /// <summary>
    /// 게임 전체의 커서 모드를 관리하는 싱글톤 매니저
    /// UI 열림/닫힘에 따라 커서 모드를 전환
    /// </summary>
    public class CursorModeManager : MonoBehaviour
    {
        public static CursorModeManager Instance { get; private set; }

        // 커서 모드 변경 이벤트
        public event Action<bool> OnCursorModeChanged;

        private int uiOpenCount = 0; // 열린 UI 개수 (중첩 UI 대응)

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            OnCursorModeChanged = null;
        }

        /// <summary>
        /// UI가 열렸을 때 호출
        /// </summary>
        public void OnUIOpened()
        {
            uiOpenCount++;
            if (uiOpenCount == 1) // 첫 번째 UI가 열렸을 때만
            {
                OnCursorModeChanged?.Invoke(true); // true = 기본 커서 사용
            }
        }

        /// <summary>
        /// UI가 닫혔을 때 호출
        /// </summary>
        public void OnUIClosed()
        {
            uiOpenCount = Mathf.Max(0, uiOpenCount - 1);
            if (uiOpenCount == 0) // 모든 UI가 닫혔을 때
            {
                OnCursorModeChanged?.Invoke(false); // false = 커스텀 애니메이션 커서
            }
        }

        /// <summary>
        /// 강제로 커서 모드 설정 (씬 전환 시 등)
        /// </summary>
        public void SetCursorMode(bool useSystemCursor)
        {
            OnCursorModeChanged?.Invoke(useSystemCursor);
        }
    }
}