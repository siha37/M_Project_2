using System;
using Newtonsoft.Json;

namespace MyFolder._1._Scripts._6._GlobalQuest
{
    [Serializable]
    public class GlobalQuestManagerData
    {
        private ushort TypeId;
        private string QuestCreateTime;

        [JsonConstructor]
        public GlobalQuestManagerData(
            [JsonProperty("TypeId")]ushort typeId,
            [JsonProperty("QuestCreateTime")]string questCreateTime)
        {
            TypeId = typeId;
            QuestCreateTime = questCreateTime;
        }
        
        public string questCreateTime => QuestCreateTime;
    }
}