using System;
using FishNet.Object;
using MyFolder._1._Scripts._6._GlobalQuest._1._GlobalQuestSpawner;

namespace MyFolder._1._Scripts._0._Object._3._QuestAgent
{
    public class QuestAgentControll : NetworkBehaviour
    {
		private QuestSpawner myMother;

		public void SetMyMother(QuestSpawner myMother)
		{
			this.myMother = myMother;
		}

		public void OnDestroy()
		{
			myMother?.DestroyObject(NetworkObject);
		}
    }
}