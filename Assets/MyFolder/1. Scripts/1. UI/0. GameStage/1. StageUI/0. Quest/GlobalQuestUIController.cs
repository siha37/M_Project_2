using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._6._GlobalQuest;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._0._Quest
{
	public sealed class GlobalQuestUIController : MonoBehaviour
	{
		[Header("Parents")]
		[SerializeField] private Transform container;

		[Header("Prefabs")]
		[SerializeField] private LineQuestPanel lineQuestPanelPrefab;

		private readonly Dictionary<int, QuestPanel> questIdToPanel = new();
		private readonly Dictionary<int, GlobalQuestReplicator> questIdToRep = new();


		private void OnEnable()
		{

			// 이미 씬에 존재하는 Replicator들 초기 동기화
			InitializeExistingReplicators();
		}

		private void OnDisable()
		{

			questIdToRep.Clear();

			foreach (var kv in questIdToPanel)
				if (kv.Value) Destroy(kv.Value.gameObject);
			questIdToPanel.Clear();

		}

		public void OnReplicatorSpawned(GlobalQuestReplicator rep)
		{
			int questId = rep.QuestId.Value;
			if (questIdToPanel.ContainsKey(questId))
				return;

			QuestPanel panel = CreatePanelFor(rep);
			if (!panel)
				return;

			panel.Initialize(rep);

			questIdToPanel[questId] = panel;
			questIdToRep[questId] = rep;
		}

		public void OnReplicatorDespawned(GlobalQuestReplicator rep)
		{
			int questId = rep.QuestId.Value;
			CleanupQuest(questId);
		}

		private void InitializeExistingReplicators()
		{
			var reps = FindObjectsOfType<GlobalQuestReplicator>();
			for (int i = 0; i < reps.Length; i++)
			{
				var rep = reps[i];
				if (!rep)
					continue;
				OnReplicatorSpawned(rep);
			}
		}

		/// <summary>
		/// Panel 생성
		/// </summary>
		/// <param name="rep"></param>
		/// <returns></returns>
		private QuestPanel CreatePanelFor(GlobalQuestReplicator rep)
		{
			QuestPanel prefab = SelectPanelPrefab(rep.QuestType.Value);
			if (!prefab)
				return null;
			return Instantiate(prefab, container ? container : transform);
		}

		private QuestPanel SelectPanelPrefab(GlobalQuestType type)
		{
			return lineQuestPanelPrefab;
		}


		private void CleanupQuest(int questId)
		{
			if (questIdToRep.TryGetValue(questId, out var rep))
			{
				questIdToRep.Remove(questId);
			}

			if (questIdToPanel.TryGetValue(questId, out var panel))
			{
				if (panel) Destroy(panel.gameObject);
				questIdToPanel.Remove(questId);
			}
		}


	}
}


