namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    public class SurvivalGlobalSpawner : QuestSpawner
    {
        protected override int createAmount => 6;
        public override void SpawnStart()
        {
            AllSpawn();
        }
    }
}