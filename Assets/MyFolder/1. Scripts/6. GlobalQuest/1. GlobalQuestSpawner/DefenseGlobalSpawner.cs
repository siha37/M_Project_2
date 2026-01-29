namespace MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner
{
    public class DefenseGlobalSpawner : QuestSpawner
    {
        protected override int createAmount => 1;
        public override void SpawnStart()
        {
            AllSpawn();
        }
    }
}