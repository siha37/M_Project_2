using System.Collections.Generic;
using FMOD.Studio;
using MyFolder._1._Scripts._3._SingleTone;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace MyFolder._1._Scripts._0._System.Option
{
    // 네트워크 옵션 제외 로컬 옵션 관리자
    public class OptionManager : SingleTone<OptionManager>
    {
        
        #region Volume Variables
        private float busVolume_Master;
        
        private float busVolume_Ambience;
        
        private float busVolume_Music;
        
        private float busVolume_SFX;
        
        private float busVolume_UI;
        
        
        public float BusVolume_Master => busVolume_Master;
        public float BusVolume_Ambience => busVolume_Ambience;
        public float BusVolume_Music => busVolume_Music;
        public float BusVolume_SFX => busVolume_SFX;
        public float BusVolume_UI => busVolume_UI;
        
        Bus Master;
        Bus Ambience;
        Bus Music;
        Bus SFX;
        Bus UI;
        #endregion
        
        #region Screen Variables
        
        private Dictionary<string, Vector2> screenResolutions = new Dictionary<string, Vector2>()
        {
            { "1280x720(16:9)",   new Vector2(1280, 720) },
            { "1280x800(16:10)",  new Vector2(1280, 800) },
            { "1024x768(4:3)",    new Vector2(1024, 768) },
            { "1280x1024(5:4)",   new Vector2(1280, 1024) },

            { "1920x1080(16:9)",  new Vector2(1920, 1080) },
            { "1920x1200(16:10)", new Vector2(1920, 1200) },
            { "2560x1440(16:9)",  new Vector2(2560, 1440) },
            { "2560x1600(16:10)", new Vector2(2560, 1600) },

            { "2560x1080(21:9)",  new Vector2(2560, 1080) },
            { "3440x1440(21:9)",  new Vector2(3440, 1440) },

            { "3840x2160(16:9)",  new Vector2(3840, 2160) },
            { "5120x2160(21:9)",  new Vector2(5120, 2160) },
        };

        private bool onFullScreen = true;
        private string CurrentyScreen;
        
        public Dictionary<string, Vector2> ScreenResolutions => screenResolutions;
        public bool OnFullScreen => onFullScreen;
        
        #endregion
        protected override void Awake()
        {
            base.Awake();
            
            //DataLoad
            LoadAllOptionsData();
        }
        
        private void Start()
        {
            GetBus();
            
            //Apply
            AllApplyOptions();
        }

        private void GetBus()
        {
            Master = FMODUnity.RuntimeManager.GetBus("bus:/");
            Ambience = FMODUnity.RuntimeManager.GetBus("bus:/Ambience");
            Music = FMODUnity.RuntimeManager.GetBus("bus:/Music");
            SFX = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
            UI = FMODUnity.RuntimeManager.GetBus("bus:/UI");   
        }

        #region SAVE/LOAD

        private void SaveAllOptionsData()
        {
            Save_MasterVolume();
            Save_AmbienceVolume();
            Save_MusicVolume();
            Save_SFXVolume();
            Save_UIVolume();
            
            Save_ScreenResolutions();
            Save_FullScreen();
        }
        
        private void Save_MasterVolume() { PlayerPrefs.SetFloat("busVolume_Master", busVolume_Master); }
        private void Save_AmbienceVolume() { PlayerPrefs.SetFloat("busVolume_Ambience", busVolume_Ambience); }
        private void Save_MusicVolume() {  PlayerPrefs.SetFloat("busVolume_Music", busVolume_Music); }
        private void Save_SFXVolume() { PlayerPrefs.SetFloat("busVolume_SFX", busVolume_SFX); }
        private void Save_UIVolume() { PlayerPrefs.SetFloat("busVolume_UI", busVolume_UI); }

        private void Save_ScreenResolutions() { PlayerPrefs.SetString("CurrentyScreen", CurrentyScreen); }
        private void Save_FullScreen() { PlayerPrefs.SetInt("OnFullScreen",OnFullScreen?1:0); }
        private void LoadAllOptionsData()
        {
            Load_MasterVolume();
            Load_AmbienceVolume();
            Load_MusicVolume();
            Load_SFXVolume();
            Load_UIVolume();

            Load_ScreenResolutions();
            Load_FullScreen();
        }

        private void Load_MasterVolume()
        {
            if (PlayerPrefs.HasKey(nameof(busVolume_Master)))
                busVolume_Master= PlayerPrefs.GetFloat(nameof(busVolume_Master), busVolume_Master);
            else
                busVolume_Master = 1;
        }

        private void Load_AmbienceVolume()
        {
            if (PlayerPrefs.HasKey("busVolume_Ambience"))
                busVolume_Ambience = PlayerPrefs.GetFloat("busVolume_Ambience", busVolume_Ambience);
            else
                busVolume_Ambience = 1;
        }

        private void Load_MusicVolume() 
        {
            if (PlayerPrefs.HasKey("busVolume_Music"))
                busVolume_Music = PlayerPrefs.GetFloat("busVolume_Music", busVolume_Music);
            else
                busVolume_Music = 1;
        }
        private void Load_SFXVolume() 
        {
            if (PlayerPrefs.HasKey("busVolume_SFX"))
                busVolume_SFX = PlayerPrefs.GetFloat("busVolume_SFX", busVolume_SFX);
            else
                busVolume_SFX = 1;
        }
        private void Load_UIVolume() 
        {
            if (PlayerPrefs.HasKey("busVolume_UI"))
                busVolume_UI = PlayerPrefs.GetFloat("busVolume_UI", busVolume_UI);
            else
                busVolume_UI = 1;
        }


        private void Load_ScreenResolutions()
        {
            if (PlayerPrefs.HasKey("CurrentyScreen"))
                CurrentyScreen = PlayerPrefs.GetString("CurrentyScreen", CurrentyScreen);
            else
                CurrentyScreen = null;
        }

        private void Load_FullScreen()
        {
            if(PlayerPrefs.HasKey("OnFullScreen"))
                onFullScreen = PlayerPrefs.GetInt("OnFullScreen", 0) == 1;
            else
                onFullScreen = true;
        }

        #endregion

        #region Apply

        private void AllApplyOptions()
        {
            MasterVolumeApply();
            AmbienceVolumeApply();
            MusicVolumeApply();
            SFXVolumeApply();
            UIVolumeApply();

            ScreenResolutionsApply();
        }

        private void MasterVolumeApply() { Master.setVolume(busVolume_Master); }
        private void AmbienceVolumeApply() { Ambience.setVolume(busVolume_Ambience);}
        private void MusicVolumeApply() { Music.setVolume(busVolume_Music); }
        private void SFXVolumeApply() { SFX.setVolume(busVolume_SFX);}
        private void UIVolumeApply() { UI.setVolume(busVolume_UI); }

        private void ScreenResolutionsApply()
        {
            Screen.fullScreen = true;
            Vector2 resolution = new Vector2(Screen.width, Screen.height);
            if(CurrentyScreen != null && screenResolutions.TryGetValue(CurrentyScreen, out var screenResolution))
                resolution = screenResolution;
            Screen.SetResolution((int)resolution.x,(int)resolution.y,OnFullScreen?FullScreenMode.FullScreenWindow:FullScreenMode.Windowed);
        }
        
        #endregion
        
        #region UPDATE
        
        public void MasterVolumeChanged(float volume)
        {
            busVolume_Master = volume;
            MasterVolumeApply();
            Save_MasterVolume();
        }

        public void AmbienceVolumeChanged(float volume)
        {
            busVolume_Ambience = volume;
            AmbienceVolumeApply();
            Save_AmbienceVolume();
        }
        public void MusicVolumeChanged(float volume)
        {
            busVolume_Music = volume;
            MusicVolumeApply();
            Save_MusicVolume();
        }
        public void SFXVolumeChanged(float volume)
        {
            busVolume_SFX = volume;
            SFXVolumeApply();
            Save_SFXVolume();
        }
        public void UIVolumeChanged(float volume)
        {
            busVolume_UI = volume;
            UIVolumeApply();
            Save_UIVolume();
        }

        public void ScreenResolutionsChanged(string screenResolution)
        {
            CurrentyScreen = screenResolution;
            ScreenResolutionsApply();
            Save_ScreenResolutions();
        }

        public void FullScreenChanged(bool fullScreen)
        {
            onFullScreen = fullScreen;
            ScreenResolutionsApply();
            Save_FullScreen();
        }


        #endregion

    }
}