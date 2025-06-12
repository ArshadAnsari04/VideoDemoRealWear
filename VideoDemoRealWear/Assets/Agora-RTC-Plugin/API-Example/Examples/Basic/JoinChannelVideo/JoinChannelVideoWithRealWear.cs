//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using Agora.Rtc;
//using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideo;
//using io.agora.rtc.demo;

//using UnityEngine;
//using UnityEngine.Serialization;
//using UnityEngine.UI;
//using WearHFPlugin;

//namespace Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear
//{
//    public class JoinChannelVideoWithRealWear : MonoBehaviour
//    {
//        [FormerlySerializedAs("appIdInput")]
//        [SerializeField]
//        private AppIdInput _appIdInput;

//        [Header("_____________Basic Configuration_____________")]
//        [FormerlySerializedAs("APP_ID")]
//        [SerializeField]
//        private string _appID = "";

//        [FormerlySerializedAs("TOKEN")]
//        [SerializeField]
//        private string _token = "";

//        [FormerlySerializedAs("CHANNEL_NAME")]
//        [SerializeField]
//        private string _channelName = "";

//        public Text LogText;
//        internal Logger Log;
//        internal IRtcEngine RtcEngine = null;

//        public Dropdown _videoDeviceSelect;
//        private IVideoDeviceManager _videoDeviceManager;
//        private DeviceInfo[] _videoDeviceInfos;
//        public Dropdown _areaSelect;
//        public GameObject _videoQualityItemPrefab;


//        [SerializeField] private Text m_debugText;
//        [SerializeField] private RawImage m_image;
//        [SerializeField] private Button _takePhotoButton;
//        [SerializeField] internal RawImage _receivedImage;
//        private WebCamTexture _videoSource;
//        private Texture2D _passthroughTexture;
//        private Coroutine _pushFramesCoroutine;
//        internal bool _joinedChannel = false;
//        private int _dataStreamId = -1;

//        [SerializeField] private WearHF wearHFManager;

//        [SerializeField] private GameObject RealWearUI;
//        private void Start()
//        {
//            LoadAssetData();
//            PrepareAreaList();
//            if (CheckAppId())
//            {
//                RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
//                InitEngine();
//                StartCoroutine(InitializeVideoSource());
//                Invoke("JoinChannel", 3);
//            }

//#if UNITY_IOS || UNITY_ANDROID
//            Invoke("JoinChannel", 3);
//            m_image.gameObject.SetActive(true);
//            // Invoke("JoinChannel", 3);
//            var text = GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/VideoDeviceManager")?.GetComponent<Text>();
//            if (text != null) text.text = "Video device manager not supported on this platform";
//            GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/VideoDeviceButton")?.SetActive(false);
//            GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/deviceIdSelect")?.SetActive(false);
//            GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/VideoSelectButton")?.SetActive(false);
//#endif


//            if (wearHFManager == null)
//            {
//                wearHFManager = GameObject.Find("WearHF Manager").GetComponent<WearHF>();
//            }
//            //else
//            //{
//            //    // var wearHf = GameObject.Find("WearHF Manager").GetComponent<WearHF>();
//            //    wearHFManager.AddVoiceCommand("Take Photo", VoiceCommandCallback);
//            //}

//            if (_takePhotoButton != null)
//            {
//                _takePhotoButton.onClick.AddListener(TakeAndSendPhoto);
//            }

//#if UNITY_ANDROID && !UNITY_EDITOR
//            if (IsRealWearDevice())
//            {

//                if (_takePhotoButton != null) _takePhotoButton.gameObject.SetActive(false);
//                if (_videoDeviceSelect != null) _videoDeviceSelect.gameObject.SetActive(false);
//                if (_areaSelect != null) _areaSelect.gameObject.SetActive(false);

//                Log.UpdateLog("Running on RealWear device. Voice commands enabled.");
//                //wearHFManager.AddVoiceCommand("Take Photo", VoiceCommandCallback);
//               // wearHFManager.AddVoiceCommand("Open UI", VoiceCommandCallbackUI);
//                //StartCoroutine(ListenForVoiceCommands());
//            }
//#endif
//        }

//        public void JoinCall()
//        {
//            m_image.gameObject.SetActive(true);
//            RealWearUI.SetActive(false);

//            Invoke("JoinChannel", 3);
//        }

//        private void VoiceCommandCallbackUI(string voiceCommand)
//        {
//            if (voiceCommand.Equals("Open UI", StringComparison.OrdinalIgnoreCase))
//            {
//                Log.UpdateLog("Voice command recognized: Take Photo");
//                ActivateUI();
//            }
//        }

//        void ActivateUI()
//        {
//            RealWearUI.SetActive(true);
//        }
//        private void VoiceCommandCallback(string voiceCommand)
//        {
//            if (voiceCommand.Equals("Take Photo", StringComparison.OrdinalIgnoreCase))
//            {
//                Log.UpdateLog("Voice command recognized: Take Photo");
//                TakeAndSendPhoto();
//            }
//        }
//        /// <summary>
//        /// Called when the voice command is triggered by the user
//        /// </summary>
//        /// <param name="voiceCommand">The voice command that was triggered</param>
//        //void VoiceCommandCallback(string voiceCommand)
//        //{
//        //    Debug.Log("Voice command recognized: " + voiceCommand);
//        //    StartCoroutine(ListenForVoiceCommands());
//        //}

//        private bool IsRealWearDevice()
//        {
//            string deviceModel = SystemInfo.deviceModel.ToLower();
//            return deviceModel.Contains("hmt-1") || deviceModel.Contains("navigator");
//        }

//        private IEnumerator ListenForVoiceCommands()
//        {
//            while (true)
//            {
//                if (Input.GetKeyDown(KeyCode.P))
//                {
//                    Log.UpdateLog("Voice command: Take Photo");
//                    TakeAndSendPhoto();
//                }
//                yield return null;
//            }
//        }

//        private IEnumerator InitializeVideoSource()
//        {
//#if UNITY_ANDROID && !UNITY_EDITOR
//            if (IsRealWearDevice())
//            {
//                if (WebCamTexture.devices.Length == 0)
//                {
//                    Log.UpdateLog("Error: No camera found on RealWear device.");
//                    yield break;
//                }

//                _videoSource = new WebCamTexture(WebCamTexture.devices[0].name, 640, 360);
//                _videoSource.Play();
//                while (!_videoSource.isPlaying)
//                {
//                    Log.UpdateLog("Waiting for RealWear camera to start...");
//                    yield return null;
//                }

//                Log.UpdateLog("RealWear camera initialized.");
//            }
//            //else if (m_webCamTextureManager != null)
//            //{
//            //    while (m_webCamTextureManager.WebCamTexture == null)
//            //    {
//            //        Log.UpdateLog("Waiting for Quest 3 passthrough WebCamTexture...");
//            //        yield return null;
//            //    }

//            //    _videoSource = m_webCamTextureManager.WebCamTexture;
//            //    if (!_videoSource.isPlaying)
//            //    {
//            //        _videoSource.Play();
//            //        while (!_videoSource.isPlaying)
//            //        {
//            //            Log.UpdateLog("Starting Quest 3 passthrough camera...");
//            //            yield return null;
//            //        }
//            //    }

