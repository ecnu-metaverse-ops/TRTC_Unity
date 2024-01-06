using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using trtc;
using LitJson;
using TRTCCUnityDemo;
# if PLATFORM_ANDROID
using UnityEngine.Android;
# endif

namespace TRTCCUnityDemo
{
    public class RoomSceneScript : MonoBehaviour, ITRTCCloudCallback
    {
        public RectTransform mainCanvas;
        public UserTableView userTableView;

        public GameObject settingPrefab;
        public GameObject customCapturePrefab;
        private SettingScript settingScript = null;
        private CustomCaptureScript customCaptureScript = null;


        private ITRTCCloud mTRTCCloud;

        private SynchronizationContext MainContext;

        void Start()
        {
            MainContext = SynchronizationContext.Current;

            mTRTCCloud = ITRTCCloud.getTRTCShareInstance();
            mTRTCCloud.addCallback(this);

            var version = mTRTCCloud.getSDKVersion();
            LogManager.Log("trtc sdk version is : " + version);

            TRTCParams trtcParams = new TRTCParams();
            trtcParams.sdkAppId = GenerateTestUserSig.SDKAPPID;
            trtcParams.roomId = uint.Parse(DataManager.GetInstance().GetRoomID());
            trtcParams.strRoomId = trtcParams.roomId.ToString();
            trtcParams.userId = DataManager.GetInstance().GetUserID();
            trtcParams.userSig = GenerateTestUserSig.GetInstance().GenTestUserSig(DataManager.GetInstance().GetUserID());

            trtcParams.privateMapKey = "";
            trtcParams.businessInfo = "";
            trtcParams.role = DataManager.GetInstance().roleType;
            TRTCAppScene scene = DataManager.GetInstance().appScene;
            mTRTCCloud.enterRoom(ref trtcParams, scene);
            SetLocalAVStatus();
            TRTCVideoEncParam videoEncParams = DataManager.GetInstance().videoEncParam;
            mTRTCCloud.setVideoEncoderParam(ref videoEncParams);

            TRTCRenderParams renderParams = new TRTCRenderParams();
            TRTCVideoRotation videoRotation = TRTCVideoRotation.TRTCVideoRotation0;
#if UNITY_IOS && !UNITY_EDITOR
            renderParams.rotation = TRTCVideoRotation.TRTCVideoRotation90;
            videoRotation = TRTCVideoRotation.TRTCVideoRotation270;
#endif
            mTRTCCloud.setVideoEncoderRotation(videoRotation);
            mTRTCCloud.setLocalRenderParams(renderParams);

            TRTCNetworkQosParam qosParams = DataManager.GetInstance().qosParams; // 网络流控相关参数设置
            mTRTCCloud.setNetworkQosParam(ref qosParams);

            LogManager.Log("Scene:" + scene + ", Role:" + trtcParams.role + ", Qos-Prefer:" + qosParams.preference +
                           ", Qos-CtrlMode:" + qosParams.controlMode);

            userTableView.DoMuteAudio += OnMuteRemoteAudio;
            userTableView.DoMuteVideo += OnMuteRemoteVideo;
            DataManager.GetInstance().DoRoleChange += OnRoleChanged;
            DataManager.GetInstance().DoVoiceChange += OnVoiceChangeChanged;
            DataManager.GetInstance().DoEarMonitorVolumeChange += OnEarMonitorVolumeChanged;
            DataManager.GetInstance().DoVideoEncParamChange += OnVideoEncParamChanged;
            DataManager.GetInstance().DoQosParamChange += OnQosParamChanged;

#if PLATFORM_ANDROID
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
            if (!Permission.HasUserAuthorizedPermission(Permission.Camera))
            {
                Permission.RequestUserPermission(Permission.Camera);
            }
#endif
        }

