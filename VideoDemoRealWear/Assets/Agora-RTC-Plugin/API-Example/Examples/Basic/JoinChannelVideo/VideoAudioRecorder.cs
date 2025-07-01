//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;
//using System;
//using System.Collections;
//using System.IO;
//using UnityEngine.Networking;
//using WearHFPlugin;
//using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
//#if VIDEOKIT_AVAILABLE
//using VideoKit;
//#endif

//public class RealWearVideoRecorder : MonoBehaviour
//{
//    private WearHF m_wearHf;
//    [SerializeField] private TextMeshProUGUI statusText;
//    [TextArea]
//    [SerializeField] private string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN"; // Replace with valid token
//    [SerializeField] private JoinChannelVideoWithRealWear joinChannelVideoWithRealWear;
//    [SerializeField] private Button startRecordButton; // UI button for Android/PC
//    [SerializeField] private Button stopRecordButton; // UI button for Android/PC
//    [SerializeField] private RawImage videoDisplay; // RawImage displaying WebCamTexture
//    [SerializeField] private int width = 854; // Recording resolution (optimized for RealWear)
//    [SerializeField] private int height = 480;
//    [SerializeField] private int frameRate = 30;

//    private bool isRecording = false;
//    private const string startVoiceCommand = "Start Recording";
//    private const string stopVoiceCommand = "Stop Recording";
//    private const string zoomInVoiceCommand = "Zoom In";
//    private const string zoomOutVoiceCommand = "Zoom Out";
//#if VIDEOKIT_AVAILABLE
//    private MediaRecorder recorder;
//    private RenderTexture renderTexture;
//#endif
//    private string videoPath;
//    private float zoomFactor = 1f; // 1x = no zoom, >1x = zoomed in
//    private const float zoomStep = 0.2f; // Zoom increment
//    private const float minZoom = 1f; // Minimum zoom
//    private const float maxZoom = 3f; // Maximum zoom

//    void Start()
//    {
//        // Initialize WearHF
//        GameObject wearHfObject = GameObject.Find("WearHF Manager");
//        if (wearHfObject == null)
//        {
//            Debug.LogError("WearHF Manager not found!");
//            UpdateStatusText("WearHF Manager not found!");
//            return;
//        }
//        m_wearHf = wearHfObject.GetComponent<WearHF>();
//        if (m_wearHf == null)
//        {
//            Debug.LogError("WearHF component not found!");
//            UpdateStatusText("WearHF component not found!");
//            return;
//        }

//        // Register voice commands
//        m_wearHf.AddVoiceCommand(startVoiceCommand, StartVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(stopVoiceCommand, StopVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(zoomInVoiceCommand, ZoomInVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(zoomOutVoiceCommand, ZoomOutVoiceCommandCallback);
//        Debug.Log($"Registered voice commands: {startVoiceCommand}, {stopVoiceCommand}, {zoomInVoiceCommand}, {zoomOutVoiceCommand}");

//        // Initialize UI buttons
//        if (startRecordButton != null)
//            startRecordButton.onClick.AddListener(() => StartRecording());
//        if (stopRecordButton != null)
//            stopRecordButton.onClick.AddListener(StopRecording);

//        // Ensure status text is initially hidden
//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(false);
//        }

//        // Request permissions for Android/RealWear
//#if UNITY_ANDROID
//        StartCoroutine(RequestPermissions());
//#endif

//        // Update button states
//        UpdateButtonStates();
//    }

//    private IEnumerator RequestPermissions()
//    {
//        // Request camera and microphone permissions
//        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
//            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
//        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
//            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

//        // Request storage permissions
//#if UNITY_ANDROID
//        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageWrite))
//            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageWrite);
//        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.ExternalStorageRead))
//            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.ExternalStorageRead);
//#endif
//    }

//    private void StartVoiceCommandCallback(string voiceCommand)
//    {
//        Debug.Log($"Voice command triggered: {voiceCommand}");
//        StartRecording();
//    }

//    private void StopVoiceCommandCallback(string voiceCommand)
//    {
//        Debug.Log($"Voice command triggered: {voiceCommand}");
//        StopRecording();
//    }

//    private void ZoomInVoiceCommandCallback(string voiceCommand)
//    {
//        Debug.Log($"Voice command triggered: {voiceCommand}");
//        ZoomIn();
//    }

