using MyFolder._1._Scripts._3._SingleTone;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._1._StageUI._2._EnemyLevel
{
    public class EnemyLevelUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI levelText;
        private void Start()
        {
            SpawnerManager.instance.Enemylevel_Changed += LevelUpdate;
        }

        private void LevelUpdate()
        {
            levelText.text = SpawnerManager.instance.EnemyLevel.ToString();
        }
    }
}
