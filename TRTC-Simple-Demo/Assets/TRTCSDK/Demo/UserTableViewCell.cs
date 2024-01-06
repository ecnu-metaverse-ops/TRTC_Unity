using System;
using UnityEngine;
using UnityEngine.UI;
using trtc;

namespace TRTCCUnityDemo
{
    public class UserTableViewCell : MonoBehaviour
    {
        public delegate void MuteAudioHandler(string userId, bool mute);
        public delegate void MuteVideoHandler(string userId, bool mute);

        public event MuteAudioHandler DoMuteAudio;
        public event MuteVideoHandler DoMuteVideo;

        public TRTCVideoRender VideoRender;

        private string userIdStr;
        public string UserIdStr
        {
            set
            {
                userIdStr = value;
            }
        }

        private TRTCVideoStreamType streamTypeInt;
        public TRTCVideoStreamType StreamTypeInt
        {
            set
            {
                streamTypeInt = value;
            }
        }

        public bool isAudioMute = false;
        public bool IsAudioMute
        {
            set
            {
                isAudioMute = value;
                updateAudioBtn();
            }
        }

        public bool isVideoMute = false;
        public bool IsVideoMute
        {
            set
            {
                isVideoMute = value;
                updateVideoBtn();
            }
        }

        private bool isVideoAvailable = false;
        public bool IsVideoAvailable
        {
            set
            {
                isVideoAvailable = value;
                VideoRender.gameObject.SetActive(isVideoAvailable);
                VideoRender.GetComponent<TRTCVideoRender>().Clear();
                VideoRender.SetForUser(userIdStr, streamTypeInt);
            }
        }

        public UInt32 AudioVolume
        {
            set {
                
            }
        }

        public bool AudioVolumeVisible
        {
            set
            {
            }
        }

        public string UserStatisText
        {
            set
            {
            }
        }

        public bool UserStatisVisible
        {
            set
            {
            }
        }

        void Start()
        {

        }

        void Update()
        {

        }

        public void CellSwitchRenderMode()
        {
            TRTCVideoFillMode videoFillMode = VideoRender.GetViewFillMode();
            if (videoFillMode == TRTCVideoFillMode.TRTCVideoFillMode_Fit)
                videoFillMode = TRTCVideoFillMode.TRTCVideoFillMode_Fill;
            else
                videoFillMode = TRTCVideoFillMode.TRTCVideoFillMode_Fit;
            VideoRender.SetViewFillMode(videoFillMode);
        }

        public void CellMuteAudioAction()
        {
            if (DoMuteAudio != null)
            {
                DoMuteAudio(userIdStr, !isAudioMute);
            }
            isAudioMute = !isAudioMute;
            updateAudioBtn();
        }

        public void CellMuteVideoAction()
        {
            if (DoMuteVideo != null)
            {
                DoMuteVideo(userIdStr, !isVideoMute);
            }
            isVideoMute = !isVideoMute;
            updateVideoBtn();
        }

        private void updateAudioBtn()
        {
        }

        private void updateVideoBtn()
        {
        }
    }
}