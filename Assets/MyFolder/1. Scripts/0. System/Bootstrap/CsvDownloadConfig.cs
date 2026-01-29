using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyFolder._1._Scripts._0._System.Bootstrap
{
    [CreateAssetMenu(menuName = "Data/Csv Download Config", fileName = "CsvDownloadConfig")]
    public class CsvDownloadConfig : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string key;           // GameDataManager Addressables key
            public string url;           // Google Sheet CSV public link
            public string fileName;      // Optional override; default <key>.json
            [Min(1)] public int headerLineIndex = 1; // 1-based header line index
        }

        public List<Entry> entries = new();
    }
}