//            //    Log.UpdateLog("Quest 3 passthrough camera initialized.");
//            //}
//            else
//            {
//                Log.UpdateLog("Error: No WebCamTextureManager for Android (non-RealWear). Falling back to default camera.");
//                if (WebCamTexture.devices.Length == 0)
//                {
//                    Log.UpdateLog("Error: No camera found on Android device.");
//                    yield break;
//                }

//                _videoSource = new WebCamTexture(WebCamTexture.devices[0].name, 640, 360);
//                _videoSource.Play();
//                while (!_videoSource.isPlaying)
//                {
//                    Log.UpdateLog("Waiting for Android camera to start...");
//                    yield return null;
//                }

//                Log.UpdateLog("Android camera initialized.");
//            }
//#else
//            if (WebCamTexture.devices.Length == 0)
//            {
//                Log.UpdateLog("Error: No webcam devices found on Windows.");
//                yield break;
//            }

//            _videoSource = new WebCamTexture(WebCamTexture.devices[0].name, 640, 360);
//            _videoSource.Play();
//            while (!_videoSource.isPlaying)
//            {
//                Log.UpdateLog("Waiting for Windows webcam to start...");
//                yield return null;
//            }

//            Log.UpdateLog("Windows webcam initialized.");
//#endif

//            int width = _videoSource.width;
//            int height = _videoSource.height;
//            if (width <= 0 || height <= 0)
//            {
//                Log.UpdateLog($"Error: Invalid video dimensions ({width}x{height}).");
//                yield break;
//            }

//            try
//            {
//                _passthroughTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
//                Log.UpdateLog($"Texture buffer created: {width}x{height}");
//                m_image.texture = _videoSource;
//                m_debugText.text += "\nVideo source ready and playing.";
//            }
//            catch (Exception e)
//            {
//                Log.UpdateLog($"Failed to create passthroughTexture: {e.Message}");
//                yield break;
//            }

//            _pushFramesCoroutine = StartCoroutine(PushPassthroughFramesToAgora());
//        }

//        private IEnumerator PushPassthroughFramesToAgora()
//        {
//            if (RtcEngine == null || _passthroughTexture == null)
//            {
//                Log.UpdateLog("Error: RtcEngine or passthroughTexture is null.");
//                yield break;
//            }

//            while (!_joinedChannel)
//            {
//                Log.UpdateLog("Waiting for channel join...");
//                yield return null;
//            }

//            var options = new ChannelMediaOptions();
//            options.publishCameraTrack.SetValue(false);
//            options.publishCustomVideoTrack.SetValue(true);
//            int ret = RtcEngine.UpdateChannelMediaOptions(options);
//            Log.UpdateLog($"UpdateChannelMediaOptions for custom video: {ret}");

//            while (true)
//            {
//                if (_videoSource == null || !_videoSource.isPlaying)
//                {
//                    Log.UpdateLog("Error: Video source is null or not playing.");
//                    yield break;
//                }

//                try
//                {
//                    _passthroughTexture.SetPixels(_videoSource.GetPixels());
//                    _passthroughTexture.Apply();

//                    byte[] frameData = _passthroughTexture.GetRawTextureData().ToArray();
//                    if (frameData.Length == 0)
//                    {
//                        Log.UpdateLog("Error: Empty frame data.");
//                        continue;
//                    }

//                    ExternalVideoFrame externalFrame = new ExternalVideoFrame
//                    {
//                        type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA,
//                        format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA,
//                        buffer = frameData,
//                        stride = _passthroughTexture.width,
//                        height = _passthroughTexture.height,
//                        timestamp = (long)(Time.time * 1000)
//                    };

//                    Log.UpdateLog($"Pushing frame: width={_passthroughTexture.width}, height={_passthroughTexture.height}, stride={externalFrame.stride}, buffer size={frameData.Length}");
//                    ret = RtcEngine.PushVideoFrame(externalFrame);
//                    if (ret != 0)
//                    {
//                        Log.UpdateLog($"PushVideoFrame failed: {ret}");
//                    }
//                    else
//                    {
//                        Log.UpdateLog("PushVideoFrame succeeded");
//                    }
//                }
//                catch (Exception e)
//                {
//                    Log.UpdateLog($"PushPassthroughFramesToAgora error: {e.Message}\nStack: {e.StackTrace}");
//                    yield break;
//                }

//                yield return new WaitForSeconds(0.066f);
//            }
//        }

//        private void Update()
//        {
//            PermissionHelper.RequestMicrophontPermission();
//            PermissionHelper.RequestCameraPermission();
//            //#if UNITY_ANDROID && UNITY_EDITOR
//            //            m_debugText.text = PassthroughCameraPermissions.HasCameraPermission ? "Permission granted." : "No permission granted.";
//            //#else
//            //            m_debugText.text = "Running on Windows.";
//            //#endif
//        }

//        private void LoadAssetData()
//        {
//            if (_appIdInput == null) return;
//            _appID = _appIdInput.appID;
//            _token = _appIdInput.token;
//            _channelName = _appIdInput.channelName;
//        }

//        private bool CheckAppId()
//        {
//            Log = new Logger(LogText);
//            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
//        }

//        private void PrepareAreaList()
//        {
//            int index = 0;
//            var areaList = new List<Dropdown.OptionData>();
//            var enumNames = Enum.GetNames(typeof(AREA_CODE));
//            foreach (var name in enumNames)
//            {
//                areaList.Add(new Dropdown.OptionData(name));
//                if (name == "AREA_CODE_GLOB") index = areaList.Count - 1;
//            }
//            _areaSelect.ClearOptions();
//            _areaSelect.AddOptions(areaList);
//            _areaSelect.value = index;
//        }

//        public void InitEngine()
//        {
//            var areaCode = (AREA_CODE)Enum.Parse(typeof(AREA_CODE), _areaSelect.captionText.text);
//            Log.UpdateLog($"Select AREA_CODE: {areaCode}");

//            var handler = new RealWearUserEventHandler(this);
//            var context = new RtcEngineContext
//            {
//                appId = _appID,
//                channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
//                audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
//                areaCode = areaCode
//            };

//            int result = RtcEngine.Initialize(context);
//            Log.UpdateLog($"Initialize result: {result}");
//            if (result != 0) return;

//            RtcEngine.InitEventHandler(handler);
//            RtcEngine.EnableAudio();
//            RtcEngine.EnableVideo();
//            RtcEngine.SetChannelProfile(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);
//            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

//            var config = new VideoEncoderConfiguration
//            {
//                dimensions = new VideoDimensions(640, 360),
//                frameRate = 15,
//                bitrate = 400
//            };
//            RtcEngine.SetVideoEncoderConfiguration(config);

//            DataStreamConfig streamConfig = new DataStreamConfig
//            {
//                syncWithAudio = false,
//                ordered = true
//            };
//            int ret = RtcEngine.CreateDataStream(ref _dataStreamId, streamConfig);
//            Log.UpdateLog($"Data stream created with ID: {_dataStreamId}, Result: {ret}");

