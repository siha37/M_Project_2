using MyFolder._1._Scripts._1._UI._3._Cursor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._1._Map
{
    public class UIOpenAction : MonoBehaviour
    {
        [SerializeField] private InputActionReference action; 
        [SerializeField] private GameObject target;
        
        private void Start()
        {
            if(action && action.action != null)
                action.action.performed += OnKey;
        }

        private void OnDestroy()
        {
            if(action && action.action != null)
                action.action.performed -= OnKey;
        }

        private void OnKey(InputAction.CallbackContext context)
        {
            if (!target.activeSelf)
                CursorModeManager.Instance.OnUIOpened();
            else
                CursorModeManager.Instance.OnUIClosed();
            target.SetActive(!target.activeSelf);
        }
    }
}