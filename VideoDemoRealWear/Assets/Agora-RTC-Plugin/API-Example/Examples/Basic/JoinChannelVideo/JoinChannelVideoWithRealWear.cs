using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideo;
using io.agora.rtc.demo;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using WearHFPlugin;


namespace Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear
{
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

        [SerializeField] internal Text m_debugText;
        [SerializeField] private RawImage m_image;
        [SerializeField] private Button _takePhotoButton;
        [SerializeField] internal RawImage _receivedImage;
        internal WebCamTexture _videoSource;
        private Texture2D _passthroughTexture;
        private Coroutine _pushFramesCoroutine;
        internal bool _joinedChannel = false;
        internal int _dataStreamId = -1;

        [SerializeField] private WearHF wearHFManager;
        [SerializeField] private GameObject RealWearUI;

        [Header("Remote User Spawn Area")]
        [SerializeField] internal RectTransform remoteUserSpawnArea;
        internal Dictionary<uint, GameObject> _remoteUserViews = new Dictionary<uint, GameObject>();

        [Header("Remote User UI")]
        public GameObject remoteUserButtonPrefab;
        public Transform remoteUserButtonParent;
        public RawImage remoteUserVideoDisplay;

        private Dictionary<uint, GameObject> _remoteUserButtons = new Dictionary<uint, GameObject>();
        private Dictionary<uint, GameObject> _remoteUserVideoViews = new Dictionary<uint, GameObject>();
        public GameObject hostRemoteUserButtonPrefab;
        public GameObject clientRemoteUserButtonPrefab;
        internal uint _localUid = 0; // Store local UID after join


        // Add an array of English names for assignment
        private readonly string[] _englishNames = new string[]
        {
            "Alice", "Bob", "Charlie", "David", "Eve", "Frank", "Grace", "Helen", "Ivy", "Jack",
            "Karen", "Leo", "Mona", "Nina", "Oscar", "Paul", "Quinn", "Rose", "Sam", "Tina",
            "Uma", "Vera", "Will", "Xena", "Yuri", "Zara"
        };

        // Track which names are already assigned (uid -> name)
        private Dictionary<uint, string> _assignedNames = new Dictionary<uint, string>();
        // Track the client-side host video button
        private GameObject _clientHostButton;

        // Track the currently active remote user in call
        private uint? _activeRemoteUid = null;
        [SerializeField] private Button zoomInButton;
        [SerializeField] private Button zoomOutButton;
        private float currentZoom = 1.0f;
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
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Host (PC)
            RealWearUI.SetActive(true);
#endif
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
                wearHFManager = GameObject.Find("WearHF Manager")?.GetComponent<WearHF>();
            }

           
            if (zoomInButton != null) zoomInButton.onClick.AddListener(ZoomIn);
            if (zoomOutButton != null) zoomOutButton.onClick.AddListener(ZoomOut);
#if UNITY_ANDROID && !UNITY_EDITOR
            if (IsRealWearDevice())
            {
              //  if (_takePhotoButton != null) _takePhotoButton.gameObject.SetActive(false);
                if (_videoDeviceSelect != null) _videoDeviceSelect.gameObject.SetActive(false);
                if (_areaSelect != null) _areaSelect.gameObject.SetActive(false);

                Log.UpdateLog("Running on RealWear device. Voice commands enabled.");
            }
#endif
        }
        private void ZoomIn()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    if (RtcEngine != null && RtcEngine.IsCameraZoomSupported())
    {
        float maxZoom = RtcEngine.GetCameraMaxZoomFactor();
        currentZoom = Mathf.Min(currentZoom + 0.1f, maxZoom);
        RtcEngine.SetCameraZoomFactor(currentZoom);
        Log.UpdateLog($"Zoomed In: {currentZoom}");
    }
#endif
        }

        private void ZoomOut()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    if (RtcEngine != null && RtcEngine.IsCameraZoomSupported())
    {
        currentZoom = Mathf.Max(currentZoom - 0.1f, 1.0f);
        RtcEngine.SetCameraZoomFactor(currentZoom);
        Log.UpdateLog($"Zoomed Out: {currentZoom}");
    }
