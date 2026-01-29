
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MyFolder._1._Scripts._3._SingleTone;
using MyFolder._1._Scripts._4._Network;
using Steamworks;
using Unity.Services.Vivox;
using UnityEngine;

namespace MyFolder._1._Scripts._9._Vivox
{
    public enum AudioQualityPreset
    {
        HighQuality,      // 고품질, 대역폭 많이 사용
        Balanced,         // 균형잡힌 설정 (기본값)
        LowBandwidth,     // 저대역폭 모드
        NoiseReduction    // 잡음 제거 중점
    }
    public class VivoxManager : SingleTone<VivoxManager>
    {
        private string channelName;
        private readonly Channel3DProperties props = new Channel3DProperties(10, 3, 1.0f, AudioFadeModel.InverseByDistance);
        private readonly float _PosUpdateDelay = 0.3f;
        private float _nextPosUpdate;
        public bool _isChannelActive = false;
        
        private GameObject participantObject;
        
        private ReadOnlyCollection<VivoxInputDevice> inputDevices;
        private ReadOnlyCollection<VivoxOutputDevice> outputDevices;
        
        private Dictionary<string,List<RosterItem>> rostersObjects = new();

        public Action<RosterItem> RosterAdded;
        public Action<RosterItem> RosterRemoved;
        public Action InputDeviceChanged;
        public Action OutputDeviceChanged;
        

        
        #region Unity Methods
        private void Update()
        {
            if (_isChannelActive && participantObject)
            {
                if (Time.time >= _nextPosUpdate)
                {
                    Set3DPositionAsync(participantObject);
                    _nextPosUpdate = Time.time + _PosUpdateDelay;
                }   
            }
        }
        
        private async void OnApplicationQuit()
        {
            if (VivoxService.Instance != null)
            {
                try
                {
                    if (_isChannelActive && !string.IsNullOrEmpty(channelName))
                    {
                        await VivoxService.Instance.LeaveChannelAsync(channelName);
                    }
            
                    if (VivoxService.Instance.IsLoggedIn)
                    {
                        await VivoxService.Instance.LogoutAsync();
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Error during Vivox cleanup: {ex.Message}");
                }
            }
        }
        private async void OnDestroy()
        {
            if (VivoxService.Instance != null)
            {
                try
                {
                    if (_isChannelActive && !string.IsNullOrEmpty(channelName))
                    {
                        await VivoxService.Instance.LeaveChannelAsync(channelName);
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"Error during Vivox cleanup: {ex.Message}");
                }
            }
        }
        #endregion
        #region LogIn

        
        //Vivox 로그인
        public async void LoginToVivoxAsync()
        {
            LoginOptions options = new LoginOptions();
            if (SteamManager.Initialized)
                options.DisplayName = SteamFriends.GetPersonaName();
            else
                options.DisplayName = NetworkStateManager.Instance.CurrentUserId;
            
            options.EnableTTS = false;
            await VivoxService.Instance.LoginAsync(options);
            
            CallBackAdded();
        }


        //로그아웃 ( 게임 종료 시 처리 )
        public async void LogoutOfVivoxAsync()
        {
            await VivoxService.Instance.LogoutAsync();
        }
        
        public void CallBackAdded()
        {
            VivoxService.Instance.ParticipantAddedToChannel += ParticipantAddedToChannel;
            VivoxService.Instance.ParticipantRemovedFromChannel += ParticipantRemovedFromChannel;
            
            VivoxService.Instance.AvailableInputDevicesChanged += UpdateAvailableInputDevices;
            VivoxService.Instance.AvailableOutputDevicesChanged += UpdateAvailableOutputDevices;
            
            //VivoxService.Instance.EffectiveInputDeviceChanged += SetDefaultVivoxInputDeviceAsync;
            //VivoxService.Instance.EffectiveOutputDeviceChanged += SetDefaultVivoxOutputDeviceAsync;
            
        }
        
        #endregion

        #region Channel Controll

        public void JoinPositionalChannel(string roomname)
        {
            JoinPositionalChannelAsync(roomname);
        }

        //포지셔널 채널 입장
        public async void JoinPositionalChannelAsync(string roomName)
        {
            if (!VivoxService.Instance.IsLoggedIn)
            {
                LogManager.LogWarning(LogCategory.Vivox, "Vivox에 로그인되지 않았습니다.");
                return;
            }

            try
            {
                // 이미 같은 채널에 조인되어 있는지 확인
                if (_isChannelActive && channelName == roomName)
                {
                    LogManager.LogWarning(LogCategory.Vivox, $"이미 채널 '{roomName}'에 조인되어 있습니다.");
                    return;
                }

                // 활성화된 채널이 있으면 먼저 떠나기
                if (_isChannelActive && !string.IsNullOrEmpty(channelName))
                {
                    LogManager.Log(LogCategory.Vivox, $"기존 채널 '{channelName}'에서 나갑니다.");
                    await VivoxService.Instance.LeaveChannelAsync(channelName);
                    _isChannelActive = false;
                }

                // Vivox ActiveChannels에서도 중복 확인
                if (VivoxService.Instance.ActiveChannels.Any(ch => ch.Key == roomName))
                {
                    LogManager.LogWarning(LogCategory.Vivox, $"채널 '{roomName}'이 여전히 활성화되어 있습니다. 정리를 시도합니다.");
                    await VivoxService.Instance.LeaveChannelAsync(roomName);
                }

                channelName = roomName;

                // 포지셔널 채널만 조인 (Echo 채널 제거)
                await VivoxService.Instance.JoinPositionalChannelAsync(roomName, ChatCapability.AudioOnly, props);
                _isChannelActive = true;

                ApplyAudioQualityPresetAsync(AudioQualityPreset.NoiseReduction);
                
                LogManager.Log(LogCategory.Vivox, $"채널 '{roomName}'에 성공적으로 조인했습니다.");
            }
            catch (Exception ex)
            {
                _isChannelActive = false;
                LogManager.LogError(LogCategory.Vivox, $"채널 조인 실패: {ex.Message}");
            }
        }
        
        // 음성 채널 떠나기
        public async void LeaveEchoChannelAsync()
        {
            if (!_isChannelActive || VivoxService.Instance == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(channelName))
                {
                    await VivoxService.Instance.LeaveChannelAsync(channelName);
                    LogManager.Log(LogCategory.Vivox, $"채널 '{channelName}'에서 나갔습니다.");
                }
            }
            catch (Exception ex)
            {
                LogManager.LogWarning(LogCategory.Vivox, $"채널 나가기 오류: {ex.Message}");
            }
            finally
            {
                _isChannelActive = false;
                channelName = null;
            }
        }

