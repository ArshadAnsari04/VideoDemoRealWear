
//using FFmpegUnityBind2;
//using FFmpegUnityBind2.Android;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using TMPro;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.UI;
//using WearHFPlugin;
//using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;

//public class VideoRecorder : MonoBehaviour, IFFmpegCallbacksHandler
//{
//    [Header("References")]
//    public RawImage sourceRawImage; // Local RawImage (e.g., WebCamTexture on Android)
//    public TextMeshProUGUI statusText;
//    public Button startButton;
//    public Button stopButton;
//    public Slider encodingProgressBar;
//    public WearHF wearHf;
//    public JoinChannelVideoWithRealWear agoraManager; // Reference to Agora script

//    [Header("Dropbox")]
//    [TextArea]
//    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";

//    [Header("Recording Settings")]
//    public int frameRate = 24;
//    public int width = 640;
//    public int height = 360;

//    private List<string> capturedFrames = new List<string>();
//    private bool isRecording = false;
//    private string outputFolder;
//    private string videoFilePath;
//    private long ffmpegExecutionId;
//    private bool ffmpegSuccess;
//    private string ffmpegLog = "";
//    private const string FramePattern = "frame_{0:D04}.jpg";
//    private const string FrameGlobPattern = "frame_%04d.jpg";
//    private const int MaxSaveAttempts = 3;
//    private const string voiceCommandStart = "Start Recording";
//    private const string voiceCommandStop = "Stop Recording";
//    private float encodingProgress = 0f;

//    private void Start()
//    {
//        // Initialize buttons
//        if (startButton != null)
//            startButton.onClick.AddListener(StartRecording);
//        else
//            UnityEngine.Debug.LogError("Start Button not assigned!");

//        if (stopButton != null)
//        {
//            stopButton.onClick.AddListener(StopRecording);
//            stopButton.gameObject.SetActive(false);
//        }
//        else
//            UnityEngine.Debug.LogError("Stop Button not assigned!");

//        // Initialize progress bar
//        if (encodingProgressBar != null)
//        {
//            encodingProgressBar.gameObject.SetActive(false);
//            encodingProgressBar.value = 0f;
//        }

//        // Initialize WearHF
//        GameObject wearHfObject = GameObject.Find("WearHF Manager");
//        if (wearHfObject != null)
//        {
//            wearHf = wearHfObject.GetComponent<WearHF>();
//            if (wearHf != null)
//            {
//                wearHf.AddVoiceCommand(voiceCommandStart, (cmd) => StartRecording());
//                wearHf.AddVoiceCommand(voiceCommandStop, (cmd) => StopRecording());
//                UnityEngine.Debug.Log($"Registered voice commands: {voiceCommandStart}, {voiceCommandStop}");
//            }
//            else
//                UnityEngine.Debug.LogError("WearHF component not found!");
//        }
//        else
//            UnityEngine.Debug.LogError("WearHF Manager not found!");

//        // Initialize Agora manager
//        if (agoraManager == null)
//        {
//            agoraManager = FindObjectOfType<JoinChannelVideoWithRealWear>();
//            if (agoraManager == null)
//                UnityEngine.Debug.LogError("JoinChannelVideoWithRealWear component not found!");
//        }

//        // Ensure status text is readable
//        if (statusText != null)
//        {
//            statusText.fontSize = 24;
//            statusText.gameObject.SetActive(false);
//        }
//    }

//    public void StartRecording()
//    {
//        if (isRecording) return;

//        Texture sourceTexture = GetRecordingTexture();
//        if (sourceTexture == null)
//        {
//            UpdateStatus("No valid texture available for recording!");
//            return;
//        }

//        isRecording = true;
//        outputFolder = GetRecordingOutputFolder();
//        if (!Directory.Exists(outputFolder))
//            Directory.CreateDirectory(outputFolder);

//        capturedFrames.Clear();
//        encodingProgress = 0f;
//        if (encodingProgressBar != null)
//            encodingProgressBar.value = 0f;

//        startButton.gameObject.SetActive(false);
//        stopButton.gameObject.SetActive(true);
//        if (wearHf != null)
//            wearHf.ClearCommands();
//        wearHf.AddVoiceCommand(voiceCommandStop, (cmd) => StopRecording());

//        StartCoroutine(CaptureFrames());
//        UpdateStatus("Recording started...");
//    }

//    private Texture GetRecordingTexture()
//    {
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN
//        // On Windows/Editor, prefer remote user's RawImage if active
//        if (agoraManager != null)
//        {
//            if (agoraManager._activeRemoteUid.HasValue)
//            {
//                uint activeUid = agoraManager._activeRemoteUid.Value;
//                if (agoraManager._remoteUserVideoViews.TryGetValue(activeUid, out GameObject videoView))
//                {
//                    RawImage remoteRawImage = videoView?.GetComponent<RawImage>();
//                    if (remoteRawImage != null && remoteRawImage.enabled && remoteRawImage.texture != null)
//                    {
//                        Texture texture = remoteRawImage.texture;
//                        if (texture.width > 0 && texture.height > 0)
//                        {
//                            UnityEngine.Debug.Log($"Recording remote user video (UID: {activeUid}, {texture.width}x{texture.height})");
//                            return texture;
//                        }
//                        else
//                        {
//                            UnityEngine.Debug.LogWarning($"Remote texture invalid (UID: {activeUid}, width: {texture.width}, height: {texture.height})");
//                        }
//                    }
//                    else
//                    {
//                        UnityEngine.Debug.LogWarning($"Remote RawImage invalid (UID: {activeUid}, enabled: {remoteRawImage?.enabled}, texture: {remoteRawImage?.texture})");
//                    }
//                }
//                else
//                {
//                    UnityEngine.Debug.LogWarning($"No video view for active remote UID: {activeUid}");
//                }
//            }
//            else
//            {
//                UnityEngine.Debug.LogWarning("No active remote user");
//            }
//        }
//        else
//        {
//            UnityEngine.Debug.LogWarning("Agora manager not assigned");
//        }
//        UnityEngine.Debug.Log("Falling back to local sourceRawImage");
//#endif