//            int build = 0;
//            string version = RtcEngine.GetVersion(ref build);
//            Log.UpdateLog($"Agora SDK Version: {version}, Build: {build}");
//        }

//        public void JoinChannel()
//        {
//            var options = new ChannelMediaOptions();
//            options.publishCameraTrack.SetValue(false);
//            options.publishCustomVideoTrack.SetValue(true);
//            options.autoSubscribeVideo.SetValue(true);

//            int ret = RtcEngine.JoinChannel(_token, _channelName, 0, options);
//            Log.UpdateLog($"JoinChannel (with options) result: {ret}");
//        }

//        //public void JoinChannel()
//        //{


//        //    int ret = RtcEngine.JoinChannel(_token, _channelName, "", 0);
//        //    Log.UpdateLog($"JoinChannel result: {ret}");
//        //}

//        public void LeaveChannel()
//        {
//            RtcEngine.LeaveChannel();
//            _joinedChannel = false;
//        }

//        public void StartPreview()
//        {
//            RtcEngine.StartPreview();
//            var node = MakeVideoView(0);
//            CreateLocalVideoCallQualityPanel(node);
//        }

//        public void StopPreview()
//        {
//            DestroyVideoView(0);
//            RtcEngine.StopPreview();
//        }

//        public void StartPublish()
//        {
//            var options = new ChannelMediaOptions();
//            options.publishMicrophoneTrack.SetValue(true);
//            options.publishCustomVideoTrack.SetValue(true);
//            int ret = RtcEngine.UpdateChannelMediaOptions(options);
//            Log.UpdateLog($"StartPublish: {ret}");
//        }

//        public void StopPublish()
//        {
//            var options = new ChannelMediaOptions();
//            options.publishMicrophoneTrack.SetValue(false);
//            options.publishCustomVideoTrack.SetValue(false);
//            int ret = RtcEngine.UpdateChannelMediaOptions(options);
//            Log.UpdateLog($"StopPublish: {ret}");
//        }

//        public void AdjustVideoEncodedConfiguration640()
//        {
//            var config = new VideoEncoderConfiguration
//            {
//                dimensions = new VideoDimensions(640, 360),
//                frameRate = 15,
//                bitrate = 400
//            };
//            RtcEngine.SetVideoEncoderConfiguration(config);
//        }

//        public void AdjustVideoEncodedConfiguration480()
//        {
//            var config = new VideoEncoderConfiguration
//            {
//                dimensions = new VideoDimensions(480, 480),
//                frameRate = 15,
//                bitrate = 400
//            };
//            RtcEngine.SetVideoEncoderConfiguration(config);
//        }

//        public void GetVideoDeviceManager()
//        {
//#if !UNITY_IOS && !UNITY_ANDROID
//            _videoDeviceSelect.ClearOptions();
//            _videoDeviceManager = RtcEngine.GetVideoDeviceManager();
//            _videoDeviceInfos = _videoDeviceManager.EnumerateVideoDevices();
//            Log.UpdateLog($"VideoDeviceManager count: {_videoDeviceInfos.Length}");
//            for (int i = 0; i < _videoDeviceInfos.Length; i++)
//            {
//                Log.UpdateLog($"Device {i}: {_videoDeviceInfos[i].deviceName}, ID: {_videoDeviceInfos[i].deviceId}");
//            }
//            _videoDeviceSelect.AddOptions(_videoDeviceInfos.Select(w => new Dropdown.OptionData($"{w.deviceName} :{w.deviceId}")).ToList());
//#endif
//        }

//        public void SelectVideoCaptureDevice()
//        {
//#if !UNITY_IOS && !UNITY_ANDROID
//            if (_videoDeviceSelect == null || _videoDeviceSelect.options.Count == 0) return;
//            var option = _videoDeviceSelect.options[_videoDeviceSelect.value].text;
//            var deviceId = option.Split(':')[1];
//            int ret = _videoDeviceManager.SetDevice(deviceId);
//            Log.UpdateLog($"SelectVideoCaptureDevice: {ret}, DeviceId: {deviceId}");

//            if (_videoSource != null) _videoSource.Stop();
//            _videoSource = new WebCamTexture(deviceId, 640, 360);
//            _videoSource.Play();
//            _passthroughTexture = new Texture2D(_videoSource.width, _videoSource.height, TextureFormat.RGBA32, false);
//            m_image.texture = _videoSource;
//#endif
//        }

//        private void TakeAndSendPhoto()
//        {
//            if (!_joinedChannel || _passthroughTexture == null || _dataStreamId < 0)
//            {
//                Log.UpdateLog("Cannot take photo: Not joined channel, no texture, or no data stream.");
//                return;
//            }

//            try
//            {
//                Texture2D photo = new Texture2D(_passthroughTexture.width, _passthroughTexture.height, TextureFormat.RGBA32, false);
//                photo.SetPixels(_passthroughTexture.GetPixels());
//                photo.Apply();

//                Texture2D resizedPhoto = ResizeTexture(photo, 320, 180);
//                UnityEngine.Object.Destroy(photo);

//                byte[] photoData = resizedPhoto.EncodeToJPG(50);
//                UnityEngine.Object.Destroy(resizedPhoto);

//                if (photoData.Length > 30000)
//                {
//                    Log.UpdateLog($"Photo data too large: {photoData.Length} bytes. Must be under 30 KB.");
//                    return;
//                }
//                int ret = RtcEngine.SendStreamMessage(_dataStreamId, photoData, (uint)photoData.Length);
//                Log.UpdateLog($"SendStreamMessage result: {ret}, Size: {photoData.Length} bytes");
//                // int ret = RtcEngine.SendStreamMessage(_dataStreamId, photoData);
//                //Log.UpdateLog($"SendStreamMessage result: {ret}, Size: {photoData.Length} bytes");
//            }
//            catch (Exception e)
//            {
//                Log.UpdateLog($"TakeAndSendPhoto error: {e.Message}");
//            }
//        }

//        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
//        {
//            try
//            {
//                RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
//                RenderTexture.active = rt;
//                Graphics.Blit(source, rt);
//                Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
//                result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
//                result.Apply();
//                RenderTexture.active = null;
//                RenderTexture.ReleaseTemporary(rt);
//                return result;
//            }
//            catch (Exception e)
//            {
//                Log.UpdateLog($"Failed to resize texture: {e.Message}");
//                return null;
//            }
//        }

//        private void OnDestroy()
//        {
//            Log.UpdateLog("OnDestroy called");

//            if (_pushFramesCoroutine != null)
//            {
//                StopCoroutine(_pushFramesCoroutine);
//                _pushFramesCoroutine = null;
//                Log.UpdateLog("PushPassthroughFramesToAgora coroutine stopped");
//            }

//            if (_videoSource != null && _videoSource.isPlaying)
//            {
//                _videoSource.Stop();
//                Log.UpdateLog("Video source stopped");
//            }

