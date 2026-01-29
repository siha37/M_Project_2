using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using UnityEngine;

namespace MyFolder._1._Scripts._0._Object._6._DynamicObject
{
    public class DoorCast : MonoBehaviour
    {
        private DoorObject doorObject;

        private void Start()
        {
            transform.parent.TryGetComponent(out doorObject);
        }

        private void OnTriggerEnter2D(Collider2D col)
        {
            if (col.gameObject.CompareTag("Player"))
            {
                if (col.TryGetComponent(out PlayerNetworkSync playerNetworkSync))
                {
                    if (playerNetworkSync.IsOwner)
                    {
                        doorObject.GetTarget(col.transform);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D col)
        {
            if (col.gameObject.CompareTag("Player"))
            {
                if (col.TryGetComponent(out PlayerNetworkSync playerNetworkSync))
                {
                    if (playerNetworkSync.IsOwner)
                    {
                        doorObject.OutTarget(col.transform);
                    }
                }
            }
        }
    }
}