        //3D 오브젝트 설정
        public void SetParticipantObejct(GameObject participantObject)
        {
            this.participantObject = participantObject;
        }
        
        // 3D 위치 변경
        public void Set3DPositionAsync(GameObject participantObject)
        {
            if (!participantObject)
                return;
            VivoxService.Instance.Set3DPosition(participantObject,channelName);
        }

        #endregion

        #region Roster Control


        private void UpdateParticipantRoster(VivoxParticipant participant, bool isAddParticipant)
        {
            if (isAddParticipant)
            {   
                RosterItem newRosterItem = new RosterItem();
                List<RosterItem> newRosterItems = new List<RosterItem>();
                if (rostersObjects.ContainsKey(channelName))
                {
                    rostersObjects.TryGetValue(participant.ChannelName, out newRosterItems);
                    if(newRosterItems!=null)
                        newRosterItems.Add(newRosterItem);
                    newRosterItem.SetupRosterItem(participant);
                    rostersObjects[channelName] = newRosterItems;
                }
                else
                {
                    newRosterItems = new();
                    newRosterItems.Add(newRosterItem);
                    newRosterItem.SetupRosterItem(participant);
                    rostersObjects.Add(channelName, newRosterItems);
                }
                RosterAdded?.Invoke(newRosterItem);
            }
            else
            {
                if (channelName != null && rostersObjects != null && rostersObjects.ContainsKey(channelName))
                {
                    RosterItem removeItem = rostersObjects[channelName].FirstOrDefault(p => p.Participant.PlayerId == participant.PlayerId);
                    if (removeItem != null)
                    {
                        removeItem.RosterRemove();
                        rostersObjects[channelName].Remove(removeItem);
                        RosterRemoved?.Invoke(removeItem);
                    }
                    else
                    {
                        LogManager.LogWarning(LogCategory.Vivox, "No Roster item found for channel " + channelName);
                    }
                }
            }
        }

        private void ParticipantAddedToChannel(VivoxParticipant participant)
        {
            UpdateParticipantRoster(participant, true);
        }

        private void ParticipantRemovedFromChannel(VivoxParticipant participant)
        {
            UpdateParticipantRoster(participant, false);
        }
        
        //  Mute, Speach는 RosterItem에서 처리됨

        public List<RosterItem> GetRostersAsync()
        {
            if (rostersObjects.Count != 0 && rostersObjects.ContainsKey(channelName))
                return rostersObjects[channelName];
            else
                return null;
        }
        
        #endregion
        
        #region My Volume Controll
        // -10~25
        public void MyInputVolumeChange(int volumeLevel)
        {
            VivoxService.Instance.SetInputDeviceVolume(volumeLevel);
        }

        public void MyOutputVolumeChange(int volumeLevel)
        {
            VivoxService.Instance.SetOutputDeviceVolume(volumeLevel);
        }

        public void MyInputMute()
        {
            if(!VivoxService.Instance.IsInputDeviceMuted)
                VivoxService.Instance.MuteInputDevice();
            else
                VivoxService.Instance.UnmuteInputDevice();
        }

        public void MyOutputMute()
        {
            if(!VivoxService.Instance.IsOutputDeviceMuted)
                VivoxService.Instance.MuteOutputDevice();
            else
                VivoxService.Instance.UnmuteOutputDevice();
        }

        #endregion

        #region Audio Devices