        void OnDestroy()
        {
            Debug.LogFormat("OnDestroy");
            DataManager.GetInstance().DoRoleChange -= OnRoleChanged;
            DataManager.GetInstance().DoVoiceChange -= OnVoiceChangeChanged;
            DataManager.GetInstance().DoEarMonitorVolumeChange -= OnEarMonitorVolumeChanged;
            DataManager.GetInstance().DoVideoEncParamChange -= OnVideoEncParamChanged;
            DataManager.GetInstance().DoQosParamChange -= OnQosParamChanged;
            userTableView.DoMuteAudio -= OnMuteRemoteAudio;
            userTableView.DoMuteVideo -= OnMuteRemoteVideo;

            mTRTCCloud.removeCallback(this);
            ITRTCCloud.destroyTRTCShareInstance();
            DataManager.GetInstance().ResetLocalAVFlag();
        }

        private void SetLocalAVStatus()
        {
            TRTCRoleType role = DataManager.GetInstance().roleType;
            bool captureVideo = DataManager.GetInstance().captureVideo;
            bool muteLocalVideo = DataManager.GetInstance().muteLocalVideo;
            bool captureAudio = DataManager.GetInstance().captureAudio;
            bool muteLocalAudio = DataManager.GetInstance().muteLocalAudio;
            bool isAudience = (role == TRTCRoleType.TRTCRoleAudience);
            if (isAudience)
            {
                captureVideo = false;
                captureAudio = false;
            }

            if (captureVideo)
            {
                mTRTCCloud.startLocalPreview(true, null);
                userTableView.UpdateVideoAvailable("", TRTCVideoStreamType.TRTCVideoStreamTypeBig, true);
            }
            else
            {
                mTRTCCloud.stopLocalPreview();
                userTableView.UpdateVideoAvailable("", TRTCVideoStreamType.TRTCVideoStreamTypeBig, false);
            }

            mTRTCCloud.muteLocalVideo(muteLocalVideo);

            if (captureAudio)
            {
                mTRTCCloud.startLocalAudio(TRTCAudioQuality.TRTCAudioQualityDefault);
            }
            else
            {
                mTRTCCloud.stopLocalAudio();
            }

            mTRTCCloud.muteLocalAudio(muteLocalAudio);
        }

        #region UI Oper

        void OnToggleMic(bool value)
        {
            LogManager.Log("OnToggleMic: " + value);
            if (value)
            {
                mTRTCCloud.startLocalAudio(TRTCAudioQuality.TRTCAudioQualityDefault);
            }
            else
            {
                mTRTCCloud.stopLocalAudio();
            }

            DataManager.GetInstance().captureAudio = value;
        }

        void OnToggleCamera(bool value)
        {
            // LogManager.Log("OnToggleCamera: " + value);
            // if (value)
            // {
            //     mTRTCCloud.startLocalPreview(true, null);
            //     userTableView.UpdateVideoAvailable("", TRTCVideoStreamType.TRTCVideoStreamTypeBig, true);
            // }
            // else
            // {
            //     mTRTCCloud.stopLocalPreview();
            //     userTableView.UpdateVideoAvailable("", TRTCVideoStreamType.TRTCVideoStreamTypeBig, false);
            // }

            DataManager.GetInstance().captureVideo = value;
        }

        void OnToggleEnableEarMonitor(bool value)
        {
            LogManager.Log("OnToggleEnableEarMonitor enable =" + value);
            mTRTCCloud.getAudioEffectManager().enableVoiceEarMonitor(value);
        }

        void OnToggleMuteLocalVideo(bool value)
        {
            LogManager.Log("OnToggleMuteLocalVideo: " + value);
            mTRTCCloud.muteLocalVideo(value);
            DataManager.GetInstance().muteLocalVideo = value;
        }


        void OnToggleMuteRemoteVideo(bool value)
        {
            LogManager.Log("OnToggleMuteRemoteVideo: " + value);
            mTRTCCloud.muteAllRemoteVideoStreams(value);
        }

        void OnToggleMuteLocalAudio(bool value)
        {
            LogManager.Log("OnToggleMuteLocalAudio: " + value);
            mTRTCCloud.muteLocalAudio(value);
            DataManager.GetInstance().muteLocalAudio = value;
        }

        void OnToggleMuteRemoteAudio(bool value)
        {
            LogManager.Log("OnToggleMuteRemoteAudio: " + value);
            mTRTCCloud.muteAllRemoteAudio(value);
            // mTRTCCloud.enable3DSpatialAudioEffect(value);
        }


