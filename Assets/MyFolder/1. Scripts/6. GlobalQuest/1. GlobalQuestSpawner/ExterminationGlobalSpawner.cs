using MyFolder._1._Scripts._3._SingleTone;

namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    public class ExterminationGlobalSpawner : QuestSpawner
    {
        protected override int createAmount => 0;
        public override void SpawnStart()
        {
            LogManager.Log(LogCategory.Quest,"Disactive ExterminationGlobalSpawner");
        }
    }
}