#endif
        }
        public void JoinCall()
        {
            m_image.gameObject.SetActive(true);
            RealWearUI.SetActive(false);
            Invoke("JoinChannel", 3);
        }

      

        private bool IsRealWearDevice()
        {
            string deviceModel = SystemInfo.deviceModel.ToLower();
            return deviceModel.Contains("hmt-1") || deviceModel.Contains("navigator");
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
            StartPublish();
            Log.UpdateLog("Set as BROADCASTER and started publishing");

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

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Host (PC)
            options.publishCameraTrack.SetValue(false);
            options.publishCustomVideoTrack.SetValue(false);
            options.publishMicrophoneTrack.SetValue(false);
            options.autoSubscribeVideo.SetValue(true);
            options.autoSubscribeAudio.SetValue(true);
            Log.UpdateLog("PC Host: Not publishing video/audio on join.");
#else
            // Remote (Android/RealWear)
            options.publishCameraTrack.SetValue(false);
            options.publishCustomVideoTrack.SetValue(false);
            options.publishMicrophoneTrack.SetValue(false);
            options.autoSubscribeVideo.SetValue(true);
            options.autoSubscribeAudio.SetValue(true);
            Log.UpdateLog("Remote: Subscribing only, not publishing video/audio.");
#endif

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

            foreach (var kvp in _remoteUserViews)
            {
                if (kvp.Value != null)
                    UnityEngine.Object.Destroy(kvp.Value);
            }
            _remoteUserViews.Clear();
        }
        internal string GetChannelName() => _channelName;
        private Vector2 GetRandomPositionInArea(RectTransform area, Vector2 objectSize)
        {
            Vector2 areaSize = area.rect.size;
            Vector2 anchorPos = area.anchoredPosition;

            // Calculate the min/max positions so the object stays fully inside the area
            float minX = -areaSize.x / 2f + objectSize.x / 2f;
            float maxX = areaSize.x / 2f - objectSize.x / 2f;
            float minY = -areaSize.y / 2f + objectSize.y / 2f;
            float maxY = areaSize.y / 2f - objectSize.y / 2f;

            float x = UnityEngine.Random.Range(minX, maxX);
            float y = UnityEngine.Random.Range(minY, maxY);
            return anchorPos + new Vector2(x, y);
        }
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
                if (transform) transform.sizeDelta = new Vector2(1450, 1080);
                transform.anchoredPosition = new Vector2(250.7889f, 5);