//            if (_passthroughTexture != null)
//            {
//                UnityEngine.Object.Destroy(_passthroughTexture);
//                _passthroughTexture = null;
//                Log.UpdateLog("Passthrough texture destroyed");
//            }

//            if (RtcEngine != null)
//            {
//                RtcEngine.InitEventHandler(null);
//                RtcEngine.LeaveChannel();
//                RtcEngine.Dispose();
//                RtcEngine = null;
//                Log.UpdateLog("RtcEngine disposed");
//            }

//            if (_takePhotoButton != null)
//            {
//                _takePhotoButton.onClick.RemoveListener(TakeAndSendPhoto);
//            }
//        }

//        internal string GetChannelName() => _channelName;

//        internal static GameObject MakeVideoView(uint uid, string channelId = "")
//        {
//            var go = GameObject.Find(uid.ToString());
//            if (go != null) return go;

//            var videoSurface = MakeImageSurface(uid.ToString());
//            if (videoSurface == null) return null;

//            videoSurface.SetForUser(uid, channelId, uid == 0 ? VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA : VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
//            videoSurface.OnTextureSizeModify += (width, height) =>
//            {
//                var transform = videoSurface.GetComponent<RectTransform>();
//#if UNITY_ANDROID && !UNITY_EDITOR
//                if (transform) transform.sizeDelta = new Vector2(640, 360);
//#else
//                if (transform) transform.sizeDelta = new Vector2(1450, 1080);
//#endif
//                Debug.Log($"OnTextureSizeModify: {width}x{height}");
//            };
//            videoSurface.SetEnable(true);
//            return videoSurface.gameObject;
//        }

//        private static VideoSurface MakeImageSurface(string goName)
//        {
//            var go = new GameObject(goName);
//            go.AddComponent<RawImage>();
//            go.AddComponent<UIElementDrag>();
//            var canvas = GameObject.Find("VideoCanvas");
//            if (canvas != null) go.transform.parent = canvas.transform;
//            go.transform.Rotate(0f, 0f, 0f);
//            go.transform.localPosition = new Vector3(200, 0, 0);
//            go.transform.localScale = new Vector3(1, 1, 1f);
//            return go.AddComponent<VideoSurface>();
//        }

//        internal static void DestroyVideoView(uint uid)
//        {
//            var go = GameObject.Find(uid.ToString());
//            if (go != null) UnityEngine.Object.Destroy(go);
//        }

//        public void CreateLocalVideoCallQualityPanel(GameObject parent)
//        {
//            if (parent.GetComponentInChildren<LocalVideoCallQualityPanel>() != null) return;
//            var panel = Instantiate(_videoQualityItemPrefab, parent.transform);
//            panel.AddComponent<LocalVideoCallQualityPanel>();
//        }

//        public LocalVideoCallQualityPanel GetLocalVideoCallQualityPanel()
//        {
//            var go = GameObject.Find("0");
//            return go?.GetComponentInChildren<LocalVideoCallQualityPanel>();
//        }

//        public void CreateRemoteVideoCallQualityPanel(GameObject parent, uint uid)
//        {
//            if (parent.GetComponentInChildren<RemoteVideoCallQualityPanel>() != null) return;
//            var panel = Instantiate(_videoQualityItemPrefab, parent.transform);
//            panel.transform.localPosition = new Vector3(0, -182, 0);
//            panel.transform.localScale = new Vector3(0, -1, 0);
//            panel.transform.Rotate(0f, 0f, 0f);
//            var comp = panel.AddComponent<RemoteVideoCallQualityPanel>();
//            comp.Uid = uid;
//        }

//        public RemoteVideoCallQualityPanel GetRemoteVideoCallQualityPanel(uint uid)
//        {
//            var go = GameObject.Find(uid.ToString());
//            return go?.GetComponentInChildren<RemoteVideoCallQualityPanel>();
//        }
//    }

//    internal class RealWearUserEventHandler : IRtcEngineEventHandler
//    {
//        private readonly JoinChannelVideoWithRealWear _videoSample;

//        internal RealWearUserEventHandler(JoinChannelVideoWithRealWear videoSample)
//        {
//            _videoSample = videoSample;
//        }

//        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
//        {
//            int build = 0;
//            _videoSample.Log.UpdateLog($"SDK Version: {_videoSample.RtcEngine.GetVersion(ref build)}, Build: {build}");
//            _videoSample.Log.UpdateLog($"OnJoinChannelSuccess: {connection.channelId}, UID: {connection.localUid}, Elapsed: {elapsed}");
//            _videoSample._joinedChannel = true;
//        }

//        public override void OnError(int err, string msg) => _videoSample.Log.UpdateLog($"OnError: {err}, Msg: {msg}");

//        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed) => _videoSample.Log.UpdateLog("OnRejoinChannelSuccess");

//        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats) => _videoSample.Log.UpdateLog("OnLeaveChannel");

//        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole, CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions) =>
//            _videoSample.Log.UpdateLog("OnClientRoleChanged");

//        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
//        {
//            _videoSample.Log.UpdateLog($"OnUserJoined: UID: {uid}, Elapsed: {elapsed}");
//            var node = JoinChannelVideoWithRealWear.MakeVideoView(uid, _videoSample.GetChannelName());
//            _videoSample.CreateRemoteVideoCallQualityPanel(node, uid);
//        }

//        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason) =>
//            _videoSample.Log.UpdateLog($"OnUserOffline: UID: {uid}, Reason: {(int)reason}");

//        public override void OnRtcStats(RtcConnection connection, RtcStats stats)
//        {
//            var panel = _videoSample.GetLocalVideoCallQualityPanel();
//            if (panel != null) { panel.Stats = stats; panel.RefreshPanel(); }
//        }

//        public override void OnLocalAudioStats(RtcConnection connection, LocalAudioStats stats)
//        {
//            var panel = _videoSample.GetLocalVideoCallQualityPanel();
//            if (panel != null) { panel.AudioStats = stats; panel.RefreshPanel(); }
//        }

//        public override void OnLocalVideoStats(RtcConnection connection, LocalVideoStats stats)
//        {
//            var panel = _videoSample.GetLocalVideoCallQualityPanel();
//            if (panel != null) { panel.VideoStats = stats; panel.RefreshPanel(); }
//        }

//        public override void OnRemoteVideoStats(RtcConnection connection, RemoteVideoStats stats)
//        {
//            var panel = _videoSample.GetRemoteVideoCallQualityPanel(stats.uid);
//            if (panel != null) { panel.VideoStats = stats; panel.RefreshPanel(); }
//        }

//        public override void OnStreamMessage(RtcConnection connection, uint remoteUid, int streamId, byte[] data, ulong length, ulong sentTs)
//        {
//            try
//            {
//                _videoSample.Log.UpdateLog($"Received stream message from UID: {remoteUid}, Stream ID: {streamId}, Size: {length} bytes");

