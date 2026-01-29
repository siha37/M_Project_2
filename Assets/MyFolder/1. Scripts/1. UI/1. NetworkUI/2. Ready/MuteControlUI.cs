using System;
using MyFolder._1._Scripts._9._Vivox;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class MuteControlUI : MonoBehaviour
    {
    
        [SerializeField] private Button InputDeviceMuteButton;
        [SerializeField] private Button OutputDeviceMuteButton;
        [SerializeField] private Image InputDeviceMuteImage;
        [SerializeField] private Image OutputDeviceMuteImage;
        [SerializeField] private Sprite InputDeviceMuteSprite;
        [SerializeField] private Sprite InputDeviceUnMuteSprite;
        [SerializeField] private Sprite OutputDeviceMuteSprite;
        [SerializeField] private Sprite OutputDeviceUnMuteSprite;


        private void Start()
        {
            InputDeviceMuteButton.onClick.AddListener(InputDeviceMute);
            OutputDeviceMuteButton.onClick.AddListener(OutputDeviceMute);
        }

        public void InputDeviceMute()
        {
            VivoxManager.Instance.MyInputMute();
            if (VivoxService.Instance.IsInputDeviceMuted)
            {
                InputDeviceMuteImage.sprite = InputDeviceMuteSprite;
            }
            else
            {
                InputDeviceMuteImage.sprite = InputDeviceUnMuteSprite;
            }
        }

        public void OutputDeviceMute()
        {
            VivoxManager.Instance.MyOutputMute();
            if (VivoxService.Instance.IsOutputDeviceMuted)
            {
                OutputDeviceMuteImage.sprite = OutputDeviceMuteSprite;
            }
            else
            {
                OutputDeviceMuteImage.sprite = OutputDeviceUnMuteSprite;
            }
        }
    }
}