//    private void ZoomOutVoiceCommandCallback(string voiceCommand)
//    {
//        Debug.Log($"Voice command triggered: {voiceCommand}");
//        ZoomOut();
//    }

//    void Update()
//    {
//        // PC keyboard input
//        if (Input.GetKeyDown(KeyCode.Space) && !isRecording)
//            StartRecording();
//        if (Input.GetKeyDown(KeyCode.S) && isRecording)
//            StopRecording();
//        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.Equals))
//            ZoomIn();
//        if (Input.GetKeyDown(KeyCode.Minus))
//            ZoomOut();
//    }

//    private void ZoomIn()
//    {
//        zoomFactor += zoomStep;
//        zoomFactor = Mathf.Clamp(zoomFactor, minZoom, maxZoom);
//        UpdateZoom();
//        UpdateStatusText($"Zoom: {zoomFactor:F1}x");
//    }

//    private void ZoomOut()
//    {
//        zoomFactor -= zoomStep;
//        zoomFactor = Mathf.Clamp(zoomFactor, minZoom, maxZoom);
//        UpdateZoom();
//        UpdateStatusText($"Zoom: {zoomFactor:F1}x");
//    }

//    private void UpdateZoom()
//    {
//        if (videoDisplay != null)
//        {
//            videoDisplay.transform.localScale = new Vector3(zoomFactor, zoomFactor, 1f);
//        }
//    }

//    public async void StartRecording()
//    {
//#if VIDEOKIT_AVAILABLE
//        if (isRecording)
//        {
//            Debug.LogWarning("Recording in progress, ignoring new request.");
//            return;
//        }

//        // Validate video source
//        if (joinChannelVideoWithRealWear == null || joinChannelVideoWithRealWear._videoSource == null)
//        {
//            UpdateStatusText("Video source is not initialized.");
//            return;
//        }

//        WebCamTexture videoSource = joinChannelVideoWithRealWear._videoSource as WebCamTexture;
//        if (videoSource == null || !videoSource.isPlaying)
//        {
//            UpdateStatusText("Camera is not running.");
//            return;
//        }

//        // Create RenderTexture
//        renderTexture = new RenderTexture(width, height, 24);
//        renderTexture.Create();

//        // Initialize VideoKit recorder
//        string tempPath = Path.Combine(Application.persistentDataPath, $"TempVideo_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
//        recorder = await MediaRecorder.Create(MediaRecorder.Format.MP4, width, height, frameRate, 4000000); // 4Mbps bitrate
//        if (recorder == null)
//        {
//            UpdateStatusText("Failed to initialize recorder.");
//            Debug.LogError("MediaRecorder creation failed.");
//            renderTexture.Release();
//            Destroy(renderTexture);
//            return;
//        }

//        isRecording = true;
//        m_wearHf.ClearCommands();
//        m_wearHf.AddVoiceCommand(stopVoiceCommand, StopVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(zoomInVoiceCommand, ZoomInVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(zoomOutVoiceCommand, ZoomOutVoiceCommandCallback);
//        StartCoroutine(RecordFrames(videoSource));
//        UpdateStatusText("Recording started");
//        Debug.Log("Recording started");
//#else
//        Debug.LogError("VideoKit is not available. Please install VideoKit via NatML registry.");
//        UpdateStatusText("VideoKit not installed.");
//#endif

//        // Update UI
//        UpdateButtonStates();
//    }

//    private IEnumerator RecordFrames(WebCamTexture videoSource)
//    {
//#if VIDEOKIT_AVAILABLE
//        while (isRecording && recorder != null)
//        {
//            // Blit WebCamTexture to RenderTexture
//            Graphics.Blit(videoSource, renderTexture);

//            // Commit frame
//            recorder.Commit(renderTexture, Time.realtimeSinceStartup * 1000000000L); // Timestamp in nanoseconds

//            yield return new WaitForEndOfFrame();
//        }
//#else
//        yield break;
//#endif
//    }

//    public async void StopRecording()
//    {
//#if VIDEOKIT_AVAILABLE
//        if (!isRecording || recorder == null)
//        {
//            Debug.LogWarning("No active recording to stop.");
//            return;
//        }

//        isRecording = false;

//        // Finalize recording and get video path
//        MediaAsset mediaAsset = await recorder.FinishWriting();
//        videoPath = mediaAsset.path;
//        recorder = null;