//        // Fallback to local sourceRawImage
//        if (sourceRawImage != null && sourceRawImage.texture != null && sourceRawImage.texture.width > 0 && sourceRawImage.texture.height > 0)
//        {
//            UnityEngine.Debug.Log($"Recording local sourceRawImage ({sourceRawImage.texture.width}x{sourceRawImage.texture.height})");
//            return sourceRawImage.texture;
//        }

//        UnityEngine.Debug.LogError("No valid local sourceRawImage texture");
//        return null;
//    }

//    public void StopRecording()
//    {
//        if (!isRecording) return;
//        isRecording = false;
//        StopAllCoroutines();
//        StartCoroutine(EncodeAndUpload());

//        startButton.gameObject.SetActive(true);
//        stopButton.gameObject.SetActive(false);
//        if (wearHf != null)
//        {
//            wearHf.ClearCommands();
//            wearHf.AddVoiceCommand(voiceCommandStart, (cmd) => StartRecording());
//        }
//    }

//    public void CancelRecording()
//    {
//        if (!isRecording) return;
//        isRecording = false;
//        StopAllCoroutines();
//        if (ffmpegExecutionId != 0)
//        {
//            FFmpegAndroid.Cancel(ffmpegExecutionId);
//            ffmpegExecutionId = 0;
//        }

//        foreach (var file in capturedFrames)
//        {
//            if (File.Exists(file)) File.Delete(file);
//        }
//        capturedFrames.Clear();

//        startButton.gameObject.SetActive(true);
//        stopButton.gameObject.SetActive(false);
//        if (wearHf != null)
//        {
//            wearHf.ClearCommands();
//            wearHf.AddVoiceCommand(voiceCommandStart, (cmd) => StartRecording());
//        }

//        UpdateStatus("Recording canceled.");
//    }

//    private IEnumerator CaptureFrames()
//    {
//        float interval = 1f / frameRate;
//        int frameCount = 0;
//        RenderTexture bufferRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

//        while (isRecording)
//        {
//            yield return new WaitForEndOfFrame();

//            Texture srcTexture = GetRecordingTexture();
//            if (srcTexture == null)
//            {
//                UpdateStatus("Texture lost during recording.");
//                Destroy(bufferRT);
//                yield break;
//            }

//            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

//            if (srcTexture is RenderTexture rt)
//            {
//                Graphics.Blit(rt, bufferRT);
//                RenderTexture.active = bufferRT;
//                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//                tex.Apply();
//            }
//            else if (srcTexture is Texture2D t2d)
//            {
//                Graphics.Blit(t2d, bufferRT);
//                RenderTexture.active = bufferRT;
//                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//                tex.Apply();
//            }
//            else if (srcTexture is WebCamTexture wct)
//            {
//                Graphics.Blit(wct, bufferRT);
//                RenderTexture.active = bufferRT;
//                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//                tex.Apply();
//            }
//            else
//            {
//                UpdateStatus("Unsupported texture type.");
//                Destroy(tex);
//                Destroy(bufferRT);
//                yield break;
//            }

//            string framePath = Path.Combine(outputFolder, string.Format(FramePattern, frameCount));
//            byte[] jpgData = tex.EncodeToJPG(75);
//            int attempts = 0;

//            while (attempts < MaxSaveAttempts)
//            {
//                try
//                {
//                    File.WriteAllBytes(framePath, jpgData);
//                    break;
//                }
//                catch (Exception ex)
//                {
//                    UnityEngine.Debug.LogError($"Frame {frameCount} save attempt {attempts + 1} failed: {ex.Message}");
//                }
//                attempts++;
//                yield return new WaitForSeconds(0.1f);
//            }

//            capturedFrames.Add(framePath);
//            long fileSize = File.Exists(framePath) ? new FileInfo(framePath).Length : 0;
//            if (!File.Exists(framePath) || fileSize < 100)
//                UpdateStatus($"Warning: Frame {frameCount} not saved correctly: {framePath}");

//            Destroy(tex);
//            frameCount++;
//            yield return new WaitForSeconds(interval);
//        }

//        Destroy(bufferRT);
//    }

//    private IEnumerator EncodeAndUpload()
//    {
//        UpdateStatus("Encoding video...");
//        if (encodingProgressBar != null)
//        {
//            encodingProgressBar.gameObject.SetActive(true);
//            encodingProgressBar.value = 0f;
//        }

//        videoFilePath = GetRecordingOutputPath();
//        ffmpegSuccess = false;
//        ffmpegLog = "";

//        string inputPattern = Path.Combine(outputFolder, FrameGlobPattern).Replace("\\", "/");
//        string outputPath = videoFilePath.Replace("\\", "/");

//        if (capturedFrames.Count == 0)
//        {
//            UpdateStatus("No frames captured for encoding.");
//            yield break;
//        }

//        foreach (var frame in capturedFrames)
//        {
//            if (!File.Exists(frame))
//            {
//                UpdateStatus($"Frame missing: {frame}");
//                UnityEngine.Debug.LogError($"Frame missing: {frame}");
//                yield break;
//            }
//        }

//#if UNITY_EDITOR || UNITY_STANDALONE_WIN
//        string ffmpegPath = GetFFmpegPath();
//        UnityEngine.Debug.Log($"FFmpeg path: {ffmpegPath}");
//        if (!File.Exists(ffmpegPath))
//        {
//            UpdateStatus($"FFmpeg not found at: {ffmpegPath}");
//            yield break;
//        }

//        Process ffmpeg = new Process();
//        ffmpeg.StartInfo.FileName = ffmpegPath;
//        ffmpeg.StartInfo.Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 5 -pix_fmt yuv420p -vf fps={frameRate} \"{outputPath}\"";
//        ffmpeg.StartInfo.CreateNoWindow = true;
//        ffmpeg.StartInfo.UseShellExecute = false;
//        ffmpeg.StartInfo.RedirectStandardOutput = true;
//        ffmpeg.StartInfo.RedirectStandardError = true;

//        UnityEngine.Debug.Log($"FFmpeg command: {ffmpeg.StartInfo.Arguments}");

