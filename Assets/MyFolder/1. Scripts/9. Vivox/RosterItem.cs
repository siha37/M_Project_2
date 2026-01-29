using System;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.UI;

namespace MyFolder._1._Scripts._9._Vivox
{
    public class RosterItem
    {
        // Player specific items.
        public VivoxParticipant Participant;

        public bool IsMuted;
        public bool IsSpeaking;
        
        public Action ParticipantStateChanged;

        private void UpdateChatStateImage()
        {
            if (Participant.IsMuted)
            {
                IsMuted = true;
            }
            else
            {
                IsMuted = false;
                if (Participant.SpeechDetected)
                {
                    IsSpeaking = true;
                }
                else
                {
                    IsSpeaking = false;
                }
            }
            ParticipantStateChanged?.Invoke();
        }

        public void SetupRosterItem(VivoxParticipant participant)
        {
            //Set the Participant variable of this RosterItem to the VivoxParticipant added in the RosterManager
            Participant = participant;

            // Update the image to the active state of the user (either the SpeakingImage, the MutedImage, or the NotSpeakingImage) and then attach
            // the function to run if an event is fired denoting a change to that users state
            UpdateChatStateImage();
            Participant.ParticipantMuteStateChanged += UpdateChatStateImage;
            Participant.ParticipantSpeechDetected += UpdateChatStateImage;
        }

        public void RosterRemove()
        {
            Participant.ParticipantMuteStateChanged -= UpdateChatStateImage;
            Participant.ParticipantSpeechDetected -= UpdateChatStateImage;
        }

        public void SetRosterVolume(int volume)
        {
            Participant.SetLocalVolume(volume);
        }

        public void SetRosterMuted(bool muted)
        {
            if(muted)
                Participant.MutePlayerLocally();
            else
                Participant.UnmutePlayerLocally();
        }
    }
}