        void OnToggleScreenCapture(bool value)
        {
            if (value)
            {
                TRTCVideoEncParam videoEncParam = new TRTCVideoEncParam()
                {
                    videoResolution = TRTCVideoResolution.TRTCVideoResolution_1280_720,
                    resMode = TRTCVideoResolutionMode.TRTCVideoResolutionModeLandscape,
                    videoFps = 10,
                    videoBitrate = 1600,
                    minVideoBitrate = 1000
                };
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                    int thumbnailWidth = 100;
                    int thumbnailHeight = 60;
                    TRTCScreenCaptureSourceInfo[] sources =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              mTRTCCloud.getScreenCaptureSources(thumbnailWidth, thumbnailHeight);
                    Debug.LogFormat("sources id={0}", sources.Length);
                    if (sources.Length > 0)
                    {
                        for (int i = 0; i < sources.Length; ++i) {
                            LogManager.Log(String.Format("item {0}, {1}, {2}", sources.Length, sources[i].sourceId, sources[i].sourceName));
                            Debug.LogFormat("item id={0}, name={1}", sources[i].sourceId, sources[i].sourceName);
                        }
                        mTRTCCloud.selectScreenCaptureTarget(sources[0], new Rect(0, 0, 0, 0), new TRTCScreenCaptureProperty());
                        mTRTCCloud.startScreenCapture(TRTCVideoStreamType.TRTCVideoStreamTypeSub, ref videoEncParam);
                        userTableView.AddUser("", TRTCVideoStreamType.TRTCVideoStreamTypeSub);
                        userTableView.UpdateVideoAvailable("", TRTCVideoStreamType.TRTCVideoStreamTypeSub, true);
                    }
#elif UNITY_ANDROID || UNITY_IOS
                mTRTCCloud.startScreenCapture(TRTCVideoStreamType.TRTCVideoStreamTypeSub, ref videoEncParam);
#endif
#if UNITY_IOS
                IosExtensionLauncher.TRTCUnityExtensionLauncher();
#endif
            }
            else
            {
                mTRTCCloud.stopScreenCapture();
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX
                userTableView.UpdateVideoAvailable("", TRTCVideoStreamType.TRTCVideoStreamTypeSub, false);
                userTableView.RemoveUser("", TRTCVideoStreamType.TRTCVideoStreamTypeSub);
#endif
            }
        }

        #endregion

        #region Setting UI Callback

        void OnMuteRemoteAudio(string userId, bool mute)
        {
            LogManager.Log("MuteRemoteAudio: " + userId + "-" + mute);
            mTRTCCloud.muteRemoteAudio(userId, mute);
        }

        void OnMuteRemoteVideo(string userId, bool mute)
        {
            LogManager.Log("MuteRemoteVideo: " + userId + "-" + mute);
            mTRTCCloud.muteRemoteVideoStream(userId, mute);
        }

        void OnRoleChanged()
        {
            SetLocalAVStatus();
            mTRTCCloud.switchRole(DataManager.GetInstance().roleType);
        }

        void OnVoiceChangeChanged()
        {
            TXVoiceChangeType type = DataManager.GetInstance().voiceChangeType;
            LogManager.Log("OnVoiceChangeChanged type =" + type);
            mTRTCCloud.getAudioEffectManager().setVoiceChangerType(type);
        }

        void OnEarMonitorVolumeChanged()
        {
            int volume = DataManager.GetInstance().earMonitorVolume;
            LogManager.Log("OnEarMonitorVolumeChanged volume =" + volume);
            mTRTCCloud.getAudioEffectManager().setVoiceEarMonitorVolume(volume);
        }

        void OnVideoEncParamChanged()
        {
            TRTCVideoEncParam videoEncParams = DataManager.GetInstance().videoEncParam;
            mTRTCCloud.setVideoEncoderParam(ref videoEncParams);
        }

        void OnQosParamChanged()
        {
            TRTCNetworkQosParam qosParams = DataManager.GetInstance().qosParams;
            mTRTCCloud.setNetworkQosParam(ref qosParams);
        }