//        string outputLog = "";
//        ffmpeg.OutputDataReceived += (sender, args) =>
//        {
//            if (args.Data != null)
//            {
//                outputLog += args.Data + "\n";
//                UpdateProgressFromOutput(args.Data);
//            }
//        };
//        ffmpeg.ErrorDataReceived += (sender, args) =>
//        {
//            if (args.Data != null)
//            {
//                outputLog += args.Data + "\n";
//                UpdateProgressFromOutput(args.Data);
//            }
//        };

//        bool startedSuccessfully = false;
//        try
//        {
//            startedSuccessfully = ffmpeg.Start();
//            ffmpeg.BeginOutputReadLine();
//            ffmpeg.BeginErrorReadLine();
//        }
//        catch (Exception ex)
//        {
//            ffmpegLog = $"FFmpeg execution error: {ex.Message}";
//            UpdateStatus(ffmpegLog);
//            UnityEngine.Debug.LogError(ffmpegLog);
//            ffmpegSuccess = false;
//            yield break;
//        }

//        if (startedSuccessfully)
//        {
//            float elapsed = 0f;
//            while (!ffmpeg.HasExited)
//            {
//                elapsed += 0.5f;
//                if (encodingProgressBar != null)
//                    encodingProgressBar.value = encodingProgress;
//                yield return new WaitForSeconds(0.5f);
//            }

//            ffmpeg.WaitForExit();
//            ffmpegLog = outputLog;
//            ffmpegSuccess = File.Exists(videoFilePath) && ffmpeg.ExitCode == 0;

//            if (!ffmpegSuccess)
//            {
//                UpdateStatus("FFmpeg encoding failed: " + ffmpegLog);
//                UnityEngine.Debug.LogError("FFmpeg encoding failed: " + ffmpegLog);
//            }
//            else
//            {
//                UpdateStatus("FFmpeg encoding successful");
//            }
//        }
//        else
//        {
//            ffmpegLog = "FFmpeg failed to start.";
//            UpdateStatus(ffmpegLog);
//            UnityEngine.Debug.LogError(ffmpegLog);
//            ffmpegSuccess = false;
//        }
//#elif UNITY_ANDROID
//        string command = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 5 -pix_fmt yuv420p -vf fps={frameRate} -threads 1 \"{outputPath}\"";
//        UnityEngine.Debug.Log($"FFmpeg Android command: {command}");
//        try
//        {
//            List<IFFmpegCallbacksHandler> handlers = new List<IFFmpegCallbacksHandler> { this };
//            ffmpegExecutionId = FFmpegAndroid.Execute(command, handlers);
//        }
//        catch (Exception ex)
//        {
//            ffmpegLog = $"FFmpeg error: {ex.Message}";
//            UpdateStatus(ffmpegLog);
//            UnityEngine.Debug.LogError(ffmpegLog);
//            ffmpegSuccess = false;
//            yield break;
//        }

//        float timeout = 120f;
//        float timer = 0f;
//        while (ffmpegExecutionId != 0 && timer < timeout)
//        {
//            if (encodingProgressBar != null)
//                encodingProgressBar.value = encodingProgress;
//            yield return new WaitForSeconds(0.5f);
//            timer += 0.5f;
//        }

//        ffmpegSuccess = File.Exists(videoFilePath);
//        if (!ffmpegSuccess)
//        {
//            UpdateStatus("Encoding failed: " + ffmpegLog);
//            UnityEngine.Debug.LogError("Encoding failed: " + ffmpegLog);
//        }
//#else
//        UpdateStatus("Encoding not supported on this platform.");
//        yield break;
//#endif

//        // Clean up frames
//        foreach (var file in capturedFrames)
//        {
//            if (File.Exists(file)) File.Delete(file);
//        }
//        capturedFrames.Clear();

//        if (encodingProgressBar != null)
//            encodingProgressBar.gameObject.SetActive(false);

//        if (ffmpegSuccess)
//        {
//            UpdateStatus("Encoding complete. Uploading to Dropbox...");
//            yield return StartCoroutine(UploadToDropboxCoroutine(videoFilePath));
//        }
//    }

//    private void UpdateProgressFromOutput(string output)
//    {
//        if (string.IsNullOrEmpty(output) || !output.Contains("frame=")) return;
//        try
//        {
//            int frameIndex = output.IndexOf("frame=");
//            string frameStr = output.Substring(frameIndex + 6).Split(' ')[0];
//            if (int.TryParse(frameStr, out int currentFrame) && capturedFrames.Count > 0)
//            {
//                encodingProgress = Mathf.Clamp01((float)currentFrame / capturedFrames.Count);
//                UnityEngine.Debug.Log($"Progress updated: {encodingProgress:P0} ({currentFrame}/{capturedFrames.Count})");
//            }
//        }
//        catch (Exception ex)
//        {
//            UnityEngine.Debug.LogWarning($"Failed to parse progress: {ex.Message}");
//        }
//    }

//    private string GetRecordingOutputFolder()
//    {
//        string folderPath = Path.Combine(Application.persistentDataPath, "Recordings");
//        if (!Directory.Exists(folderPath))
//            Directory.CreateDirectory(folderPath);
//        return folderPath;
//    }

//    private string GetRecordingOutputPath()
//    {
//        string folder = GetRecordingOutputFolder();
//        string fileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
//        return Path.Combine(folder, fileName);
//    }

//    private string GetFFmpegPath()
//    {
//        string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "Desktop/Win/ffmpeg.exe");
//        return ffmpegPath;
//    }

//    private IEnumerator UploadToDropboxCoroutine(string filePath)
//    {
//        if (!File.Exists(filePath))
//        {
//            UpdateStatus("File not found for upload: " + filePath);
//            yield break;
//        }

//        byte[] fileData = File.ReadAllBytes(filePath);
//        string fileName = Path.GetFileName(filePath);

//        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
//        www.uploadHandler = new UploadHandlerRaw(fileData);
//        www.downloadHandler = new DownloadHandlerBuffer();

//        www.SetRequestHeader("Authorization", $"Bearer {dropboxAccessToken}");
//        www.SetRequestHeader("Content-Type", "application/octet-stream");
//        www.SetRequestHeader("Dropbox-API-Arg", $"{{\"path\": \"/{fileName}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}}");

//        yield return www.SendWebRequest();

//        if (www.result == UnityWebRequest.Result.Success)
//            UpdateStatus("Upload successful!");
//        else
//            UpdateStatus("Upload failed: " + www.error);

