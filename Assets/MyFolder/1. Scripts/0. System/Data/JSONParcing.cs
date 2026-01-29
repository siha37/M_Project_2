using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace MyFolder._1._Scripts._0._System.Data
{
    public static class JsonParcing
    {
        public static T ReaderByResources<T> (string path)
        {
            TextAsset textasset =Resources.Load<TextAsset>(path);
            if(textasset)
            {
                //Debug.Log(textasset.text);
                return JsonUtility.FromJson<T>(textasset.text);
            }
            else
            {
                Debug.Log(path+":null");
            }
            return default(T);
        }
        public static T Reader<T> (TextAsset textasset)
        {
            if(textasset)
            {
                //Debug.Log(textasset.text);
                return JsonUtility.FromJson<T>(textasset.text);
            }
            else
            {
                Debug.LogError($"textasset is null {textasset}");
            }
            return default(T);
        }
        public static List<T> ReaderArrayByResources<T> (string path)
        {
            TextAsset textasset = Resources.Load<TextAsset>(path);
            return ReaderArray<T>(textasset);
        }
        public static List<T> ReaderArray<T>(TextAsset textasset)
        {
            DataArray<T> dd = JsonConvert.DeserializeObject<DataArray<T>>(textasset.text);
            return dd.data;
        }
        public static string Write(object obj)
        {
            string data = JsonConvert.SerializeObject(obj);
            string beautifiedJson = JValue.Parse(data).ToString(Formatting.Indented);
            return beautifiedJson;
        }
        
        public static string WriteArray<T>(List<T> data)
        {
            DataArray<T> dataArray = new DataArray<T>();
            dataArray.data = data;
            return Write(dataArray);
        }
        
        
        [System.Serializable]
        private class DataArray<T>
        {
            [FormerlySerializedAs("Data")] public List<T> data;
        }
    }
}

