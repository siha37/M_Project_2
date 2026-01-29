using System.Collections.ObjectModel;
using MyFolder._1._Scripts._9._Vivox;
using TMPro;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class VivoxAudioOptionUI : MonoBehaviour
    {
        [Header("Vivox Options")]
        [SerializeField] private TMP_Dropdown inputDeviceDropdown;
        [SerializeField] private TMP_Dropdown outputDeviceDropdown;
        
        [SerializeField] private Slider inputDeviceSlider;
        [SerializeField] private Slider outputDeviceSlider;

        [Header("Audio Quality Settings")]
        [SerializeField] private Slider micSensitivitySlider;     // 마이크 민감도 (0-100)
        [SerializeField] private Slider noiseGateSlider;          // 잡음 차단 강도 (0-100)

        #region Unity Methods

        private void Start()
        {
            CallbackAdd();
        }

        private void OnEnable()
        {
            GetAudioDeivce();
        }


        private void OnDisable()
        {
            CallbackRemove();
        }

        private void OnDestroy()
        {
            CallbackRemove();
        }
        
        private void CallbackAdd()
        {   
            VivoxManager.Instance.InputDeviceChanged +=GetAudioDeivce;
            VivoxManager.Instance.OutputDeviceChanged +=GetAudioDeivce;

            inputDeviceDropdown.onValueChanged.AddListener(SetInputDevice);
            outputDeviceDropdown.onValueChanged.AddListener(SetOutputDevice);
            
            inputDeviceSlider.onValueChanged.AddListener(InputVolumeChanged);
            outputDeviceSlider.onValueChanged.AddListener(OutputVolumeChanged);
            
            
            micSensitivitySlider.onValueChanged.AddListener(OnMicSensitivityChanged);
            noiseGateSlider.onValueChanged.AddListener(OnNoiseGateChanged);
        }

        private void CallbackRemove()
        {
            VivoxManager.Instance.InputDeviceChanged -=GetAudioDeivce;
            VivoxManager.Instance.OutputDeviceChanged -=GetAudioDeivce;
        }
        #endregion

        #region DeviceInfo

        private void GetAudioDeivce()
        {
            GetInputDeivce();
            GetOutputDeivce();
        }

        private void GetInputDeivce()
        {
            ReadOnlyCollection<VivoxInputDevice> dlist = VivoxManager.Instance.GetAvailableInputDevices();
            inputDeviceDropdown.options.Clear();

            // 현재 활성화된 디바이스 가져오기
            VivoxInputDevice activeDevice = VivoxService.Instance.ActiveInputDevice;
            string activeDeviceName = activeDevice != null ? activeDevice.DeviceName : null;

            int i = 0;
            int selectedIndex = 0;
            bool foundActiveDevice = false;

            foreach (VivoxInputDevice device in dlist)
            {
                inputDeviceDropdown.options.Add(new TMP_Dropdown.OptionData(device.DeviceName));

                // 현재 활성화된 디바이스와 일치하면 선택
                if (activeDeviceName != null && device.DeviceName == activeDeviceName)
                {
                    selectedIndex = i;
                    foundActiveDevice = true;
                }
                // 활성화된 디바이스를 찾지 못했고 Default System Device를 찾으면 선택 (폴백)
                else if (!foundActiveDevice && device.DeviceID == "Default System Device")
                {
                    selectedIndex = i;
                }

                i++;
            }

            inputDeviceDropdown.value = selectedIndex;
        }

        private void GetOutputDeivce()
        {
            ReadOnlyCollection<VivoxOutputDevice> dlist = VivoxManager.Instance.GetAvailableOutputDevices();
            outputDeviceDropdown.options.Clear();

            // 현재 활성화된 디바이스 가져오기
            VivoxOutputDevice activeDevice = VivoxService.Instance.ActiveOutputDevice;
            string activeDeviceName = activeDevice != null ? activeDevice.DeviceName : null;

            int i = 0;
            int selectedIndex = 0;
            bool foundActiveDevice = false;

            foreach (VivoxOutputDevice device in dlist)
            {
                outputDeviceDropdown.options.Add(new TMP_Dropdown.OptionData(device.DeviceName));

                // 현재 활성화된 디바이스와 일치하면 선택
                if (activeDeviceName != null && device.DeviceName == activeDeviceName)
                {
                    selectedIndex = i;
                    foundActiveDevice = true;
                }
                // 활성화된 디바이스를 찾지 못했고 Default System Device를 찾으면 선택 (폴백)
                else if (!foundActiveDevice && device.DeviceID == "Default System Device")
                {
                    selectedIndex = i;
                }

                i++;
            }

            outputDeviceDropdown.value = selectedIndex;
        }

        #endregion

        #region DeviceSelecte

        private void SetInputDevice(int index)
        {
            string deviceID = inputDeviceDropdown.options[index].text;
            VivoxInputDevice device= VivoxManager.Instance.FindNameInputDevice(deviceID);
            VivoxManager.Instance.SetVivoxInputDeviceAsync(device);
        }

        private void SetOutputDevice(int index)
        {
            string deviceID = outputDeviceDropdown.options[index].text;
            VivoxOutputDevice device= VivoxManager.Instance.FindNameOutputDevice(deviceID);
            VivoxManager.Instance.SetVivoxOutputDeviceAsync(device);
        }

        #endregion

        #region VolumeControl

        private void InputVolumeChanged(float value)
        {
            VivoxManager.Instance.MyInputVolumeChange((int)inputDeviceSlider.value);
        }

        private void OutputVolumeChanged(float value)
        {
            VivoxManager.Instance.MyOutputVolumeChange((int)outputDeviceSlider.value);
        }

        #endregion

        
        #region Audio Quality Control

        private void OnMicSensitivityChanged(float value)
        {
            ApplyAudioQualitySettings();
        }

        private void OnNoiseGateChanged(float value)
        {
            ApplyAudioQualitySettings();
        }

        private void ApplyAudioQualitySettings()
        {
            // 유저 값 (0-100) → VAD 파라미터 변환
            
            // 마이크 민감도: 0(둔감) ~ 100(민감) → VAD sensitivity: 100 ~ 0
            float userSensitivity = micSensitivitySlider != null ? micSensitivitySlider.value : 57f;
            int vadSensitivity = 100 - (int)userSensitivity;
            
            // 잡음 차단 강도: 0(약함) ~ 100(강함) → VAD noiseFloor: 200 ~ 1500
            float userNoiseGate = noiseGateSlider != null ? noiseGateSlider.value : 29f;
            int vadNoiseFloor = 200 + (int)(userNoiseGate * 13);
            
            // hangoverTime은 고정 (1500ms)
            int vadHangoverTime = 1500;
            
            // VivoxManager에 적용
            VivoxManager.Instance.SetVADPropertiesAsync(vadHangoverTime, vadSensitivity, vadNoiseFloor);
        }

        #endregion
    }
}