//        www.Dispose();
//    }

//    private void UpdateStatus(string message)
//    {
//        if (statusText != null)
//        {
//            statusText.text = message;
//            statusText.gameObject.SetActive(true);
//        }
//        UnityEngine.Debug.Log(message);
//    }

//    // IFFmpegCallbacksHandler implementation
//    public void OnStart(long executionId)
//    {
//        ffmpegExecutionId = executionId;
//        UpdateStatus("Encoding started...");
//    }

//    public void OnProgress(long executionId, string message)
//    {
//        ffmpegLog += $"Progress: {message}\n";
//        UpdateProgressFromOutput(message);
//    }

//    public void OnSuccess(long executionId)
//    {
//        ffmpegSuccess = File.Exists(videoFilePath);
//        ffmpegExecutionId = 0;
//        encodingProgress = 1f;
//        if (encodingProgressBar != null)
//            encodingProgressBar.value = 1f;
//    }

//    public void OnError(long executionId, string message)
//    {
//        ffmpegLog = $"Encoding error: {message}";
//        UpdateStatus(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }

//    public void OnCancel(long executionId) { OnCanceled(executionId); }

//    public void OnLog(long executionId, string message)
//    {
//        ffmpegLog += $"Log: {message}\n";
//    }

//    public void OnWarning(long executionId, string message)
//    {
//        ffmpegLog += $"Warning: {message}\n";
//    }

//    public void OnCanceled(long executionId)
//    {
//        ffmpegLog = "Encoding canceled";
//        UpdateStatus(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }

//    public void OnFail(long executionId)
//    {
//        ffmpegLog = "Encoding failed";
//        UpdateStatus(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }

//    private void OnDestroy()
//    {
//        if (wearHf != null)
//            wearHf.ClearCommands();
//    }
//}


using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
using FFmpegUnityBind2;
using FFmpegUnityBind2.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WearHFPlugin;

public class VideoRecorder : MonoBehaviour, IFFmpegCallbacksHandler
{
    [Header("References")]
    public RawImage sourceRawImage; // Local RawImage (e.g., WebCamTexture on Android)
    public TextMeshProUGUI statusText;
    public Button startButton;
    public Button stopButton;
    public Slider encodingProgressBar;
    public WearHF wearHf;
    public JoinChannelVideoWithRealWear agoraManager; // Reference to Agora script

    [Header("Upload Settings")]
    [TextArea]
    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";
    public bool uploadToGitHub = false; // Toggle between GitHub and Dropbox
    [TextArea]
    public string gitHubAccessToken = "YOUR_GITHUB_PERSONAL_ACCESS_TOKEN"; // GitHub PAT with repo scope
    public string gitHubOwner = "ArshadAnsari04"; // GitHub username
    public string gitHubRepo = "UploadedData"; // Repository name
    public string gitHubPath = "videos"; // Base folder path in repo
    public string gitHubBranch = "main"; // Initial branch (will be validated)

    [Header("Recording Settings")]
    public int frameRate = 24;
    public int width = 640;
    public int height = 360;

    private List<string> capturedFrames = new List<string>();
    private bool isRecording = false;
    private string outputFolder;
    private string videoFilePath;
    private long ffmpegExecutionId;
    private bool ffmpegSuccess;
    private string ffmpegLog = "";
    private const string FramePattern = "frame_{0:D04}.jpg";
    private const string FrameGlobPattern = "frame_%04d.jpg";
    private const int MaxSaveAttempts = 3;
    private const string voiceCommandStart = "Start Recording";
    private const string voiceCommandStop = "Stop Recording";
    private float encodingProgress = 0f;
    private string currentDeviceIdentifier; // Store device ID or name

    private void Start()
    {
        // Initialize buttons
        if (startButton != null)
            startButton.onClick.AddListener(StartRecording);
        else
            UnityEngine.Debug.LogError("Start Button not assigned!");

        if (stopButton != null)
        {
            stopButton.onClick.AddListener(StopRecording);
            stopButton.gameObject.SetActive(false);
        }
        else
            UnityEngine.Debug.LogError("Stop Button not assigned!");

        // Initialize progress bar
        if (encodingProgressBar != null)
        {
            encodingProgressBar.gameObject.SetActive(false);
            encodingProgressBar.value = 0f;
        }

        // Initialize WearHF
        GameObject wearHfObject = GameObject.Find("WearHF Manager");
        if (wearHfObject != null)
        {
            wearHf = wearHfObject.GetComponent<WearHF>();
            if (wearHf != null)
            {
                wearHf.AddVoiceCommand(voiceCommandStart, (cmd) => StartRecording());
                wearHf.AddVoiceCommand(voiceCommandStop, (cmd) => StopRecording());
                UnityEngine.Debug.Log($"Registered voice commands: {voiceCommandStart}, {voiceCommandStop}");
            }
            else
                UnityEngine.Debug.LogError("WearHF component not found!");
        }
        else
            UnityEngine.Debug.LogError("WearHF Manager not found!");

        // Initialize Agora manager
        if (agoraManager == null)
        {
            agoraManager = FindObjectOfType<JoinChannelVideoWithRealWear>();
            if (agoraManager == null)
                UnityEngine.Debug.LogError("JoinChannelVideoWithRealWear component not found!");
        }

        // Ensure status text is readable
        if (statusText != null)
        {
            statusText.fontSize = 24;
            statusText.gameObject.SetActive(false);
        }

        // Validate GitHub settings at startup
        if (uploadToGitHub)
        {
            StartCoroutine(ValidateGitHubSettings());
        }
    }

    private IEnumerator ValidateGitHubSettings()
    {
        if (string.IsNullOrEmpty(gitHubAccessToken) || string.IsNullOrEmpty(gitHubOwner) || string.IsNullOrEmpty(gitHubRepo))
        {
            UpdateStatus("GitHub settings incomplete: Missing access token, owner, or repository.");
            UnityEngine.Debug.LogError("GitHub settings incomplete: Missing access token, owner, or repository.");
            yield break;
        }

        string repoUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}";
        UnityEngine.Debug.Log($"Validating GitHub repository: {repoUrl}");
        UnityWebRequest repoRequest = UnityWebRequest.Get(repoUrl);
        repoRequest.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        repoRequest.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        repoRequest.SetRequestHeader("User-Agent", "UnityVideoRecorder");

