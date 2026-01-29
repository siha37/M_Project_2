using UnityEngine;

namespace MyFolder._1._Scripts._3._SingleTone
{
    public class DieAbleSingleTon<T> :MonoBehaviour where T : MonoBehaviour
    {
        private static T instance;
        public static T Instance
        {
            get
            {
                if (!instance)
                {
                    instance = FindFirstObjectByType<T>();

                    if (!instance)
                    {
                        GameObject obj = new GameObject();
                        obj.name = typeof(T).Name;
                        instance = obj.AddComponent<T>();
                    }
                }
                return instance;
            }
        }

        protected virtual void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
            }
        }
    }
}