//                Texture2D receivedTexture = new Texture2D(2, 2);
//                if (receivedTexture.LoadImage(data))
//                {
//                    if (_videoSample._receivedImage != null)
//                    {
//                        _videoSample._receivedImage.texture = receivedTexture;
//                        _videoSample._receivedImage.GetComponent<RectTransform>().sizeDelta = new Vector2(receivedTexture.width, receivedTexture.height);
//                        _videoSample.Log.UpdateLog($"Displayed received photo: {receivedTexture.width}x{receivedTexture.height}");
//                    }
//                    else
//                    {
//                        _videoSample.Log.UpdateLog("Received photo but no RawImage assigned to display it.");
//                        UnityEngine.Object.Destroy(receivedTexture);
//                    }
//                }
//                else
//                {
//                    _videoSample.Log.UpdateLog("Failed to load received photo data.");
//                    UnityEngine.Object.Destroy(receivedTexture);
//                }
//            }
//            catch (Exception e)
//            {
//                _videoSample.Log.UpdateLog($"OnStreamMessage error: {e.Message}");
//            }
//        }
//    }
//}



using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideo;
using io.agora.rtc.demo;

using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WearHFPlugin;

namespace Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear
{
    [Serializable]
    public class CallInviteMessage
    {
        public uint targetUid;
        public string type = "invite";
        public string channelName;
    }

    public class JoinChannelVideoWithRealWear : MonoBehaviour
    {
        [FormerlySerializedAs("appIdInput")]
        [SerializeField]
        private AppIdInput _appIdInput;

        [Header("_____________Basic Configuration_____________")]
        [FormerlySerializedAs("APP_ID")]
        [SerializeField]
        private string _appID = "";

        [FormerlySerializedAs("TOKEN")]
        [SerializeField]
        private string _token = "";

        [FormerlySerializedAs("CHANNEL_NAME")]
        [SerializeField]
        private string _channelName = "";

        public Text LogText;
        internal Logger Log;
        internal IRtcEngine RtcEngine = null;

        public Dropdown _videoDeviceSelect;
        private IVideoDeviceManager _videoDeviceManager;
        private DeviceInfo[] _videoDeviceInfos;
        public Dropdown _areaSelect;
        public GameObject _videoQualityItemPrefab;

        [SerializeField] private Text m_debugText;
        [SerializeField] private RawImage m_image;
        [SerializeField] private Button _takePhotoButton;
        [SerializeField] internal RawImage _receivedImage;
        internal  WebCamTexture _videoSource;
        private Texture2D _passthroughTexture;
        private Coroutine _pushFramesCoroutine;
        internal bool _joinedChannel = false;
        private int _dataStreamId = -1;

        [SerializeField] private WearHF wearHFManager;
        [SerializeField] private GameObject RealWearUI;

        // Track remote user UIDs for host selection
        private List<uint> remoteUserUids = new List<uint>();
        private uint myUid = 0; // Set in OnJoinChannelSuccess

        private void Start()
        {
            LoadAssetData();
            PrepareAreaList();
            if (CheckAppId())
            {
                RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();
                InitEngine();
                StartCoroutine(InitializeVideoSource());
                Invoke("JoinChannel", 3);
            }

#if UNITY_IOS || UNITY_ANDROID
            Invoke("JoinChannel", 3);
            m_image.gameObject.SetActive(true);
            var text = GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/VideoDeviceManager")?.GetComponent<Text>();
            if (text != null) text.text = "Video device manager not supported on this platform";
            GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/VideoDeviceButton")?.SetActive(false);
            GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/deviceIdSelect")?.SetActive(false);
            GameObject.Find("VideoCanvas/Scroll View/Viewport/Content/VideoSelectButton")?.SetActive(false);
#endif

            if (wearHFManager == null)
            {
                wearHFManager = GameObject.Find("WearHF Manager").GetComponent<WearHF>();
            }

            if (_takePhotoButton != null)
            {
                _takePhotoButton.onClick.AddListener(TakeAndSendPhoto);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsRealWearDevice())
            {
                if (_takePhotoButton != null) _takePhotoButton.gameObject.SetActive(false);
                if (_videoDeviceSelect != null) _videoDeviceSelect.gameObject.SetActive(false);
                if (_areaSelect != null) _areaSelect.gameObject.SetActive(false);

                Log.UpdateLog("Running on RealWear device. Voice commands enabled.");
            }
#endif
        }

        public void JoinCall()
        {
            m_image.gameObject.SetActive(true);
            RealWearUI.SetActive(false);
            Invoke("JoinChannel", 3);
        }

        private void VoiceCommandCallbackUI(string voiceCommand)
        {
            if (voiceCommand.Equals("Open UI", StringComparison.OrdinalIgnoreCase))
            {
                Log.UpdateLog("Voice command recognized: Take Photo");
                ActivateUI();
            }
        }
     
        void ActivateUI()
        {
            RealWearUI.SetActive(true);
        }
        private void VoiceCommandCallback(string voiceCommand)
        {
            if (voiceCommand.Equals("Take Photo", StringComparison.OrdinalIgnoreCase))
            {
                Log.UpdateLog("Voice command recognized: Take Photo");
                TakeAndSendPhoto();
            }
        }

        private bool IsRealWearDevice()
        {
            string deviceModel = SystemInfo.deviceModel.ToLower();
            return deviceModel.Contains("hmt-1") || deviceModel.Contains("navigator");
        }

        private IEnumerator ListenForVoiceCommands()
        {
            while (true)
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    Log.UpdateLog("Voice command: Take Photo");
                    TakeAndSendPhoto();
                }
                yield return null;
            }
        }

        private IEnumerator InitializeVideoSource()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsRealWearDevice())
            {
                if (WebCamTexture.devices.Length == 0)
                {
                    Log.UpdateLog("Error: No camera found on RealWear device.");
                    yield break;
                }

                _videoSource = new WebCamTexture(WebCamTexture.devices[0].name, 640, 360);
                _videoSource.Play();
                while (!_videoSource.isPlaying)
                {
                    Log.UpdateLog("Waiting for RealWear camera to start...");
                    yield return null;
                }

                Log.UpdateLog("RealWear camera initialized.");
            }
            else
            {
                Log.UpdateLog("Error: No WebCamTextureManager for Android (non-RealWear). Falling back to default camera.");
                if (WebCamTexture.devices.Length == 0)
                {
                    Log.UpdateLog("Error: No camera found on Android device.");
                    yield break;
                }

                _videoSource = new WebCamTexture(WebCamTexture.devices[0].name, 640, 360);
                _videoSource.Play();
                while (!_videoSource.isPlaying)
                {
                    Log.UpdateLog("Waiting for Android camera to start...");
                    yield return null;
                }

                Log.UpdateLog("Android camera initialized.");
            }
#else
            if (WebCamTexture.devices.Length == 0)
            {
                Log.UpdateLog("Error: No webcam devices found on Windows.");
                yield break;
            }

            _videoSource = new WebCamTexture(WebCamTexture.devices[0].name, 640, 360);
            _videoSource.Play();
            while (!_videoSource.isPlaying)
            {
                Log.UpdateLog("Waiting for Windows webcam to start...");
                yield return null;
            }

            Log.UpdateLog("Windows webcam initialized.");