        yield return repoRequest.SendWebRequest();

        if (repoRequest.result == UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log($"GitHub repository validated: {gitHubOwner}/{gitHubRepo}");
            string jsonResponse = repoRequest.downloadHandler.text;
            RepositoryInfo repoInfo = JsonUtility.FromJson<RepositoryInfo>(jsonResponse);
            if (!string.IsNullOrEmpty(repoInfo.default_branch))
            {
                gitHubBranch = repoInfo.default_branch;
                UnityEngine.Debug.Log($"Detected default branch: {gitHubBranch}");
            }
            else
            {
                UnityEngine.Debug.LogWarning("Could not detect default branch, using configured branch: {gitHubBranch}");
            }
        }
        else
        {
            UpdateStatus($"GitHub repository validation failed: {repoRequest.error}\nResponse: {repoRequest.downloadHandler.text}");
            UnityEngine.Debug.LogError($"GitHub repository validation failed: {repoRequest.error}\nResponse: {repoRequest.downloadHandler.text}");
        }

        repoRequest.Dispose();
    }

    [System.Serializable]
    private class RepositoryInfo
    {
        public string default_branch;
    }

    public void StartRecording()
    {
        if (isRecording) return;

        Texture sourceTexture = GetRecordingTexture(out currentDeviceIdentifier);
        if (sourceTexture == null)
        {
            UpdateStatus("No valid texture available for recording!");
            return;
        }

        isRecording = true;
        outputFolder = GetRecordingOutputFolder();
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        capturedFrames.Clear();
        encodingProgress = 0f;
        if (encodingProgressBar != null)
            encodingProgressBar.value = 0f;

        startButton.gameObject.SetActive(false);
        stopButton.gameObject.SetActive(true);
        if (wearHf != null)
            wearHf.ClearCommands();
        wearHf.AddVoiceCommand(voiceCommandStop, (cmd) => StopRecording());

        StartCoroutine(CaptureFrames());
        UpdateStatus("Recording started...");
    }

    private Texture GetRecordingTexture(out string deviceIdentifier)
    {
        deviceIdentifier = "UnknownDevice";

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        // On Windows/Editor, prefer remote user's RawImage if active
        if (agoraManager != null)
        {
            if (agoraManager._activeRemoteUid.HasValue)
            {
                uint activeUid = agoraManager._activeRemoteUid.Value;
                if (agoraManager._remoteUserVideoViews.TryGetValue(activeUid, out GameObject videoView))
                {
                    RawImage remoteRawImage = videoView?.GetComponent<RawImage>();
                    if (remoteRawImage != null && remoteRawImage.enabled && remoteRawImage.texture != null)
                    {
                        Texture texture = remoteRawImage.texture;
                        if (texture.width > 0 && texture.height > 0)
                        {
                            // Get remote user name or fallback to device ID
                            if (agoraManager._assignedNames.TryGetValue(activeUid, out string userName) && !string.IsNullOrEmpty(userName))
                            {
                                deviceIdentifier = userName.Trim();
                                UnityEngine.Debug.Log($"Recording remote user video (UID: {activeUid}, Name: {deviceIdentifier}, {texture.width}x{texture.height})");
                            }
                            else
                            {
                                deviceIdentifier = $"Device_{activeUid}";
                                UnityEngine.Debug.LogWarning($"No name found for UID: {activeUid}, using device ID: {deviceIdentifier}");
                            }
                            return texture;
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"Remote texture invalid (UID: {activeUid}, width: {texture.width}, height: {texture.height})");
                        }
                    }
                    else
                    {
                        UnityEngine.Debug.LogWarning($"Remote RawImage invalid (UID: {activeUid}, enabled: {remoteRawImage?.enabled}, texture: {remoteRawImage?.texture})");
                    }
                }
                else
                {
                    UnityEngine.Debug.LogWarning($"No video view for active remote UID: {activeUid}");
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("No active remote user, using fallback device ID");
                deviceIdentifier = $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Agora manager not assigned, using fallback device ID");
            deviceIdentifier = $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        }
        UnityEngine.Debug.Log("Falling back to local sourceRawImage");
#endif

        // Fallback to local sourceRawImage
        if (sourceRawImage != null && sourceRawImage.texture != null && sourceRawImage.texture.width > 0 && sourceRawImage.texture.height > 0)
        {
            UnityEngine.Debug.Log($"Recording local sourceRawImage ({sourceRawImage.texture.width}x{sourceRawImage.texture.height})");
            return sourceRawImage.texture;
        }

        UnityEngine.Debug.LogError("No valid local sourceRawImage texture");
        return null;
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        StopAllCoroutines();
        StartCoroutine(EncodeAndUpload());

        startButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
        if (wearHf != null)
        {
            wearHf.ClearCommands();
            wearHf.AddVoiceCommand(voiceCommandStart, (cmd) => StartRecording());
        }
        currentDeviceIdentifier = null;
    }

    public void CancelRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        StopAllCoroutines();
        if (ffmpegExecutionId != 0)
        {
            FFmpegAndroid.Cancel(ffmpegExecutionId);
            ffmpegExecutionId = 0;
        }

        foreach (var file in capturedFrames)
        {
            if (File.Exists(file)) File.Delete(file);
        }
        capturedFrames.Clear();

        startButton.gameObject.SetActive(true);
        stopButton.gameObject.SetActive(false);
        if (wearHf != null)
        {
            wearHf.ClearCommands();
            wearHf.AddVoiceCommand(voiceCommandStart, (cmd) => StartRecording());
        }

        UpdateStatus("Recording canceled.");
        currentDeviceIdentifier = null;
    }

    private IEnumerator CaptureFrames()
    {
        float interval = 1f / frameRate;
        int frameCount = 0;
        RenderTexture bufferRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        while (isRecording)
        {
            yield return new WaitForEndOfFrame();

            Texture srcTexture = GetRecordingTexture(out string _);
            if (srcTexture == null)
            {
                UpdateStatus("Texture lost during recording.");
                Destroy(bufferRT);
                yield break;
            }

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

            if (srcTexture is RenderTexture rt)
            {
                Graphics.Blit(rt, bufferRT);
                RenderTexture.active = bufferRT;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
            }
            else if (srcTexture is Texture2D t2d)
            {
                Graphics.Blit(t2d, bufferRT);
                RenderTexture.active = bufferRT;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
            }
            else if (srcTexture is WebCamTexture wct)
            {
                Graphics.Blit(wct, bufferRT);
                RenderTexture.active = bufferRT;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
            }
            else
            {
                UpdateStatus("Unsupported texture type.");
                Destroy(tex);
                Destroy(bufferRT);
                yield break;
            }

            string framePath = Path.Combine(outputFolder, string.Format(FramePattern, frameCount));
            byte[] jpgData = tex.EncodeToJPG(75);
            int attempts = 0;

            while (attempts < MaxSaveAttempts)
            {
                try
                {
                    File.WriteAllBytes(framePath, jpgData);
                    break;
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"Frame {frameCount} save attempt {attempts + 1} failed: {ex.Message}");
                }
                attempts++;
                yield return new WaitForSeconds(0.1f);
            }

            capturedFrames.Add(framePath);
            long fileSize = File.Exists(framePath) ? new FileInfo(framePath).Length : 0;
            if (!File.Exists(framePath) || fileSize < 100)
                UpdateStatus($"Warning: Frame {frameCount} not saved correctly: {framePath}");

            Destroy(tex);
            frameCount++;
            yield return new WaitForSeconds(interval);
        }

        Destroy(bufferRT);
    }

    private IEnumerator EncodeAndUpload()
    {
        UpdateStatus("Encoding video...");
        if (encodingProgressBar != null)
        {
            encodingProgressBar.gameObject.SetActive(true);
            encodingProgressBar.value = 0f;
        }

        videoFilePath = GetRecordingOutputPath();
        ffmpegSuccess = false;
        ffmpegLog = "";

        string inputPattern = Path.Combine(outputFolder, FrameGlobPattern).Replace("\\", "/");
        string outputPath = videoFilePath.Replace("\\", "/");

        if (capturedFrames.Count == 0)
        {
            UpdateStatus("No frames captured for encoding.");
            yield break;
        }

        foreach (var frame in capturedFrames)
        {
            if (!File.Exists(frame))
            {
                UpdateStatus($"Frame missing: {frame}");
                UnityEngine.Debug.LogError($"Frame missing: {frame}");
                yield break;
            }
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        string ffmpegPath = GetFFmpegPath();
        UnityEngine.Debug.Log($"FFmpeg path: {ffmpegPath}");
        if (!File.Exists(ffmpegPath))
        {
            UpdateStatus($"FFmpeg not found at: {ffmpegPath}");
            yield break;
        }

        Process ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = ffmpegPath;
        ffmpeg.StartInfo.Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 5 -pix_fmt yuv420p -vf fps={frameRate} \"{outputPath}\"";
        ffmpeg.StartInfo.CreateNoWindow = true;
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.RedirectStandardOutput = true;
        ffmpeg.StartInfo.RedirectStandardError = true;

        UnityEngine.Debug.Log($"FFmpeg command: {ffmpeg.StartInfo.Arguments}");

        string outputLog = "";
        ffmpeg.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputLog += args.Data + "\n";
                UpdateProgressFromOutput(args.Data);
            }
        };
        ffmpeg.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
            {
                outputLog += args.Data + "\n";
                UpdateProgressFromOutput(args.Data);
            }
        };

        bool startedSuccessfully = false;
        try
        {
            startedSuccessfully = ffmpeg.Start();
            ffmpeg.BeginOutputReadLine();
            ffmpeg.BeginErrorReadLine();
        }
        catch (Exception ex)
        {
            ffmpegLog = $"FFmpeg execution error: {ex.Message}";
            UpdateStatus(ffmpegLog);
            UnityEngine.Debug.LogError(ffmpegLog);
            ffmpegSuccess = false;
            yield break;
        }

        if (startedSuccessfully)
        {
            float elapsed = 0f;
            while (!ffmpeg.HasExited)
            {
                elapsed += 0.5f;
                if (encodingProgressBar != null)
                    encodingProgressBar.value = encodingProgress;
                yield return new WaitForSeconds(0.5f);
            }

            ffmpeg.WaitForExit();
            ffmpegLog = outputLog;
            ffmpegSuccess = File.Exists(videoFilePath) && ffmpeg.ExitCode == 0;

            if (!ffmpegSuccess)
            {
                UpdateStatus("FFmpeg encoding failed: " + ffmpegLog);
                UnityEngine.Debug.LogError("FFmpeg encoding failed: " + ffmpegLog);
            }
            else
            {
                UpdateStatus("FFmpeg encoding successful");
            }
        }
        else
        {
            ffmpegLog = "FFmpeg failed to start.";
            UpdateStatus(ffmpegLog);
            UnityEngine.Debug.LogError(ffmpegLog);
            ffmpegSuccess = false;
        }
