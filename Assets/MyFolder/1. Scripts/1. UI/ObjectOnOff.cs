using UnityEngine;

namespace MyFolder._1._Scripts._1._UI
{
    public class ObjectOnOff : MonoBehaviour
    {
        public void ObjectOn()
        {
            gameObject.SetActive(true);   
        }

        public void ObjectOff()
        {
            
            gameObject.SetActive(false);
        }
    }
}