using UnityEngine;

namespace MyFolder._1._Scripts._8998._PositionClass
{
    public class RotationAntiSync : MonoBehaviour
    {
        public Transform target;

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.x, target.rotation.x * -1);
        }
    }
}
