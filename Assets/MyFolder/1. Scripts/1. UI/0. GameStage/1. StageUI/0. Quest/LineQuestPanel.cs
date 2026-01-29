using MyFolder._1._Scripts._6._GlobalQuest;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest
{
    public class LineQuestPanel : QuestPanel
    {
        [SerializeField] private TextMeshProUGUI progressText;

        public override void ProgressUpdate()
        {
            progressText.text = $"{(progress / target)*100}%";
            base.ProgressUpdate();
        }
    }
}