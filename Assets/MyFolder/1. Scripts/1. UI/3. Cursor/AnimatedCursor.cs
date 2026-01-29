using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._3._Cursor
{
    public class AnimatedCursor : MonoBehaviour
    {
        [SerializeField] private RectTransform cursorTransform;
        [SerializeField] private Animator cursorAnimator;
        [SerializeField] private Canvas cursorCanvas;
        [SerializeField] private Image reloadImage;
    
        // 애니메이션 상태 (Idle, Click, Hover 등)
        private static readonly int ShotHash = Animator.StringToHash("Shot");
        private static readonly string ReloadHash = "Reload";
        private static readonly string ReloadShotHash = "ReloadShot";
        private static readonly string EmptyHash = "Empty";
        private static readonly string EmptyShotHash = "EmptyShot";
    
        private bool isCustomCursorActive = true;
        private void Awake()
        {
            // CursorModeManager 이벤트 구독
            if (CursorModeManager.Instance)
            {
                CursorModeManager.Instance.OnCursorModeChanged += OnCursorModeChanged;
            }
            
            // 기본은 커스텀 커서 (인게임)
            OnCursorModeChanged(false);
        }

        private void OnDestroy()
        {
            OnCursorModeChanged(true);
        }
        
        /// <summary>
        /// CursorModeManager로부터 호출되는 콜백
        /// </summary>
        /// <param name="useSystemCursor">true = 기본 커서, false = 커스텀 커서</param>
        private void OnCursorModeChanged(bool useSystemCursor)
        {
            SetCustomCursorMode(!useSystemCursor);
        }
        /// <summary>
        /// 커스텀 커서 모드 설정
        /// </summary>
        private void SetCustomCursorMode(bool enable)
        {
            isCustomCursorActive = enable;
            
            if (enable)
            {
                // 커스텀 애니메이션 커서 활성화
                Cursor.visible = false;
            }
            else
            {
                // 기본 시스템 커서 활성화
                Cursor.visible = true;
            }
        }

        
        public void Update()
        {
            // 커스텀 커서가 활성화되어 있을 때만 업데이트
            if (isCustomCursorActive)
            {
                UpdateCursorPosition();
            }
        }
    
        private void UpdateCursorPosition()
        {
            if (Mouse.current != null)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();
                cursorTransform.position = mousePosition;
            }
        }
    
        public void ShotCursor()
        {
            cursorAnimator.SetTrigger(ShotHash);
        }

        public void ReloadCursor(bool enable)
        {
            cursorAnimator.SetBool(ReloadHash,enable);
            if(enable)
                cursorAnimator.SetTrigger(ReloadShotHash);
        }

        public void ReloadCursor_Update(float progress)
        {
            reloadImage.fillAmount = Mathf.Clamp01(progress);
        }
        public void EmptyCursor(bool enable)
        {
            cursorAnimator.SetBool(EmptyHash,enable);
            cursorAnimator.SetTrigger(EmptyShotHash);
        }
        
    }
}