        #endregion

        #region ITRTCCloudCallback

        public void onError(TXLiteAVError errCode, String errMsg, IntPtr arg)
        {
            LogManager.Log(String.Format("onError {0}, {1}", errCode, errMsg));
        }

        public void onWarning(TXLiteAVWarning warningCode, String warningMsg, IntPtr arg)
        {
            LogManager.Log(String.Format("onWarning {0}, {1}", warningCode, warningMsg));
        }

        public void onEnterRoom(int result)
        {
            LogManager.Log(String.Format("onEnterRoom {0}", result));
            MainContext.Post(_ => { userTableView.AddUser("", TRTCVideoStreamType.TRTCVideoStreamTypeBig); }, null);
        }

        public void onExitRoom(int reason)
        {
            LogManager.Log(String.Format("onExitRoom {0}", reason));
            MainContext.Post(_ =>
            {
                userTableView.RemoveUser("", TRTCVideoStreamType.TRTCVideoStreamTypeBig);
                SceneManager.LoadScene("HomeScene", LoadSceneMode.Single);
            }, null);
        }

        public void onSwitchRole(TXLiteAVError errCode, String errMsg)
        {
            LogManager.Log(String.Format("onSwitchRole {0}, {1}", errCode, errMsg));
        }

        public void onRemoteUserEnterRoom(String userId)
        {
            LogManager.Log(String.Format("onRemoteUserEnterRoom {0}", userId));
        }
        public void onRemoteUserLeaveRoom(String userId, int reason)
        {
            LogManager.Log(String.Format("onRemoteUserLeaveRoom {0}, {1}", userId, reason));
            MainContext.Post(_ => { userTableView.RemoveUser(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig); },
                null);
        }

        public void onUserVideoAvailable(String userId, bool available)
        {
            LogManager.Log(String.Format("onUserVideoAvailable {0}, {1}", userId, available));
            // Important: startRemoteView is needed for receiving video stream.
            MainContext.Post(_ =>
            {
                if (available)
                {
                    userTableView.AddUser(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig);
                    mTRTCCloud.startRemoteView(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig, null);
                }
                else
                {
                    mTRTCCloud.stopRemoteView(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig);
                }

                userTableView.UpdateVideoAvailable(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig, available);
            }, null);
        }

        public void onUserSubStreamAvailable(String userId, bool available)
        {
            LogManager.Log(String.Format("onUserSubStreamAvailable {0}, {1}", userId, available));
            // Important: startRemoteView is needed for receiving video stream.
            MainContext.Post(_ =>
            {
                if (available)
                {
                    userTableView.AddUser(userId, TRTCVideoStreamType.TRTCVideoStreamTypeSub);
                    userTableView.UpdateVideoAvailable(userId, TRTCVideoStreamType.TRTCVideoStreamTypeSub, available);
                    mTRTCCloud.startRemoteView(userId, TRTCVideoStreamType.TRTCVideoStreamTypeSub, null);
                }
                else
                {
                    mTRTCCloud.stopRemoteView(userId, TRTCVideoStreamType.TRTCVideoStreamTypeSub);
                    userTableView.RemoveUser(userId, TRTCVideoStreamType.TRTCVideoStreamTypeSub);
                }
            }, null);
        }

        public void onUserAudioAvailable(String userId, bool available)
        {
            LogManager.Log(String.Format("onUserAudioAvailable {0}, {1}", userId, available));
            MainContext.Post(_ =>
            {
                if (available)
                {
                    userTableView.AddUser(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig);
                }

                userTableView.UpdateAudioAvailable(userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig, available);
            }, null);
        }

        public void onFirstVideoFrame(String userId, TRTCVideoStreamType streamType, int width, int height)
        {
            LogManager.Log(String.Format("onFirstVideoFrame {0}, {1}, {2}, {3}", userId, streamType, width, height));
        }

        public void onFirstAudioFrame(String userId)
        {
            LogManager.Log(String.Format("onFirstAudioFrame {0}", userId));
        }