#else
                if (transform) transform.sizeDelta = new Vector2(1450, 1080);

                if(transform && width > 0 && height > 0)
                {
                    transform.anchoredPosition = new Vector2(250.7889f, 5);
                }
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
        // Only the host spawns remote user buttons and can start a call
        public void OnRemoteUserJoined(uint uid)
        {
            
            if (uid == _localUid || uid == 0)
                return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Only host spawns remote user buttons
            if (hostRemoteUserButtonPrefab != null && remoteUserButtonParent != null)
            {
                var buttonObj = Instantiate(hostRemoteUserButtonPrefab, remoteUserButtonParent);

                buttonObj.name = $"RemoteUserButton_{uid}";
                // Set random position
                if (remoteUserSpawnArea != null)
                {
                    var rect = buttonObj.GetComponent<RectTransform>();
                    if (rect != null)
                        rect.anchoredPosition = GetRandomPositionInArea(remoteUserSpawnArea, rect.sizeDelta);
                }

                // Assign a unique English name
                string assignedName = GetUniqueEnglishName(uid);
                var ui = buttonObj.GetComponent<HostRemoteUserUI>();
                if (ui != null)
                {
                    ui.SetUserName(assignedName);
                    ui.SetCallButtonText("Connect");
                    ui.SetCallButtonInteractable(true);
                    uint capturedUid = uid;
                    ui.callButton.onClick.AddListener(() => OnRemoteUserCallButtonClicked(capturedUid, ui));
                }

                _remoteUserButtons[uid] = buttonObj;
            }
#endif

            var videoView = MakeVideoView(uid, GetChannelName());
            if (videoView != null)
            {
                // Set random position for video view as well
                if (remoteUserSpawnArea != null)
                {
                    var rect = videoView.GetComponent<RectTransform>();
                    if (rect != null)
                        rect.anchoredPosition = GetRandomPositionInArea(remoteUserSpawnArea, rect.sizeDelta);
                }
                videoView.SetActive(false);
                _remoteUserVideoViews[uid] = videoView;
            }
        }
        // Helper to get a unique English name for a new user
        private string GetUniqueEnglishName(uint uid)
        {
            var usedNames = new HashSet<string>(_assignedNames.Values);
            string name = _englishNames.FirstOrDefault(n => !usedNames.Contains(n));
            if (string.IsNullOrEmpty(name))
            {
                name = $"User {uid}";
            }
            _assignedNames[uid] = name;
            return name;
        }

        // Host connects/disconnects call with remote user using the HostRemoteUserUI button
        public void OnRemoteUserCallButtonClicked(uint uid, HostRemoteUserUI ui)
        {
            // If already connected to this user, end the call
            if (_activeRemoteUid.HasValue && _activeRemoteUid.Value == uid)
            {
                EndCurrentCall();
                return;
            }

            // If another call is active, ignore
            if (_activeRemoteUid.HasValue && _activeRemoteUid.Value != uid)
                return;

            _activeRemoteUid = uid;

            Log.UpdateLog($"Host connected to remote user: {uid}");

            // Update button text and disable all other call buttons
            foreach (var kvp in _remoteUserButtons)
            {
                var uiComp = kvp.Value.GetComponent<HostRemoteUserUI>();
                if (uiComp != null)
                {
                    if (kvp.Key == uid)
                    {
                        uiComp.SetCallButtonInteractable(true);
                        uiComp.SetCallButtonText("Connected");
                    }
                    else
                    {
                        uiComp.SetCallButtonInteractable(false);
                        uiComp.SetCallButtonText("Connect");
                    }
                }
                var img = kvp.Value.GetComponent<RawImage>();
                if (img != null)
                    img.color = (kvp.Key == uid) ? Color.yellow : Color.white;
            }

            // Mute all other users, unmute only the selected one
            foreach (var kvp in _remoteUserVideoViews)
            {
                bool isSelected = kvp.Key == uid;
                RtcEngine.MuteRemoteAudioStream(kvp.Key, !isSelected);
                RtcEngine.MuteRemoteVideoStream(kvp.Key, !isSelected);
            }

            // Show only the selected user's video
            foreach (var videoObj in _remoteUserVideoViews.Values)
                videoObj.SetActive(false);

            if (_remoteUserVideoViews.TryGetValue(uid, out var videoView) && remoteUserVideoDisplay != null)
            {
                videoView.SetActive(true);
                var userRawImage = videoView.GetComponent<RawImage>();
                if (userRawImage != null)
                {
                    remoteUserVideoDisplay.texture = userRawImage.texture;
                    remoteUserVideoDisplay.enabled = true;
                }
            }

            Log.UpdateLog($"Started call with remote user: {uid}");

            // --- START PUBLISHING AUDIO/VIDEO ---
            StartPublish();

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_dataStreamId >= 0 && _joinedChannel)
            {
                // Send SHOW_HOST_VIDEO message with target UID
                string showHostMsg = $"SHOW_HOST_VIDEO:{_localUid}:{uid}";
                byte[] showHostBytes = System.Text.Encoding.UTF8.GetBytes(showHostMsg);
                RtcEngine.SendStreamMessage(_dataStreamId, showHostBytes, (uint)showHostBytes.Length);

                // Send HOST_BUSY message to all other clients
                foreach (var otherUid in _remoteUserButtons.Keys)
                {
                    if (otherUid != uid)
                    {
                        string busyMsg = $"HOST_BUSY:{otherUid}";
                        byte[] busyBytes = System.Text.Encoding.UTF8.GetBytes(busyMsg);
                        RtcEngine.SendStreamMessage(_dataStreamId, busyBytes, (uint)busyBytes.Length);
                    }
                }
                Log.UpdateLog($"Sent SHOW_HOST_VIDEO to {uid} and HOST_BUSY to others.");
            }