#endif

            int width = _videoSource.width;
            int height = _videoSource.height;
            if (width <= 0 || height <= 0)
            {
                Log.UpdateLog($"Error: Invalid video dimensions ({width}x{height}).");
                yield break;
            }

            try
            {
                _passthroughTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                Log.UpdateLog($"Texture buffer created: {width}x{height}");
                m_image.texture = _videoSource;
                m_debugText.text += "\nVideo source ready and playing.";
            }
            catch (Exception e)
            {
                Log.UpdateLog($"Failed to create passthroughTexture: {e.Message}");
                yield break;
            }

            _pushFramesCoroutine = StartCoroutine(PushPassthroughFramesToAgora());
        }

        private IEnumerator PushPassthroughFramesToAgora()
        {
            if (RtcEngine == null || _passthroughTexture == null)
            {
                Log.UpdateLog("Error: RtcEngine or passthroughTexture is null.");
                yield break;
            }

            while (!_joinedChannel)
            {
                Log.UpdateLog("Waiting for channel join...");
                yield return null;
            }

            var options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(false);
            options.publishCustomVideoTrack.SetValue(true);
            int ret = RtcEngine.UpdateChannelMediaOptions(options);
            Log.UpdateLog($"UpdateChannelMediaOptions for custom video: {ret}");

            while (true)
            {
                if (_videoSource == null || !_videoSource.isPlaying)
                {
                    Log.UpdateLog("Error: Video source is null or not playing.");
                    yield break;
                }

                try
                {
                    _passthroughTexture.SetPixels(_videoSource.GetPixels());
                    _passthroughTexture.Apply();

                    byte[] frameData = _passthroughTexture.GetRawTextureData().ToArray();
                    if (frameData.Length == 0)
                    {
                        Log.UpdateLog("Error: Empty frame data.");
                        continue;
                    }

                    ExternalVideoFrame externalFrame = new ExternalVideoFrame
                    {
                        type = VIDEO_BUFFER_TYPE.VIDEO_BUFFER_RAW_DATA,
                        format = VIDEO_PIXEL_FORMAT.VIDEO_PIXEL_RGBA,
                        buffer = frameData,
                        stride = _passthroughTexture.width,
                        height = _passthroughTexture.height,
                        timestamp = (long)(Time.time * 1000)
                    };

                    Log.UpdateLog($"Pushing frame: width={_passthroughTexture.width}, height={_passthroughTexture.height}, stride={externalFrame.stride}, buffer size={frameData.Length}");
                    ret = RtcEngine.PushVideoFrame(externalFrame);
                    if (ret != 0)
                    {
                        Log.UpdateLog($"PushVideoFrame failed: {ret}");
                    }
                    else
                    {
                        Log.UpdateLog("PushVideoFrame succeeded");
                    }
                }
                catch (Exception e)
                {
                    Log.UpdateLog($"PushPassthroughFramesToAgora error: {e.Message}\nStack: {e.StackTrace}");
                    yield break;
                }

                yield return new WaitForSeconds(0.066f);
            }
        }

        private void Update()
        {
            PermissionHelper.RequestMicrophontPermission();
            PermissionHelper.RequestCameraPermission();
        }

        private void LoadAssetData()
        {
            if (_appIdInput == null) return;
            _appID = _appIdInput.appID;
            _token = _appIdInput.token;
            _channelName = _appIdInput.channelName;
        }

        private bool CheckAppId()
        {
            Log = new Logger(LogText);
            return Log.DebugAssert(_appID.Length > 10, "Please fill in your appId in API-Example/profile/appIdInput.asset");
        }

        private void PrepareAreaList()
        {
            int index = 0;
            var areaList = new List<Dropdown.OptionData>();
            var enumNames = Enum.GetNames(typeof(AREA_CODE));
            foreach (var name in enumNames)
            {
                areaList.Add(new Dropdown.OptionData(name));
                if (name == "AREA_CODE_GLOB") index = areaList.Count - 1;
            }
            _areaSelect.ClearOptions();
            _areaSelect.AddOptions(areaList);
            _areaSelect.value = index;
        }

        public void InitEngine()
        {
            var areaCode = (AREA_CODE)Enum.Parse(typeof(AREA_CODE), _areaSelect.captionText.text);
            Log.UpdateLog($"Select AREA_CODE: {areaCode}");

            var handler = new RealWearUserEventHandler(this);
            var context = new RtcEngineContext
            {
                appId = _appID,
                channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
                audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
                areaCode = areaCode
            };

            int result = RtcEngine.Initialize(context);
            Log.UpdateLog($"Initialize result: {result}");
            if (result != 0) return;

            RtcEngine.InitEventHandler(handler);
            RtcEngine.EnableAudio();
            RtcEngine.EnableVideo();
            RtcEngine.SetChannelProfile(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);
            RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

            var config = new VideoEncoderConfiguration
            {
                dimensions = new VideoDimensions(640, 360),
                frameRate = 15,
                bitrate = 400
            };
            RtcEngine.SetVideoEncoderConfiguration(config);

            DataStreamConfig streamConfig = new DataStreamConfig
            {
                syncWithAudio = false,
                ordered = true
            };
            int ret = RtcEngine.CreateDataStream(ref _dataStreamId, streamConfig);
            Log.UpdateLog($"Data stream created with ID: {_dataStreamId}, Result: {ret}");

            int build = 0;
            string version = RtcEngine.GetVersion(ref build);
            Log.UpdateLog($"Agora SDK Version: {version}, Build: {build}");
        }

        public void JoinChannel()
        {
            var options = new ChannelMediaOptions();
            options.publishCameraTrack.SetValue(false);
            options.publishCustomVideoTrack.SetValue(true);
            options.autoSubscribeVideo.SetValue(true);

            int ret = RtcEngine.JoinChannel(_token, _channelName, 0, options);
            Log.UpdateLog($"JoinChannel (with options) result: {ret}");
        }

        public void LeaveChannel()
        {
            RtcEngine.LeaveChannel();
            _joinedChannel = false;
        }

        public void StartPreview()
        {
            RtcEngine.StartPreview();
            var node = MakeVideoView(0);
            CreateLocalVideoCallQualityPanel(node);
        }

        public void StopPreview()
        {
            DestroyVideoView(0);
            RtcEngine.StopPreview();
        }

        public void StartPublish()
        {
            var options = new ChannelMediaOptions();
            options.publishMicrophoneTrack.SetValue(true);
            options.publishCustomVideoTrack.SetValue(true);
            int ret = RtcEngine.UpdateChannelMediaOptions(options);
            Log.UpdateLog($"StartPublish: {ret}");
        }

        public void StopPublish()
        {
            var options = new ChannelMediaOptions();
            options.publishMicrophoneTrack.SetValue(false);
            options.publishCustomVideoTrack.SetValue(false);
            int ret = RtcEngine.UpdateChannelMediaOptions(options);
            Log.UpdateLog($"StopPublish: {ret}");
        }

        public void AdjustVideoEncodedConfiguration640()
        {
            var config = new VideoEncoderConfiguration
            {
                dimensions = new VideoDimensions(640, 360),
                frameRate = 15,
                bitrate = 400
            };
            RtcEngine.SetVideoEncoderConfiguration(config);
        }

        public void AdjustVideoEncodedConfiguration480()
        {
            var config = new VideoEncoderConfiguration
            {
                dimensions = new VideoDimensions(480, 480),
                frameRate = 15,
                bitrate = 400
            };
            RtcEngine.SetVideoEncoderConfiguration(config);
        }

        public void GetVideoDeviceManager()
        {
#if !UNITY_IOS && !UNITY_ANDROID
            _videoDeviceSelect.ClearOptions();
            _videoDeviceManager = RtcEngine.GetVideoDeviceManager();
            _videoDeviceInfos = _videoDeviceManager.EnumerateVideoDevices();
            Log.UpdateLog($"VideoDeviceManager count: {_videoDeviceInfos.Length}");
            for (int i = 0; i < _videoDeviceInfos.Length; i++)
            {
                Log.UpdateLog($"Device {i}: {_videoDeviceInfos[i].deviceName}, ID: {_videoDeviceInfos[i].deviceId}");
            }
            _videoDeviceSelect.AddOptions(_videoDeviceInfos.Select(w => new Dropdown.OptionData($"{w.deviceName} :{w.deviceId}")).ToList());
#endif
        }

        public void SelectVideoCaptureDevice()
        {
#if !UNITY_IOS && !UNITY_ANDROID
            if (_videoDeviceSelect == null || _videoDeviceSelect.options.Count == 0) return;
            var option = _videoDeviceSelect.options[_videoDeviceSelect.value].text;
            var deviceId = option.Split(':')[1];
            int ret = _videoDeviceManager.SetDevice(deviceId);
            Log.UpdateLog($"SelectVideoCaptureDevice: {ret}, DeviceId: {deviceId}");

            if (_videoSource != null) _videoSource.Stop();
            _videoSource = new WebCamTexture(deviceId, 640, 360);
            _videoSource.Play();
            _passthroughTexture = new Texture2D(_videoSource.width, _videoSource.height, TextureFormat.RGBA32, false);
            m_image.texture = _videoSource;
#endif
        }

        private void TakeAndSendPhoto()
        {
            if (!_joinedChannel || _passthroughTexture == null || _dataStreamId < 0)
            {
                Log.UpdateLog("Cannot take photo: Not joined channel, no texture, or no data stream.");
                return;
            }

            try
            {
                Texture2D photo = new Texture2D(_passthroughTexture.width, _passthroughTexture.height, TextureFormat.RGBA32, false);
                photo.SetPixels(_passthroughTexture.GetPixels());
                photo.Apply();

                Texture2D resizedPhoto = ResizeTexture(photo, 320, 180);
                UnityEngine.Object.Destroy(photo);

                byte[] photoData = resizedPhoto.EncodeToJPG(50);
                UnityEngine.Object.Destroy(resizedPhoto);

                if (photoData.Length > 30000)
                {
                    Log.UpdateLog($"Photo data too large: {photoData.Length} bytes. Must be under 30 KB.");
                    return;
                }
                int ret = RtcEngine.SendStreamMessage(_dataStreamId, photoData, (uint)photoData.Length);
                Log.UpdateLog($"SendStreamMessage result: {ret}, Size: {photoData.Length} bytes");
            }
            catch (Exception e)
            {
                Log.UpdateLog($"TakeAndSendPhoto error: {e.Message}");
            }
        }

        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            try
            {
                RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
                RenderTexture.active = rt;
                Graphics.Blit(source, rt);
                Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
                result.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                return result;
            }
            catch (Exception e)
            {
                Log.UpdateLog($"Failed to resize texture: {e.Message}");
                return null;
            }
        }

        private void OnDestroy()
        {
            Log.UpdateLog("OnDestroy called");

            if (_pushFramesCoroutine != null)
            {
                StopCoroutine(_pushFramesCoroutine);
                _pushFramesCoroutine = null;
                Log.UpdateLog("PushPassthroughFramesToAgora coroutine stopped");
            }

            if (_videoSource != null && _videoSource.isPlaying)
            {
                _videoSource.Stop();
                Log.UpdateLog("Video source stopped");
            }

            if (_passthroughTexture != null)
            {
                UnityEngine.Object.Destroy(_passthroughTexture);
                _passthroughTexture = null;
                Log.UpdateLog("Passthrough texture destroyed");
            }

            if (RtcEngine != null)
            {
                RtcEngine.InitEventHandler(null);
                RtcEngine.LeaveChannel();
                RtcEngine.Dispose();
                RtcEngine = null;
                Log.UpdateLog("RtcEngine disposed");
            }

            if (_takePhotoButton != null)
            {
                _takePhotoButton.onClick.RemoveListener(TakeAndSendPhoto);
            }
        }

        internal string GetChannelName() => _channelName;

        internal static GameObject MakeVideoView(uint uid, string channelId = "")
        {
            var go = GameObject.Find(uid.ToString());
            if (go != null) return go;

            var videoSurface = MakeImageSurface(uid.ToString());
            if (videoSurface == null) return null;

            videoSurface.SetForUser(uid, channelId, uid == 0 ? VIDEO_SOURCE_TYPE.VIDEO_SOURCE_CAMERA : VIDEO_SOURCE_TYPE.VIDEO_SOURCE_REMOTE);
            videoSurface.OnTextureSizeModify += (width, height) =>
            {
                var transform = videoSurface.GetComponent<RectTransform>();
#if UNITY_ANDROID && !UNITY_EDITOR
                if (transform) transform.sizeDelta = new Vector2(640, 360);
#else
                if (transform) transform.sizeDelta = new Vector2(1450, 1080);
#endif
                Debug.Log($"OnTextureSizeModify: {width}x{height}");
            };
            videoSurface.SetEnable(true);
            return videoSurface.gameObject;
        }

        private static VideoSurface MakeImageSurface(string goName)
        {
            var go = new GameObject(goName);
            go.AddComponent<RawImage>();
            go.AddComponent<UIElementDrag>();
            var canvas = GameObject.Find("VideoCanvas");
            if (canvas != null) go.transform.parent = canvas.transform;
            go.transform.Rotate(0f, 0f, 0f);
            go.transform.localPosition = new Vector3(200, 0, 0);
            go.transform.localScale = new Vector3(1, 1, 1f);
            return go.AddComponent<VideoSurface>();
        }

        internal static void DestroyVideoView(uint uid)
        {
            var go = GameObject.Find(uid.ToString());
            if (go != null) UnityEngine.Object.Destroy(go);
        }

        public void CreateLocalVideoCallQualityPanel(GameObject parent)
        {
            if (parent.GetComponentInChildren<LocalVideoCallQualityPanel>() != null) return;
            var panel = Instantiate(_videoQualityItemPrefab, parent.transform);
            panel.AddComponent<LocalVideoCallQualityPanel>();
        }

        public LocalVideoCallQualityPanel GetLocalVideoCallQualityPanel()
        {
            var go = GameObject.Find("0");
            return go?.GetComponentInChildren<LocalVideoCallQualityPanel>();
        }

        public void CreateRemoteVideoCallQualityPanel(GameObject parent, uint uid)
        {
            if (parent.GetComponentInChildren<RemoteVideoCallQualityPanel>() != null) return;
            var panel = Instantiate(_videoQualityItemPrefab, parent.transform);
            panel.transform.localPosition = new Vector3(0, -182, 0);
            panel.transform.localScale = new Vector3(0, -1, 0);
            panel.transform.Rotate(0f, 0f, 0f);
            var comp = panel.AddComponent<RemoteVideoCallQualityPanel>();
            comp.Uid = uid;
        }

        public RemoteVideoCallQualityPanel GetRemoteVideoCallQualityPanel(uint uid)
        {
            var go = GameObject.Find(uid.ToString());
            return go?.GetComponentInChildren<RemoteVideoCallQualityPanel>();
        }

        // --- Host: Invite a remote user to join call ---
        public void InviteUserToCall(uint targetUid)
        {
            if (!_joinedChannel || _dataStreamId < 0)
            {
                Log.UpdateLog("Cannot send invite: Not joined channel or no data stream.");
                return;
            }

            var invite = new CallInviteMessage
            {
                targetUid = targetUid,
                channelName = _channelName
            };
            string json = JsonUtility.ToJson(invite);
            byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

            int ret = RtcEngine.SendStreamMessage(_dataStreamId, data, (uint)data.Length);
            Log.UpdateLog($"Sent invite to UID {targetUid}, result: {ret}");
        }

        // --- UI: Host selects a remote user (call this from your UI) ---
        public void OnInviteButtonClicked(uint remoteUid)
        {
            InviteUserToCall(remoteUid);
        }

        // --- Remote: Show invite notification and join ---
        public void ShowCallInvite(string channelName, uint hostUid)
        {
            Log.UpdateLog($"You are invited to join channel '{channelName}' by host {hostUid}.");
            // For demo, auto-join after 2 seconds:
            StartCoroutine(AutoJoinAfterDelay(channelName));
        }

        private IEnumerator AutoJoinAfterDelay(string channelName)
        {
            yield return new WaitForSeconds(2f);
            LeaveChannel();
            _channelName = channelName;
            JoinChannel();
        }

        // --- For UI: Get list of remote users (for host) ---
        public List<uint> GetRemoteUserUids()
        {
            return new List<uint>(remoteUserUids);
        }

        // --- For UI: Get my UID ---
        public uint GetMyUid()
        {
            return myUid;
        }

        // --- Called by event handler ---
        internal void SetMyUid(uint uid)
        {
            myUid = uid;
        }

        internal void AddRemoteUser(uint uid)
        {
            if (!remoteUserUids.Contains(uid))
                remoteUserUids.Add(uid);
        }

        internal void RemoveRemoteUser(uint uid)
        {
            remoteUserUids.Remove(uid);
        }
    }

    internal class RealWearUserEventHandler : IRtcEngineEventHandler
    {
        private readonly JoinChannelVideoWithRealWear _videoSample;

        internal RealWearUserEventHandler(JoinChannelVideoWithRealWear videoSample)
        {
            _videoSample = videoSample;
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            int build = 0;
            _videoSample.Log.UpdateLog($"SDK Version: {_videoSample.RtcEngine.GetVersion(ref build)}, Build: {build}");
            _videoSample.Log.UpdateLog($"OnJoinChannelSuccess: {connection.channelId}, UID: {connection.localUid}, Elapsed: {elapsed}");
            _videoSample._joinedChannel = true;
            _videoSample.SetMyUid(connection.localUid);
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            _videoSample.Log.UpdateLog($"OnUserJoined: UID: {uid}, Elapsed: {elapsed}");
            _videoSample.AddRemoteUser(uid);
            var node = JoinChannelVideoWithRealWear.MakeVideoView(uid, _videoSample.GetChannelName());
            _videoSample.CreateRemoteVideoCallQualityPanel(node, uid);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _videoSample.Log.UpdateLog($"OnUserOffline: UID: {uid}, Reason: {(int)reason}");
            _videoSample.RemoveRemoteUser(uid);
        }

        public override void OnRtcStats(RtcConnection connection, RtcStats stats)
        {
            var panel = _videoSample.GetLocalVideoCallQualityPanel();
            if (panel != null) { panel.Stats = stats; panel.RefreshPanel(); }
        }

        public override void OnLocalAudioStats(RtcConnection connection, LocalAudioStats stats)
        {
            var panel = _videoSample.GetLocalVideoCallQualityPanel();
            if (panel != null) { panel.AudioStats = stats; panel.RefreshPanel(); }
        }

        public override void OnLocalVideoStats(RtcConnection connection, LocalVideoStats stats)
        {
            var panel = _videoSample.GetLocalVideoCallQualityPanel();
            if (panel != null) { panel.VideoStats = stats; panel.RefreshPanel(); }
        }

        public override void OnRemoteVideoStats(RtcConnection connection, RemoteVideoStats stats)
        {
            var panel = _videoSample.GetRemoteVideoCallQualityPanel(stats.uid);
            if (panel != null) { panel.VideoStats = stats; panel.RefreshPanel(); }
        }

        public override void OnStreamMessage(RtcConnection connection, uint remoteUid, int streamId, byte[] data, ulong length, ulong sentTs)
        {
            try
            {
                _videoSample.Log.UpdateLog($"Received stream message from UID: {remoteUid}, Stream ID: {streamId}, Size: {length} bytes");

                // Try to parse as invite message
                string json = System.Text.Encoding.UTF8.GetString(data, 0, (int)length);
                CallInviteMessage invite = null;
                try
                {
                    invite = JsonUtility.FromJson<CallInviteMessage>(json);
                }
                catch { }

                if (invite != null && invite.type == "invite")
                {
                    uint myUid = _videoSample.GetMyUid();
                    if (invite.targetUid == myUid)
                    {
                        _videoSample.Log.UpdateLog($"Received call invite from host for channel: {invite.channelName}");
                        _videoSample.ShowCallInvite(invite.channelName, remoteUid);
                        return;
                    }
                }

                // Fallback: treat as photo
                Texture2D receivedTexture = new Texture2D(2, 2);
                if (receivedTexture.LoadImage(data))
                {
                    if (_videoSample._receivedImage != null)
                    {
                        _videoSample._receivedImage.texture = receivedTexture;
                        _videoSample._receivedImage.GetComponent<RectTransform>().sizeDelta = new Vector2(receivedTexture.width, receivedTexture.height);
                        _videoSample.Log.UpdateLog($"Displayed received photo: {receivedTexture.width}x{receivedTexture.height}");
                    }
                    else
                    {
                        _videoSample.Log.UpdateLog("Received photo but no RawImage assigned to display it.");
                        UnityEngine.Object.Destroy(receivedTexture);
                    }
                }
                else
                {
                    _videoSample.Log.UpdateLog("Failed to load received photo data.");
                    UnityEngine.Object.Destroy(receivedTexture);
                }
            }
            catch (Exception e)
            {
                _videoSample.Log.UpdateLog($"OnStreamMessage error: {e.Message}");
            }
        }
    }
}