#elif UNITY_ANDROID
        string command = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 5 -pix_fmt yuv420p -vf fps={frameRate} -threads 1 \"{outputPath}\"";
        UnityEngine.Debug.Log($"FFmpeg Android command: {command}");
        try
        {
            List<IFFmpegCallbacksHandler> handlers = new List<IFFmpegCallbacksHandler> { this };
            ffmpegExecutionId = FFmpegAndroid.Execute(command, handlers);
        }
        catch (Exception ex)
        {
            ffmpegLog = $"FFmpeg error: {ex.Message}";
            UpdateStatus(ffmpegLog);
            UnityEngine.Debug.LogError(ffmpegLog);
            ffmpegSuccess = false;
            yield break;
        }

        float timeout = 120f;
        float timer = 0f;
        while (ffmpegExecutionId != 0 && timer < timeout)
        {
            if (encodingProgressBar != null)
                encodingProgressBar.value = encodingProgress;
            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }

        ffmpegSuccess = File.Exists(videoFilePath);
        if (!ffmpegSuccess)
        {
            UpdateStatus("Encoding failed: " + ffmpegLog);
            UnityEngine.Debug.LogError("Encoding failed: " + ffmpegLog);
        }
#else
        UpdateStatus("Encoding not supported on this platform.");
        yield break;
