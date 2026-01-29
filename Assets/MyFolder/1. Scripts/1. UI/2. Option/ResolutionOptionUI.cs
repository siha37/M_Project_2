using System;
using System.Collections.Generic;
using MyFolder._1._Scripts._0._System.Option;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._2._Option
{
    public class ResolutionOptionUI : MonoBehaviour
    {
        [SerializeField] Toggle onFullScreenToggle;
        [SerializeField] TMP_Dropdown resolutionDropdown;

        private void Start()
        {
            onFullScreenToggle.isOn = OptionManager.Instance.OnFullScreen;
            DropDownListResolution();
            
            onFullScreenToggle.onValueChanged.AddListener(OptionManager.Instance.FullScreenChanged);
            resolutionDropdown.onValueChanged.AddListener(OptionManager_ResolutionsChanged);
        }

        private void DropDownListResolution()
        {
            Dictionary<string, Vector2> resoutions = OptionManager.Instance.ScreenResolutions;
            foreach (KeyValuePair<string, Vector2> resolution in resoutions)
            {
                resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(resolution.Key));
            }
        }

        private void OptionManager_ResolutionsChanged(int id)
        {
            OptionManager.Instance.ScreenResolutionsChanged(resolutionDropdown.options[id].text);
        }
    }
}