#endif
        }
        public void EndCurrentCall()
        {
            if (!_activeRemoteUid.HasValue)
                return;

            uint uid = _activeRemoteUid.Value;
            Log.UpdateLog($"Host ended call with remote user: {uid}");

            // Mute all remote users
            foreach (var kvp in _remoteUserVideoViews)
            {
                RtcEngine.MuteRemoteAudioStream(kvp.Key, true);
                RtcEngine.MuteRemoteVideoStream(kvp.Key, true);
            }

            // Hide all remote user videos
            foreach (var videoObj in _remoteUserVideoViews.Values)
                videoObj.SetActive(false);

            // Reset all call buttons
            foreach (var kvp in _remoteUserButtons)
            {
                var uiComp = kvp.Value.GetComponent<HostRemoteUserUI>();
                if (uiComp != null)
                {
                    uiComp.SetCallButtonInteractable(true);
                    uiComp.SetCallButtonText("Connect");
                }
                var img = kvp.Value.GetComponent<RawImage>();
                if (img != null)
                    img.color = Color.white;
            }

            // --- STOP PUBLISHING AUDIO/VIDEO ---
            StopPublish();

            // Clear active call state
            _activeRemoteUid = null;
        }
        // On the client, display the host's video in the main display (no button spawn)
        public void SpawnClientHostVideoPrefab(uint hostUid)
        {
#if !UNITY_STANDALONE_WIN && !UNITY_EDITOR_WIN
            if (remoteUserVideoDisplay != null)
            {
                var hostVideoView = MakeVideoView(hostUid, GetChannelName());
                if (hostVideoView != null)
                {
                    var hostRawImage = hostVideoView.GetComponent<RawImage>();
                    if (hostRawImage != null)
                    {
                        remoteUserVideoDisplay.texture = hostRawImage.texture;
                        remoteUserVideoDisplay.enabled = true;
                    }
                }
            }
#endif
        }
        public void OnRemoteUserLeft(uint uid)
        {
            // Destroy the host remote user button prefab if it exists
            if (_remoteUserButtons.TryGetValue(uid, out var buttonObj))
            {
                if (buttonObj != null)
                    Destroy(buttonObj);
                _remoteUserButtons.Remove(uid);
            }

            // Destroy the remote user's video view if it exists
            if (_remoteUserVideoViews.TryGetValue(uid, out var videoView))
            {
                if (videoView != null)
                    Destroy(videoView);
                _remoteUserVideoViews.Remove(uid);
            }

            // Remove the assigned name for this user
            _assignedNames.Remove(uid);

            // If the active user left, end the call and re-enable all buttons
            if (_activeRemoteUid.HasValue && _activeRemoteUid.Value == uid)
            {
                EndCurrentCall();
            }

            // Optionally, remove from _remoteUserViews if you use it for other UI
            if (_remoteUserViews.TryGetValue(uid, out var remoteView))
            {
                if (remoteView != null)
                    Destroy(remoteView);
                _remoteUserViews.Remove(uid);
            }
        }
    }
    internal class RealWearUserEventHandler : IRtcEngineEventHandler
    {
        private readonly JoinChannelVideoWithRealWear _videoSample;

        internal RealWearUserEventHandler(JoinChannelVideoWithRealWear videoSample)
        {
            _videoSample = videoSample;
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            if (connection.localUid != 0 && _videoSample._localUid == 0)
                _videoSample._localUid = connection.localUid;

            _videoSample.OnRemoteUserJoined(uid);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            _videoSample.OnRemoteUserLeft(uid);
        }
        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            int build = 0;
            _videoSample.Log.UpdateLog($"SDK Version: {_videoSample.RtcEngine.GetVersion(ref build)}, Build: {build}");
            _videoSample.Log.UpdateLog($"OnJoinChannelSuccess: {connection.channelId}, UID: {connection.localUid}, Elapsed: {elapsed}");
            _videoSample._joinedChannel = true;
            _videoSample._localUid = connection.localUid;

            // Create data stream here (after join)
            DataStreamConfig streamConfig = new DataStreamConfig
            {
                syncWithAudio = false,
                ordered = true
            };
            int ret = _videoSample.RtcEngine.CreateDataStream(ref _videoSample._dataStreamId, streamConfig);
            _videoSample.Log.UpdateLog($"Data stream created with ID: {_videoSample._dataStreamId}, Result: {ret}");
        }
        //public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        //{
        //    int build = 0;
        //    _videoSample.Log.UpdateLog($"SDK Version: {_videoSample.RtcEngine.GetVersion(ref build)}, Build: {build}");
        //    _videoSample.Log.UpdateLog($"OnJoinChannelSuccess: {connection.channelId}, UID: {connection.localUid}, Elapsed: {elapsed}");
        //    _videoSample._joinedChannel = true;
        //    _videoSample._localUid = connection.localUid;
        //}

        public override void OnError(int err, string msg) => _videoSample.Log.UpdateLog($"OnError: {err}, Msg: {msg}");

        public override void OnRejoinChannelSuccess(RtcConnection connection, int elapsed) => _videoSample.Log.UpdateLog("OnRejoinChannelSuccess");

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats) => _videoSample.Log.UpdateLog("OnLeaveChannel");

        public override void OnClientRoleChanged(RtcConnection connection, CLIENT_ROLE_TYPE oldRole, CLIENT_ROLE_TYPE newRole, ClientRoleOptions newRoleOptions) =>
            _videoSample.Log.UpdateLog("OnClientRoleChanged");

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
                string msg = System.Text.Encoding.UTF8.GetString(data, 0, (int)length);
                _videoSample.Log.UpdateLog($"Received stream message: {msg}");

                if (msg.StartsWith("SHOW_HOST_VIDEO:"))
                {
                    var parts = msg.Split(':');
                    if (parts.Length > 2 && uint.TryParse(parts[2], out uint targetUid))
                    {
                        if (targetUid != _videoSample._localUid)
                        {
                            // Not for this client, mute host and stop publishing audio
                            _videoSample.RtcEngine.MuteRemoteAudioStream(remoteUid, true);
                            _videoSample.RtcEngine.MuteRemoteVideoStream(remoteUid, true);
                            _videoSample.StopPublish(); // <--- Add this line
                            if (_videoSample.remoteUserVideoDisplay != null)
                            {
                                _videoSample.remoteUserVideoDisplay.texture = null;
                                _videoSample.remoteUserVideoDisplay.enabled = false;
                            }
                            return;
                        }
                        else
                        {
                            // For this client, unmute host and start publishing audio
                            _videoSample.RtcEngine.MuteRemoteAudioStream(remoteUid, false);
                            _videoSample.RtcEngine.MuteRemoteVideoStream(remoteUid, false);
                            _videoSample.StartPublish(); // <--- Add this line
                            _videoSample.SpawnClientHostVideoPrefab(remoteUid);
                        }
                    }
                    return;
                }
                else if (msg.StartsWith("HOST_BUSY:"))
                {
                    var parts = msg.Split(':');
                    if (parts.Length > 1 && uint.TryParse(parts[1], out uint targetUid))
                    {
                        if (targetUid != _videoSample._localUid)
                            return; // Not for this client
                    }
                    // Mute host for this client and stop publishing audio
                    _videoSample.RtcEngine.MuteRemoteAudioStream(remoteUid, true);
                    _videoSample.RtcEngine.MuteRemoteVideoStream(remoteUid, true);
                    _videoSample.StopPublish(); // <--- Add this line
                    if (_videoSample.remoteUserVideoDisplay != null)
                    {
                        _videoSample.remoteUserVideoDisplay.texture = null;
                        _videoSample.remoteUserVideoDisplay.enabled = false;
                    }
                    if (_videoSample.m_debugText != null)
                    {
                        _videoSample.m_debugText.text = "Host Is Busy";
                    }
                    return;
                }

                // ... existing photo receive logic ...
            }
            catch (Exception e)
            {
                _videoSample.Log.UpdateLog($"OnStreamMessage error: {e.Message}");
            }
        }

    }
}