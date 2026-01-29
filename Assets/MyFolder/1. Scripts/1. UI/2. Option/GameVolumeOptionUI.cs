using MyFolder._1._Scripts._0._System.Option;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._2._Option
{
    public class GameVolumeOptionUI : MonoBehaviour
    {
        [SerializeField] private Slider MasterVolumeSlider;
        [SerializeField] private Slider AmbienceVolumeSlider;
        [SerializeField] private Slider MusicVolumeSlider;
        [SerializeField] private Slider SFXVolumeSlider;
        [SerializeField] private Slider UIVolumeSlider;

        private void Start()
        {
            VolumeVauleLoad();
            
            MasterVolumeSlider.onValueChanged.AddListener(OptionManager.Instance.MasterVolumeChanged);
            AmbienceVolumeSlider.onValueChanged.AddListener(OptionManager.Instance.AmbienceVolumeChanged);
            MusicVolumeSlider.onValueChanged.AddListener(OptionManager.Instance.MusicVolumeChanged);
            SFXVolumeSlider.onValueChanged.AddListener(OptionManager.Instance.SFXVolumeChanged);
            UIVolumeSlider.onValueChanged.AddListener(OptionManager.Instance.UIVolumeChanged);
        }

        private void OnDestroy()
        {
            MasterVolumeSlider.onValueChanged.RemoveListener(OptionManager.Instance.MasterVolumeChanged);
            AmbienceVolumeSlider.onValueChanged.RemoveListener(OptionManager.Instance.AmbienceVolumeChanged);
            MusicVolumeSlider.onValueChanged.RemoveListener(OptionManager.Instance.MusicVolumeChanged);
            SFXVolumeSlider.onValueChanged.RemoveListener(OptionManager.Instance.SFXVolumeChanged);
            UIVolumeSlider.onValueChanged.RemoveListener(OptionManager.Instance.UIVolumeChanged);
        }

        private void VolumeVauleLoad()
        {
            if (OptionManager.Instance)
            {
                MasterVolumeSlider.value = OptionManager.Instance.BusVolume_Master;
                AmbienceVolumeSlider.value = OptionManager.Instance.BusVolume_Ambience;
                MusicVolumeSlider.value = OptionManager.Instance.BusVolume_Music;
                SFXVolumeSlider.value = OptionManager.Instance.BusVolume_SFX;
                UIVolumeSlider.value = OptionManager.Instance.BusVolume_UI;
            }
        }
    }
}