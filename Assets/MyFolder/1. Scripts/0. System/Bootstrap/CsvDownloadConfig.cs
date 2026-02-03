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
            public string fileName;      // 옵션 사항 / 기본 <key>.json
            [Min(1)] public int headerLineIndex = 1; // 데이터 시작 번호
        }

        public List<Entry> entries = new();
    }
}