#endif

        // Clean up frames
        foreach (var file in capturedFrames)
        {
            if (File.Exists(file)) File.Delete(file);
        }
        capturedFrames.Clear();

        if (encodingProgressBar != null)
            encodingProgressBar.gameObject.SetActive(false);

        if (ffmpegSuccess)
        {
            UpdateStatus(uploadToGitHub ? "Encoding complete. Uploading to GitHub..." : "Encoding complete. Uploading to Dropbox...");
            yield return StartCoroutine(uploadToGitHub ? UploadToGitHubCoroutine(videoFilePath) : UploadToDropboxCoroutine(videoFilePath));
        }
    }

    private IEnumerator UploadToGitHubCoroutine(string filePath)
    {
        if (!File.Exists(filePath))
        {
            UpdateStatus("File not found for upload: " + filePath);
            yield break;
        }

        if (string.IsNullOrEmpty(gitHubAccessToken) || string.IsNullOrEmpty(gitHubOwner) || string.IsNullOrEmpty(gitHubRepo) || string.IsNullOrEmpty(gitHubBranch))
        {
            UpdateStatus("GitHub upload failed: Missing access token, owner, repository, or branch.");
            UnityEngine.Debug.LogError("GitHub upload failed: Missing access token, owner, repository, or branch.");
            yield break;
        }

        // Pre-upload permission check
        string repoUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}";
        UnityWebRequest repoCheck = UnityWebRequest.Get(repoUrl);
        repoCheck.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        repoCheck.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        repoCheck.SetRequestHeader("User-Agent", "UnityVideoRecorder");
        yield return repoCheck.SendWebRequest();

        if (repoCheck.result != UnityWebRequest.Result.Success)
        {
            UpdateStatus($"GitHub pre-upload check failed: {repoCheck.error}\nResponse: {repoCheck.downloadHandler.text}");
            UnityEngine.Debug.LogError($"GitHub pre-upload check failed: {repoCheck.error}\nResponse: {repoCheck.downloadHandler.text}");
            repoCheck.Dispose();
            yield break;
        }
        repoCheck.Dispose();

        // Write access check using HEAD request
        string testUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}?ref={gitHubBranch}";
        UnityEngine.Debug.Log($"Write access check URL: {testUrl}");
        UnityWebRequest writeCheck = UnityWebRequest.Head(testUrl);
        writeCheck.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        writeCheck.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        writeCheck.SetRequestHeader("User-Agent", "UnityVideoRecorder");
        yield return writeCheck.SendWebRequest();

        if (writeCheck.result != UnityWebRequest.Result.Success)
        {
            UpdateStatus($"GitHub write access check failed: {writeCheck.error}\nResponse: {writeCheck.downloadHandler.text}");
            UnityEngine.Debug.LogError($"GitHub write access check failed: {writeCheck.error}\nResponse: {writeCheck.downloadHandler.text}");
            if (!writeCheck.downloadHandler.text.Contains("Repository not found"))
            {
                UnityEngine.Debug.LogWarning("Proceeding with upload despite write check failure; GitHub will create the path if needed.");
            }
            else
            {
                writeCheck.Dispose();
                yield break;
            }
        }
        else
        {
            UnityEngine.Debug.Log("GitHub write access check successful.");
        }
        writeCheck.Dispose();

        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);

        // Use device identifier with dynamic date
        string deviceIdentifier = agoraManager != null && agoraManager._activeRemoteUid.HasValue
            ? (agoraManager._assignedNames.TryGetValue(agoraManager._activeRemoteUid.Value, out string userName) && !string.IsNullOrEmpty(userName)
                ? userName.Trim()
                : $"Device_{agoraManager._activeRemoteUid.Value}")
            : $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        string currentDate = DateTime.Now.ToString("ddMMMMyyyy"); // e.g., "01July2025" for July 01, 2025
        string gitHubFilePathRaw = Path.Combine(gitHubPath, deviceIdentifier, "Videos", currentDate, fileName).Replace("\\", "/");

        string gitHubFilePath = SanitizePath(gitHubFilePathRaw);
        if (gitHubFilePath.Length > 400) // GitHub path length limit
        {
            UpdateStatus("GitHub upload failed: Path exceeds 400 characters limit.");
            UnityEngine.Debug.LogError($"GitHub upload failed: Path exceeds 400 characters: {gitHubFilePath}");
            yield break;
        }
        UnityEngine.Debug.Log($"GitHub upload path (raw): {gitHubFilePathRaw}");
        UnityEngine.Debug.Log($"GitHub upload path (sanitized): {gitHubFilePath}");

        // Encode file to Base64
        string base64Content = Convert.ToBase64String(fileData);

        // Prepare JSON payload with proper escaping
        GitHubUploadPayload payload = new GitHubUploadPayload
        {
            message = $"Upload video {fileName} via Unity",
            content = base64Content,
            branch = gitHubBranch
        };
        string jsonPayload = JsonUtility.ToJson(payload);
        UnityEngine.Debug.Log($"GitHub JSON Payload: {jsonPayload}");

        // Ensure the full path is escaped for the API
        string apiUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}/contents/{UnityWebRequest.EscapeURL(gitHubFilePath)}";
        UnityEngine.Debug.Log($"GitHub API URL: {apiUrl}");

        UnityWebRequest www = new UnityWebRequest(apiUrl, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("User-Agent", "UnityVideoRecorder");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            UpdateStatus("GitHub upload successful!");
            UnityEngine.Debug.Log($"GitHub upload response: {www.downloadHandler.text}");
        }
        else
        {
            string errorDetails = $"GitHub upload failed: {www.error}\nResponse: {www.downloadHandler.text}";
            UpdateStatus(errorDetails);
            UnityEngine.Debug.LogError(errorDetails);

            if (www.downloadHandler.text.Contains("403"))
            {
                UnityEngine.Debug.LogError("403 Error: The PAT lacks permission to access or modify the repository. Ensure it has 'repo' scope and access to ArshadAnsari04/UploadedData.");
            }
            else if (www.downloadHandler.text.Contains("404"))
            {
                UnityEngine.Debug.LogError("404 Error: Verify gitHubOwner, gitHubRepo, and gitHubBranch. Ensure repository exists and branch is correct.");
            }
            else if (www.downloadHandler.text.Contains("422"))
            {
                UnityEngine.Debug.LogError("422 Error: Path is malformed. Check sanitized gitHubFilePath for invalid characters or length. GitHub should create the path if valid.");
            }
        }

        www.Dispose();
    }

    private string SanitizePath(string path)
    {
        // Remove or replace invalid characters for GitHub paths (e.g., :, *, ?, ", <, >, |) and normalize
        string sanitized = Regex.Replace(path, "[*:?\"<>|]", "_");
        sanitized = sanitized.Trim().Replace("//", "/"); // Normalize double slashes
        sanitized = sanitized.TrimStart('/').TrimEnd('/');
        return sanitized;
    }

    [System.Serializable]
    private class GitHubUploadPayload
    {
        public string message;
        public string content;
        public string branch;
    }

    private IEnumerator UploadToDropboxCoroutine(string filePath)
    {
        if (!File.Exists(filePath))
        {
            UpdateStatus("File not found for upload: " + filePath);
            yield break;
        }

        byte[] fileData = File.ReadAllBytes(filePath);
        string fileName = Path.GetFileName(filePath);
        string deviceIdentifier = agoraManager != null && agoraManager._activeRemoteUid.HasValue
            ? (agoraManager._assignedNames.TryGetValue(agoraManager._activeRemoteUid.Value, out string userName) && !string.IsNullOrEmpty(userName)
                ? userName.Trim()
                : $"Device_{agoraManager._activeRemoteUid.Value}")
            : $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        string currentDate = DateTime.Now.ToString("ddMMMMyyyy"); // e.g., "01July2025"
        string dropboxFilePath = deviceIdentifier != "UnknownDevice"
            ? $"/{UnityWebRequest.EscapeURL(deviceIdentifier)}/Videos/{currentDate}/{fileName}"
            : $"/{fileName}";

        UnityEngine.Debug.Log($"Dropbox upload path: {dropboxFilePath}");

        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
        www.uploadHandler = new UploadHandlerRaw(fileData);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Authorization", $"Bearer {dropboxAccessToken}");
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        www.SetRequestHeader("Dropbox-API-Arg", $"{{\"path\": \"{dropboxFilePath}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}}");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
            UpdateStatus("Dropbox upload successful!");
        else
            UpdateStatus("Dropbox upload failed: " + www.error);

        www.Dispose();
    }

    private void UpdateProgressFromOutput(string output)
    {
        if (string.IsNullOrEmpty(output) || !output.Contains("frame=")) return;
        try
        {
            int frameIndex = output.IndexOf("frame=");
            string frameStr = output.Substring(frameIndex + 6).Split(' ')[0];
            if (int.TryParse(frameStr, out int currentFrame) && capturedFrames.Count > 0)
            {
                encodingProgress = Mathf.Clamp01((float)currentFrame / capturedFrames.Count);
                UnityEngine.Debug.Log($"Progress updated: {encodingProgress:P0} ({currentFrame}/{capturedFrames.Count})");
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"Failed to parse progress: {ex.Message}");
        }
    }

    private string GetRecordingOutputFolder()
    {
        string deviceIdentifier = agoraManager != null && agoraManager._activeRemoteUid.HasValue
            ? (agoraManager._assignedNames.TryGetValue(agoraManager._activeRemoteUid.Value, out string userName) && !string.IsNullOrEmpty(userName)
                ? userName.Trim()
                : $"Device_{agoraManager._activeRemoteUid.Value}")
            : $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        string baseFolder = Path.Combine(Application.persistentDataPath, deviceIdentifier, "Videos");
        string currentDate = DateTime.Now.ToString("ddMMMMyyyy"); // e.g., "01July2025" for July 01, 2025
        string videoFolder = Path.Combine(baseFolder, currentDate);
        if (!Directory.Exists(videoFolder))
        {
            try
            {
                Directory.CreateDirectory(videoFolder);
                UnityEngine.Debug.Log($"Created folder: {videoFolder}");
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError($"Failed to create folder {videoFolder}: {ex.Message}");
            }
        }
        return videoFolder;
    }

    private string GetRecordingOutputPath()
    {
        string folder = GetRecordingOutputFolder();
        string fileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4"; // e.g., Recording_20250701_170523.mp4
        return Path.Combine(folder, fileName);
    }

    private string GetFFmpegPath()
    {
        string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "Desktop/Win/ffmpeg.exe");
        return ffmpegPath;
    }

    private void UpdateStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }
        UnityEngine.Debug.Log(message);
    }

    // IFFmpegCallbacksHandler implementation
    public void OnStart(long executionId)
    {
        ffmpegExecutionId = executionId;
        UpdateStatus("Encoding started...");
    }

    public void OnProgress(long executionId, string message)
    {
        ffmpegLog += $"Progress: {message}\n";
        UpdateProgressFromOutput(message);
    }

    public void OnSuccess(long executionId)
    {
        ffmpegSuccess = File.Exists(videoFilePath);
        ffmpegExecutionId = 0;
        encodingProgress = 1f;
        if (encodingProgressBar != null)
            encodingProgressBar.value = 1f;
    }

    public void OnError(long executionId, string message)
    {
        ffmpegLog = $"Encoding error: {message}";
        UpdateStatus(ffmpegLog);
        ffmpegSuccess = false;
        ffmpegExecutionId = 0;
    }

    public void OnCancel(long executionId) { OnCanceled(executionId); }

    public void OnLog(long executionId, string message)
    {
        ffmpegLog += $"Log: {message}\n";
    }

    public void OnWarning(long executionId, string message)
    {
        ffmpegLog += $"Warning: {message}\n";
    }

    public void OnCanceled(long executionId)
    {
        ffmpegLog = "Encoding canceled";
        UpdateStatus(ffmpegLog);
        ffmpegSuccess = false;
        ffmpegExecutionId = 0;
    }

    public void OnFail(long executionId)
    {
        ffmpegLog = "Encoding failed";
        UpdateStatus(ffmpegLog);
        ffmpegSuccess = false;
        ffmpegExecutionId = 0;
    }

    private void OnDestroy()
    {
        if (wearHf != null)
            wearHf.ClearCommands();
    }
}