        public void onSendFirstLocalVideoFrame(TRTCVideoStreamType streamType)
        {
            LogManager.Log(String.Format("onSendFirstLocalVideoFrame {0}", streamType));
        }

        public void onSendFirstLocalAudioFrame()
        {
            LogManager.Log(String.Format("onSendFirstLocalAudioFrame"));
        }

        public void onNetworkQuality(TRTCQualityInfo localQuality, TRTCQualityInfo[] remoteQuality,
            UInt32 remoteQualityCount)
        {
            // LogManager.Log(String.Format("onNetworkQuality {0}, {1}, {2}", localQuality, remoteQuality, remoteQualityCount));
        }

        public void onStatistics(TRTCStatistics statis)
        {
            MainContext.Post(_ =>
            {
                // LogManager.Log(String.Format("onStatistics {0}", statis));
                string localStatisText = "";
                foreach (TRTCLocalStatistics local in statis.localStatisticsArray)
                {
                    localStatisText = string.Format(
                        "width: {0}\r\nheight: {1}\r\nvideoframerate: {2}\r\nvideoBitrate: {3}\r\naudioSampleRate: {4}\r\naudioBitrate:{5}\r\nstreamType:{6}\r\n",
                        local.width, local.height,
                        local.frameRate, local.videoBitrate, local.audioSampleRate, local.audioBitrate,
                        local.streamType);
                    userTableView.updateUserStatistics("", local.streamType, localStatisText);
                }

                foreach (TRTCRemoteStatistics remote in statis.remoteStatisticsArray)
                {
                    string remoteStatisText = "";
                    remoteStatisText = string.Format(
                        "finalLoss: {7}\r\njitterBufferDelay: {8}\r\nwidth: {0}\r\nheight: {1}\r\nvideoframerate: {2}\r\nvideoBitrate: {3}\r\naudioSampleRate: {4}\r\naudioBitrate:{5}\r\nstreamType:{6}\r\n",
                        remote.width, remote.height,
                        remote.frameRate, remote.videoBitrate, remote.audioSampleRate, remote.audioBitrate,
                        remote.streamType,
                        remote.finalLoss, remote.jitterBufferDelay);
                    userTableView.updateUserStatistics(remote.userId, remote.streamType, remoteStatisText);
                }
            }, null);
        }

        public void onConnectionLost()
        {
            LogManager.Log(String.Format("onConnectionLost"));
        }

        public void onTryToReconnect()
        {
            LogManager.Log(String.Format("onTryToReconnect"));
        }

        public void onConnectionRecovery()
        {
            LogManager.Log(String.Format("onConnectionRecovery"));
        }

        public void onCameraDidReady()
        {
            LogManager.Log(String.Format("onCameraDidReady"));
        }

        public void onMicDidReady()
        {
            LogManager.Log(String.Format("onMicDidReady"));
        }

        public void onUserVoiceVolume(TRTCVolumeInfo[] userVolumes, UInt32 userVolumesCount, UInt32 totalVolume)
        {
            MainContext.Post(_ =>
            {
                foreach (TRTCVolumeInfo userVolume in userVolumes)
                {
                    userTableView.UpdateAudioVolume(userVolume.userId, TRTCVideoStreamType.TRTCVideoStreamTypeBig,
                        userVolume.volume);
                }
            }, null);
        }

        public void onDeviceChange(String deviceId, TRTCDeviceType type, TRTCDeviceState state)
        {
            LogManager.Log(String.Format("onSwitchRole {0}, {1}, {2}", deviceId, type, state));
        }

        public void onRecvSEIMsg(String userId, Byte[] message, UInt32 msgSize)
        {
            LogManager.Log("onRecvSEIMsg: " + userId + ", " + msgSize + ", " + msgSize);
            //LogManager.Log("onRecvSEIMsg: " + userId + ", " + msgSize + ", " + m_byteReceivedFacialData.Length);
            string strInfo = "";
            for (int i = 0; i < msgSize; i++)
            {
                strInfo += message[i].ToString() + ", ";
            }

            LogManager.Log("strInfo: " + strInfo);

            // string seiMessage = System.Text.Encoding.UTF8.GetString(message, 0, (int)msgSize);
            // LogManager.Log(String.Format("onRecvSEIMsg {0}, {1}, {2}", userId, seiMessage, msgSize));
        }