//        // Clean up RenderTexture
//        if (renderTexture != null)
//        {
//            renderTexture.Release();
//            Destroy(renderTexture);
//        }

//        // Generate final video path
//        string finalPath = Path.Combine(Application.persistentDataPath, $"Video_{DateTime.Now:yyyyMMdd_HHmmss}.mp4");
//        try
//        {
//            File.Move(videoPath, finalPath);
//            videoPath = finalPath;
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to rename video file: {e.Message}");
//            UpdateStatusText("Failed to rename video file.");
//            return;
//        }

//        Debug.Log($"Video saved to: {videoPath}");
//        UpdateStatusText($"Video saved to: {videoPath}");

//        // Upload to Dropbox
//        StartCoroutine(UploadToDropbox(videoPath));

//        // Re-register voice commands
//        m_wearHf.ClearCommands();
//        m_wearHf.AddVoiceCommand(startVoiceCommand, StartVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(stopVoiceCommand, StopVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(zoomInVoiceCommand, ZoomInVoiceCommandCallback);
//        m_wearHf.AddVoiceCommand(zoomOutVoiceCommand, ZoomOutVoiceCommandCallback);
//#else
//        Debug.LogError("VideoKit is not available. Cannot stop recording.");
//        UpdateStatusText("VideoKit not installed.");
//#endif

//        // Update UI
//        UpdateButtonStates();
//    }

//    private IEnumerator UploadToDropbox(string filePath)
//    {
//#if VIDEOKIT_AVAILABLE
//        if (!File.Exists(filePath))
//        {
//            Debug.LogError($"Video file not found: {filePath}");
//            UpdateStatusText("Video file not found.");
//            yield break;
//        }

//        byte[] fileData = null;
//        try
//        {
//            fileData = File.ReadAllBytes(filePath);
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to read video file: {e.Message}");
//            UpdateStatusText("Failed to read video file.");
//            yield break;
//        }

//        string filename = Path.GetFileName(filePath);
//        UpdateStatusText("Uploading video...");

//        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
//        www.uploadHandler = new UploadHandlerRaw(fileData);
//        www.downloadHandler = new DownloadHandlerBuffer();

//        // Set headers
//        www.SetRequestHeader("Authorization", $"Bearer {dropboxAccessToken}");
//        www.SetRequestHeader("Content-Type", "application/octet-stream");
//        www.SetRequestHeader("Dropbox-API-Arg", $"{{\"path\": \"/{filename}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}}");

//        // Send request
//        yield return www.SendWebRequest();

//        // Handle result
//        if (www.result == UnityWebRequest.Result.Success)
//        {
//            Debug.Log($"Upload successful: {www.downloadHandler.text}");
//            UpdateStatusText("Upload successful!");
//        }
//        else
//        {
//            Debug.LogError($"Upload failed: {www.error}\nResponse: {www.downloadHandler.text}");
//            UpdateStatusText($"Upload failed: {www.error}");
//        }

//        www.Dispose();

//        // Delete local file
//        try
//        {
//            File.Delete(filePath);
//            Debug.Log($"Deleted local video file: {filePath}");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to delete video file: {e.Message}");
//        }
//#else
//        Debug.LogError("VideoKit is not available. Cannot upload video.");
//        UpdateStatusText("VideoKit not installed.");
//        yield break;
//#endif
//    }

//    private void UpdateButtonStates()
//    {
//        if (startRecordButton != null)
//            startRecordButton.interactable = !isRecording;
//        if (stopRecordButton != null)
//            stopRecordButton.interactable = isRecording;
//    }

//    private void UpdateStatusText(string message)
//    {
//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(true);
//            statusText.text = message;
//            statusText.fontSize = 24; // Readable on RealWear
//            Debug.Log($"Status: {message}");
//        }
//        else
//        {
//            Debug.LogWarning("StatusText is not assigned!");
//        }
//    }

//    private void OnDestroy()
//    {
//        if (m_wearHf != null)
//        {
//            m_wearHf.ClearCommands();
//        }
//#if VIDEOKIT_AVAILABLE
//        if (recorder != null)
//        {
//            recorder.FinishWriting().GetAwaiter().GetResult(); // Synchronous cleanup
//            recorder = null;
//        }
//        if (renderTexture != null)
//        {
//            renderTexture.Release();
//            Destroy(renderTexture);
//        }
//#endif
//    }
//}


