using System;
using System.Collections.Generic;
using FishNet;
using MyFolder._1._Scripts._0._Object._0._Agent._0._Player;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Main;
using MyFolder._1._Scripts._0._Object._0._Agent._1._Enemy.Status;
using MyFolder._1._Scripts._0._Object._6._DynamicObject;
using UnityEngine;

namespace MyFolder._1._Scripts._6._GlobalQuest
{
    public class QuestPoint : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D Areasize;
        [SerializeField] private List<Transform> subPoints;
        [SerializeField] private List<DoorObject> door;
        
        private List<PlayerNetworkSync> players = new List<PlayerNetworkSync>();
        private List<EnemyControll> enemys = new List<EnemyControll>();
        
        public Vector3 Point => transform.position;
        public Vector2 Size => Areasize.size;
        
        public List<Transform> SubPoints => subPoints;

        /// <summary>
        /// BoxCollider2D 영역 내부 임의 좌표(월드 좌표) 반환
        /// 회전/스케일을 고려하여 로컬에서 샘플 후 TransformPoint로 변환
        /// </summary>
        public Vector3 GetRandomPointInside()
        {
            if (!Areasize)
                return transform.position;

            Vector2 half = Areasize.size * (0.95f * 0.5f);
            Vector2 local = new Vector2(
                UnityEngine.Random.Range(-half.x, half.x),
                UnityEngine.Random.Range(-half.y, half.y)
            );
            Vector3 world = Areasize.transform.TransformPoint((Areasize.offset + local));
            world.z = transform.position.z;
            return world;
        }

        public void QuestActive(int questId)
        {
            foreach (PlayerNetworkSync player in players)
            {
                player.OnQuestingStarted(questId);
            }

            for (int i = enemys.Count - 1; i >= 0; i--)
            {
                enemys[i].SetQuestMeta(true,questId,GlobalQuestType.Extermination,null);   
            }
            enemys.Clear();
            
            foreach (DoorObject doorObject in door)
            {
                doorObject.DoorClose();
            }
        }

        public void QuestEnd()
        {
            foreach (PlayerNetworkSync player in players)
            {
                player.OnQuestingFinished();
            }
            foreach (DoorObject doorObject in door)
            {
                doorObject.DoorOpen();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (InstanceFinder.ServerManager)
            {
                if (other.CompareTag("Player"))
                {
                    if (other.TryGetComponent(out PlayerNetworkSync playerController))
                    {
                        if (!players.Contains(playerController))
                            players.Add(playerController);
                    }
                }
                else if (other.CompareTag("Enemy"))
                {
                    if (other.TryGetComponent(out EnemyControll enemy))
                    {
                        if (!enemys.Contains(enemy))
                            enemys.Add(enemy);
                    }
                    else if (other.TryGetComponent(out PlayerNetworkSync playerController))
                    {
                        if (!players.Contains(playerController))
                            players.Add(playerController);
                    }
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (InstanceFinder.ServerManager)
            {
                if (other.CompareTag("Player"))
                {
                    if (other.TryGetComponent(out PlayerNetworkSync playerController))
                    {
                        if (players.Contains(playerController))
                            players.Remove(playerController);
                    }
                }
                else if (other.CompareTag("Enemy"))
                {
                    if (other.TryGetComponent(out EnemyControll enemy))
                    {
                        if (enemys.Contains(enemy))
                            enemys.Remove(enemy);
                    }
                    else if (other.TryGetComponent(out PlayerNetworkSync playerController))
                    {
                        if (players.Contains(playerController))
                            players.Remove(playerController);
                    }
                }
            }
        }
    }
}