using System;
using System.Collections;
using System.Collections.Generic;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._7._PlayerRole;
using MyFolder._1._Scripts._8999._Utility.Corutin;
using TMPro;
using UnityEngine;

namespace MyFolder._1._Scripts._1._UI._0._GameStage._4._MainTarget
{
    public class MainTargetUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI maintext;
        [SerializeField] private List<string> texts;
        private float delay = 10 ,currentTime = 0;
        private int index = 0;
        private void Start()
        {
            PlayerSettingManager.PlayerSettings setting = PlayerSettingManager.Instance.GetLocalPlayerSettings();
            if (setting.role == PlayerRoleType.Destroyer)
            {
                texts.Add("모든 시민을 없애세요");
                texts.Add("모든 시민을 없애세요");
                maintext.text = texts[index];
            }
            else
            {
                texts.Add("모든 스포너를 파괴하시오");
                texts.Add("모든 제거자를 전멸시키시오");
                texts.Add("20분을 버티시오");
                maintext.text = texts[index];
            }
        }

        private void Update()
        {
            if (currentTime < delay)
            {
                currentTime += Time.deltaTime;
            }
            else
            {
                currentTime = 0;
                StartCoroutine(nameof(FadeInOut));
            }
        }

        private IEnumerator FadeInOut()
        {
            float alpha = 1;
            while (alpha > 0)
            {
                alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime * 10f);
                yield return WaitForSecondsCache.Get(0.01f);
                maintext.color = new Color(1, 1, 1, alpha);
            }

            index++;
            index = index % texts.Count;
            maintext.text = texts[index];
            
            while (alpha < 1)
            {
                alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime * 10f);
                yield return WaitForSecondsCache.Get(0.01f);
                maintext.color = new Color(1, 1, 1, alpha);
            }
        }
    }
}
