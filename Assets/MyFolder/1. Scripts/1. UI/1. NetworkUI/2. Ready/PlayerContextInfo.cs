using System;
using FMOD;
using MyFolder._1._Scripts._4._Network;
using MyFolder._1._Scripts._9._Vivox;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace MyFolder._1._Scripts._1._UI._1._NetworkUI._2._Ready
{
    public class PlayerContextInfo : MonoBehaviour
    {
        [SerializeField] private Image profileImge;
        [SerializeField] private TextMeshProUGUI profileName;
        [SerializeField] private Button exitButton;
        
        private RosterItem rosterItem;
        [SerializeField] private Sprite muteSprite;
        [SerializeField] private Sprite unmuteSprite;
        [SerializeField] private Color speakerColor;
        [SerializeField] private Toggle soundButton;
        [SerializeField] private Image soundImage;
        [SerializeField] private Slider volumeSlider;
        
        public int clientID;
        public string DisplayName;

        public PlayerContextInfo(Image soundImage)
        {
            this.soundImage = soundImage;
        }

        public void Start()
        {
            exitButton?.onClick.AddListener(OnKickPlayer);
            soundButton.onValueChanged.AddListener(SetRosterMute);
            volumeSlider.onValueChanged.AddListener(SetRosterVolume);
        }

        public void OnKickPlayer()
        {
            if (!GameNetworkManager.Instance.IsHost())
            {
                Debug.LogWarning("호스트만 플레이어를 추방할 수 있습니다.");
                return;
            }

            GameNetworkManager.Instance.KickClient(clientID);
        }

        public void SetOwner()
        {
            exitButton?.gameObject.SetActive(false);
            soundButton.gameObject.SetActive(false);
            volumeSlider.gameObject.SetActive(false);
        }
        public void SetClientId(int clientId)
        {
            clientID = clientId;
        }
        public void SetProfile(Texture2D texture)
        {
            if(texture)
                profileImge.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
        }

        public void SetName(string name)
        {
            profileName.text = name;
            DisplayName = name;
        }

        public void SetVivoxRoster(RosterItem item)
        {
            rosterItem = item;
            rosterItem.ParticipantStateChanged += ParticipantStateChanged;
            ParticipantStateChanged();
        }

        public void RemoveVivoxRoster()
        {
            rosterItem.ParticipantStateChanged -= ParticipantStateChanged;
            rosterItem = null;
        }

        private void ParticipantStateChanged()
        {
            if (rosterItem != null)
            {
                if (rosterItem.IsMuted)
                {
                    soundImage.sprite = muteSprite;
                }
                else if(soundImage)
                {
                    soundImage.sprite = unmuteSprite;
                    soundImage.color = rosterItem.IsSpeaking ? speakerColor : Color.white;
                }
            }
        }

        private void SetRosterVolume(float volume)
        {
            rosterItem?.SetRosterVolume((int)volume);
        }
        
        private void SetRosterMute(bool value)
        {
            rosterItem?.SetRosterMuted(value);
        }

        private void OnDestroy()
        {
            RemoveVivoxRoster();
        }
    }
}