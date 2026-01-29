using System;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._8._Time;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._3._Spawner
{
    public class SpawnerCounterUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI coutner;
        [SerializeField] private TextMeshProUGUI timer;
        private string MainTex = "";
        private void Start()
        {
            SpawnerManager.instance.spawnerSpawn = SpawnerCounter;
        }
        private void SpawnerCounter(int count)
        {
            coutner.text = MainTex+count;
        }

        private void LateUpdate()
        {
            float lateTime = SpawnerManager.instance.NowSpawnTime - TimeManager.instance.CurrentTime;
            int minutes = (int)(lateTime / 60);
            int seconds = (int)(lateTime % 60);
            timer.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }
}