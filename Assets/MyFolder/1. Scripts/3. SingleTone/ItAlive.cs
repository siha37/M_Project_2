using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class ItAlive : MonoBehaviour
    {
        void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}