        // 장치 업데이트
        private void UpdateAvailableInputDevices()
        {
            inputDevices = VivoxService.Instance.AvailableInputDevices;
            InputDeviceChanged?.Invoke();
        }
        
        
        //장치 업데이트
        private void UpdateAvailableOutputDevices()
        {
            outputDevices = VivoxService.Instance.AvailableOutputDevices;
            OutputDeviceChanged?.Invoke();
        }
        
        // 장치 정보 반환
        public ReadOnlyCollection<VivoxInputDevice> GetAvailableInputDevices()
        {
            UpdateAvailableInputDevices();
            return inputDevices;
        }

        // 장치 정보 반환
        public ReadOnlyCollection<VivoxOutputDevice> GetAvailableOutputDevices()
        {
            UpdateAvailableOutputDevices();
            return outputDevices;
        }

        public VivoxInputDevice FindNameInputDevice(string dname)
        {
            return inputDevices.FirstOrDefault(p=> p.DeviceName == dname);
        }
        public VivoxOutputDevice FindNameOutputDevice(string dname)
        {
            return outputDevices.FirstOrDefault(p=> p.DeviceName == dname);
        }

        // 장치 설정
        public async void SetVivoxInputDeviceAsync(VivoxInputDevice vivoxInputDevice)
        {
            if (inputDevices.Contains(vivoxInputDevice))
            {
                await VivoxService.Instance.SetActiveInputDeviceAsync(vivoxInputDevice);
            }
        }

        // 장치 설정
        public async void SetVivoxOutputDeviceAsync(VivoxOutputDevice vivoxOutputDevice)
        {
            if (outputDevices.Contains(vivoxOutputDevice))
            {
                await VivoxService.Instance.SetActiveOutputDeviceAsync(vivoxOutputDevice);
            }
        }

        
        #endregion

        #region Audio Quality
        
        /// <summary>
        /// VAD (Voice Activity Detection) 설정 - 잡음 제거에 효과적
        /// </summary>
        /// <param name="hangoverTime">음성 종료 후 전송 유지 시간 (밀리초, 기본 2000ms)</param>
        /// <param name="sensitivity">민감도 0-100 (값이 높을수록 덜 민감함, 기본 43)</param>
        /// <param name="noiseFloor">노이즈 임계값 0-20000 (높을수록 시끄러운 환경에 적합, 기본 576)</param>
        public async void SetVADPropertiesAsync(int hangoverTime = 2000, int sensitivity = 43, int noiseFloor = 576)
        {
            // Auto VAD를 먼저 비활성화해야 수동 설정이 적용됨
            await VivoxService.Instance.DisableAutoVoiceActivityDetectionAsync();
    
            // VAD 속성 설정
            await VivoxService.Instance.SetVoiceActivityDetectionPropertiesAsync(hangoverTime, noiseFloor, sensitivity);
    
            LogManager.Log(LogCategory.Vivox, $"VAD 설정 완료 - Hangover: {hangoverTime}ms, Sensitivity: {sensitivity}, NoiseFloor: {noiseFloor}");
        }

        
        /// <summary>
        /// 채널 전송 모드 설정 (대역폭 제한)
        /// </summary>
        /// <param name="mode">
        /// None: 전송 안함
        /// Single: 단일 채널만 전송 (대역폭 절약)
        /// All: 모든 채널에 전송
        /// </param>
        public async void SetChannelTransmissionModeAsync(TransmissionMode mode = TransmissionMode.Single)
        {
            await VivoxService.Instance.SetChannelTransmissionModeAsync(mode, channelName);
            LogManager.Log(LogCategory.Vivox, $"전송 모드 설정: {mode}");
        }
        
        
        /// <summary>
        /// 종합 오디오 품질 프리셋 적용
        /// </summary>
        public async void ApplyAudioQualityPresetAsync(AudioQualityPreset preset)
        {
            switch (preset)
            {
                case AudioQualityPreset.HighQuality:
                    // 고품질 - 대역폭 많이 사용, 민감한 음성 감지
                    SetChannelTransmissionModeAsync(TransmissionMode.Single);
                    SetVADPropertiesAsync(hangoverTime: 1000, sensitivity: 30, noiseFloor: 300);
                    break;
            
                case AudioQualityPreset.Balanced:
                    // 균형 - 기본 설정
                    SetChannelTransmissionModeAsync(TransmissionMode.Single);
                    SetVADPropertiesAsync(hangoverTime: 2000, sensitivity: 43, noiseFloor: 576);
                    break;
            
                case AudioQualityPreset.LowBandwidth:
                    // 저대역폭 - 대역폭 절약, 명확한 음성만 전송
                    SetChannelTransmissionModeAsync(TransmissionMode.Single);
                    SetVADPropertiesAsync(hangoverTime: 1500, sensitivity: 60, noiseFloor: 1000);
                    break;
            
                case AudioQualityPreset.NoiseReduction:
                    // 잡음 제거 중점 - 시끄러운 환경
                    SetChannelTransmissionModeAsync(TransmissionMode.Single);
                    SetVADPropertiesAsync(hangoverTime: 1500, sensitivity: 70, noiseFloor: 1500);
                    break;
            }
    
            LogManager.Log(LogCategory.Vivox, $"오디오 품질 프리셋 적용: {preset}");
        }
        #endregion
        
     

    }
}