        public void onStartPublishing(int err, string errMsg)
        {
            LogManager.Log(String.Format("onStartPublishing {0}, {1}", err, errMsg));
        }

        public void onStopPublishing(int err, string errMsg)
        {
            LogManager.Log(String.Format("onStopPublishing {0}, {1}", err, errMsg));
        }

        public void onScreenCaptureStarted()
        {
            LogManager.Log(String.Format("onScreenCaptureStarted"));
        }

        public void onScreenCapturePaused(int reason)
        {
            LogManager.Log(String.Format("onScreenCapturePaused {0}", reason));
        }

        public void onScreenCaptureResumed(int reason)
        {
            LogManager.Log(String.Format("onScreenCaptureResumed {0}", reason));
        }

        public void onScreenCaptureStoped(int reason)
        {
            LogManager.Log(String.Format("onScreenCaptureStoped {0}", reason));
        }

        public void onStartPublishCDNStream(int err, string errMsg)
        {
            LogManager.Log(String.Format("onStartPublishCDNStream {0}, {1}", err, errMsg));
        }

        public void onStopPublishCDNStream(int err, string errMsg)
        {
            LogManager.Log(String.Format("onStopPublishCDNStream {0}, {1}", err, errMsg));
        }

        public void onConnectOtherRoom(string userId, TXLiteAVError errCode, string errMsg)
        {
            LogManager.Log(String.Format("onConnectOtherRoom {0}, {1}, {2}", userId, errCode, errMsg));
        }

        public void onDisconnectOtherRoom(TXLiteAVError errCode, string errMsg)
        {
            LogManager.Log(String.Format("onDisconnectOtherRoom {0}, {1}", errCode, errMsg));
        }

        public void onSwitchRoom(TXLiteAVError errCode, string errMsg)
        {
            LogManager.Log(String.Format("onSwitchRoom {0}, {1}", errCode, errMsg));
        }

        public void onSpeedTest(TRTCSpeedTestResult currentResult, int finishedCount, int totalCount)
        {
            LogManager.Log(String.Format("onSpeedTest {0}, {1} ,{2}", currentResult.upLostRate, finishedCount,
                totalCount));
        }

        public void onTestMicVolume(int volume)
        {
            LogManager.Log(String.Format("onTestMicVolume {0}", volume));
        }

        public void onTestSpeakerVolume(int volume)
        {
            LogManager.Log(String.Format("onTestSpeakerVolume {0}", volume));
        }

        public void onAudioDeviceCaptureVolumeChanged(int volume, bool muted)
        {
            LogManager.Log(String.Format("onAudioDeviceCaptureVolumeChanged {0} , {1}", volume, muted));
        }

        public void onAudioDevicePlayoutVolumeChanged(int volume, bool muted)
        {
            LogManager.Log(String.Format("onAudioDevicePlayoutVolumeChanged {0} , {1}", volume, muted));
        }

        public void onRecvCustomCmdMsg(string userId, int cmdID, int seq, byte[] message, int messageSize)
        {
            string msg = System.Text.Encoding.UTF8.GetString(message, 0, messageSize);
            LogManager.Log(Environment.NewLine + String.Format("onRecvCustomCmdMsg {0}, {1} ,{2}", userId, cmdID, msg));
        }

        public void onMissCustomCmdMsg(string userId, int cmdID, int errCode, int missed)
        {
            LogManager.Log(String.Format("onMissCustomCmdMsg {0}, {1}", userId, cmdID));
        }

        public void onSnapshotComplete(string userId, TRTCVideoStreamType type, byte[] data, int length, int width,
            int height, TRTCVideoPixelFormat format)
        {
            LogManager.Log(String.Format("onSnapshotComplete {0} , {1}", userId, type));
        }

        public void onSetMixTranscodingConfig(int errCode, String errMsg)
        {
            LogManager.Log(String.Format("onSetMixTranscodingConfig {0} , {1}", errCode, errMsg));
        }

        #endregion
    }
}