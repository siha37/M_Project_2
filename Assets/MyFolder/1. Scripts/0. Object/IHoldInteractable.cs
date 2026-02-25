using UnityEngine;

namespace MyFolder._1._Scripts._0._Object
{
    /// <summary>
    /// 일정 시간 동안 홀드해야 활성화되는 상호작용 인터페이스.
    /// IInteractable과 동시 사용 불가 — PlayerInteractController에서 우선 확인 후 처리.
    /// </summary>
    public interface IHoldInteractable
    {
        float HoldDuration { get; }
        void StartHold(GameObject interactor);
        void CancelHold(GameObject interactor);
        void CompleteHold(GameObject interactor);
    }
}
