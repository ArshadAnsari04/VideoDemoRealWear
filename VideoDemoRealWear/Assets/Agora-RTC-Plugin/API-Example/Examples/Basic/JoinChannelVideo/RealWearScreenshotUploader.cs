//using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
//using System;
//using TMPro;
//using UnityEngine;
//using UnityEngine.Networking;
//using WearHFPlugin;
//using UnityEngine.UI;
//using System.Collections;
//public class RealWearScreenshotUploader : MonoBehaviour
//{
//    private WearHF m_wearHf;
//    public TextMeshProUGUI statusText;
//    [TextArea]
//    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";
//    [SerializeField] private JoinChannelVideoWithRealWear JoinChannelVideoWithRealWear;

//    private void Start()
//    {
//        Button button = GetComponentInChildren<Button>();
//        button.onClick.AddListener(CaptureAndUpload);

//        string buttonText = GetComponentInChildren<Text>().text;

//        m_wearHf = GameObject.Find("WearHF Manager").GetComponent<WearHF>();
//        m_wearHf.AddVoiceCommand(buttonText, VoiceCommandCallback);
//    }

//    private void VoiceCommandCallback(string voiceCommand)
//    {
//        CaptureAndUpload();
//    }

//    public void CaptureAndUpload()
//    {
//        m_wearHf.ClearCommands();
//        StartCoroutine(CaptureCameraFrameAndUploadCoroutine());

//    }

//    private IEnumerator CaptureCameraFrameAndUploadCoroutine()
//    {
//        if (JoinChannelVideoWithRealWear._videoSource == null || !JoinChannelVideoWithRealWear._videoSource.isPlaying)
//        {
//            UpdateStatusText("Camera is not running.");
//            yield break;
//        }

//        int width = JoinChannelVideoWithRealWear._videoSource.width;
//        int height = JoinChannelVideoWithRealWear._videoSource.height;
//        if (width <= 0 || height <= 0)
//        {
//            UpdateStatusText("Invalid camera dimensions.");
//            yield break;
//        }

//        Texture2D cameraFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
//        cameraFrame.SetPixels(JoinChannelVideoWithRealWear._videoSource.GetPixels());
//        cameraFrame.Apply();

//        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//        string filename = $"CameraFrame_{timestamp}.png";
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN
//        string directory = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Screenshots");
//#else
//        string directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
//#endif
//        if (!System.IO.Directory.Exists(directory))
//            System.IO.Directory.CreateDirectory(directory);

//        string path = System.IO.Path.Combine(directory, filename);
//        byte[] pngData = cameraFrame.EncodeToPNG();
//        System.IO.File.WriteAllBytes(path, pngData);
//        UnityEngine.Object.Destroy(cameraFrame);

//        UpdateStatusText($"Camera frame saved to: {path}");
//        Debug.Log($"Camera frame saved to: {path}");

//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(true);
//            statusText.text = "Uploading camera frame...";
//        }

//        yield return StartCoroutine(UploadToDropbox(pngData, filename));
//    }

//    private IEnumerator UploadToDropbox(byte[] fileData, string filename)
//    {
//        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
//        www.uploadHandler = new UploadHandlerRaw(fileData);
//        www.downloadHandler = new DownloadHandlerBuffer();

//        www.SetRequestHeader("Authorization", "Bearer " + dropboxAccessToken);
//        www.SetRequestHeader("Content-Type", "application/octet-stream");
//        www.SetRequestHeader("Dropbox-API-Arg", "{\"path\": \"/" + filename + "\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}");

//        yield return www.SendWebRequest();

//        if (www.result == UnityWebRequest.Result.Success)
//        {
//            Debug.Log("✅ Upload successful: " + www.downloadHandler.text);
//            if (statusText != null) statusText.text = "Upload successful!";
//        }
//        else
//        {
//            Debug.LogError("❌ Upload failed: " + www.error + "\n" + www.downloadHandler.text);
//            if (statusText != null) statusText.text = "Upload failed!";
//        }

//        yield return new WaitForSeconds(1f);
//        if (statusText != null) statusText.gameObject.SetActive(false);
//    }

//    private void UpdateStatusText(string message)
//    {
//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(true);
//            statusText.text = message;
//        }
//    }
//}

//using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
//using System;
//using System.Collections;
//using TMPro;
//using UnityEngine;
//using UnityEngine.Networking;
//using WearHFPlugin;
//using UnityEngine.UI;

//public class RealWearScreenshotUploader : MonoBehaviour
//{
//    private WearHF m_wearHf;
//    public TextMeshProUGUI statusText;
//    [TextArea]
//    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN"; // Replace with valid token
//    [SerializeField] private JoinChannelVideoWithRealWear joinChannelVideoWithRealWear;
//    private bool isCapturing = false;
//    private const string voiceCommandPhrase = "Capture";

//    private void Start()
//    {
//        // Initialize button
//        Button button = GetComponentInChildren<Button>();
//        if (button == null)
//        {
//            Debug.LogError("Button not found in children!");
//            UpdateStatusText("Button not found!");
//            return;
//        }
//        button.onClick.AddListener(CaptureAndUpload);

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

//        // Register voice command
//        m_wearHf.AddVoiceCommand(voiceCommandPhrase, VoiceCommandCallback);
//        Debug.Log($"Registered voice command: {voiceCommandPhrase}");

//        // Ensure status text is initially hidden
//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(false);
//        }
//    }

//    private void VoiceCommandCallback(string voiceCommand)
//    {
//        Debug.Log($"Voice command triggered: {voiceCommand}");
//        CaptureAndUpload();
//    }

//    public void CaptureAndUpload()
//    {
//        if (isCapturing)
//        {
//            Debug.LogWarning("Capture in progress, ignoring new request.");
//            return;
//        }
//        isCapturing = true;
//       // m_wearHf.ClearCommands();
//        StartCoroutine(CaptureCameraFrameAndUploadCoroutine());
//    }

//    private IEnumerator CaptureCameraFrameAndUploadCoroutine()
//    {
//        // Validate video source
//        if (joinChannelVideoWithRealWear == null || joinChannelVideoWithRealWear._videoSource == null)
//        {
//            UpdateStatusText("Video source is not initialized.");
//            isCapturing = false;
//            yield break;
//        }

//        WebCamTexture videoSource = joinChannelVideoWithRealWear._videoSource as WebCamTexture;
//        if (videoSource == null || !videoSource.isPlaying)
//        {
//            UpdateStatusText("Camera is not running.");
//            isCapturing = false;
//            yield break;
//        }

//        // Wait for frame
//        yield return new WaitForEndOfFrame();

//        // Get camera dimensions
//        int width = videoSource.width;
//        int height = videoSource.height;
//        if (width <= 0 || height <= 0)
//        {
//            UpdateStatusText("Invalid camera dimensions.");
//            isCapturing = false;
//            yield break;
//        }

//        // Capture camera frame
//        Texture2D cameraFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
//        try
//        {
//            cameraFrame.SetPixels(videoSource.GetPixels());
//            cameraFrame.Apply();
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to capture camera frame: {e.Message}");
//            UpdateStatusText("Failed to capture camera frame.");
//            Destroy(cameraFrame);
//            isCapturing = false;
//            yield break;
//        }

//        // Resize for RealWear display (854x480)
//        Texture2D resizedFrame = ResizeTexture(cameraFrame, 854, 480);
//        Destroy(cameraFrame);

//        // Generate filename
//        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
//        string filename = $"CameraFrame_{timestamp}.png";

//        // Save to file
//        string directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
//        try
//        {
//            if (!System.IO.Directory.Exists(directory))
//            {
//                System.IO.Directory.CreateDirectory(directory);
//            }
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to create directory: {e.Message}");
//            UpdateStatusText("Failed to create screenshot directory.");
//            Destroy(resizedFrame);
//            isCapturing = false;
//            yield break;
//        }

//        string path = System.IO.Path.Combine(directory, filename);
//        byte[] pngData = null;
//        try
//        {
//            pngData = resizedFrame.EncodeToPNG();
//            System.IO.File.WriteAllBytes(path, pngData);
//            Debug.Log($"Camera frame saved to: {path}");
//            UpdateStatusText($"Camera frame saved to: {path}");
//        }
//        catch (Exception e)
//        {
//            Debug.LogError($"Failed to save screenshot: {e.Message}");
//            UpdateStatusText("Failed to save screenshot.");
//            Destroy(resizedFrame);
//            isCapturing = false;
//            yield break;
//        }

//        Destroy(resizedFrame);

//        // Upload to Dropbox
//        UpdateStatusText("Uploading camera frame...");
//        yield return StartCoroutine(UploadToDropbox(pngData, filename));

//        // Reset state
//        isCapturing = false;
//        yield return new WaitForSeconds(1f);
//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(false);
//        }

//        // Re-register voice command
//        m_wearHf.AddVoiceCommand(voiceCommandPhrase, VoiceCommandCallback);
//    }

//    private IEnumerator UploadToDropbox(byte[] fileData, string filename)
//    {
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
//    }

//    private void UpdateStatusText(string message)
//    {
//        if (statusText != null)
//        {
//            statusText.gameObject.SetActive(true);
//            statusText.text = message;
//            statusText.fontSize = 24; // Ensure readable on RealWear
//            Debug.Log($"Status: {message}");
//        }
//        else
//        {
//            Debug.LogWarning("StatusText is not assigned!");
//        }
//    }

//    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
//    {
//        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
//        RenderTexture.active = rt;
//        Graphics.Blit(source, rt);
//        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
//        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
//        result.Apply();
//        RenderTexture.active = null;
//        RenderTexture.ReleaseTemporary(rt);
//        return result;
//    }

//    private void OnDestroy()
//    {
//        if (m_wearHf != null)
//        {
//            m_wearHf.ClearCommands();
//        }
//    }
//}

using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
using System;
using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WearHFPlugin;

public class RealWearScreenshotUploader : MonoBehaviour
{
    [Header("References")]
   // public RawImage sourceRawImage; // Local RawImage (e.g., WebCamTexture on Android)
    public TextMeshProUGUI statusText;
    public Button startButton;
    public JoinChannelVideoWithRealWear agoraManager; // Reference to Agora script

    [Header("Upload Settings")]
    [TextArea]
    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN"; // Replace with valid token
    public bool uploadToGitHub = false; // Toggle between GitHub and Dropbox
    [TextArea]
    public string gitHubAccessToken = "YOUR_GITHUB_PERSONAL_ACCESS_TOKEN"; // GitHub PAT with repo scope
    public string gitHubOwner = "ArshadAnsari04"; // GitHub username
    public string gitHubRepo = "UploadedData"; // Repository name
    public string gitHubPath = "videos"; // Base folder path in repo
    public string gitHubBranch = "main"; // Initial branch (will be validated)

    private WearHF m_wearHf;
    private bool isCapturing = false;
    private const string voiceCommandPhrase = "Capture";

    private void Start()
    {
        // Initialize button
        Button button = GetComponentInChildren<Button>();
        if (button == null)
        {
            Debug.LogError("Button not found in children!");
            UpdateStatusText("Button not found!");
            return;
        }
        button.onClick.AddListener(CaptureAndUpload);

        // Initialize WearHF
        GameObject wearHfObject = GameObject.Find("WearHF Manager");
        if (wearHfObject == null)
        {
            Debug.LogError("WearHF Manager not found!");
            UpdateStatusText("WearHF Manager not found!");
            return;
        }
        m_wearHf = wearHfObject.GetComponent<WearHF>();
        if (m_wearHf == null)
        {
            Debug.LogError("WearHF component not found!");
            UpdateStatusText("WearHF component not found!");
            return;
        }

        // Register voice command
        m_wearHf.AddVoiceCommand(voiceCommandPhrase, VoiceCommandCallback);
        Debug.Log($"Registered voice command: {voiceCommandPhrase}");

        // Ensure status text is initially hidden
        if (statusText != null)
        {
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
            UpdateStatusText("GitHub settings incomplete: Missing access token, owner, or repository.");
            Debug.LogError("GitHub settings incomplete: Missing access token, owner, or repository.");
            yield break;
        }

        string repoUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}";
        Debug.Log($"Validating GitHub repository: {repoUrl}");
        UnityWebRequest repoRequest = UnityWebRequest.Get(repoUrl);
        repoRequest.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        repoRequest.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        repoRequest.SetRequestHeader("User-Agent", "RealWearScreenshotUploader");

        yield return repoRequest.SendWebRequest();

        if (repoRequest.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"GitHub repository validated: {gitHubOwner}/{gitHubRepo}");
            string jsonResponse = repoRequest.downloadHandler.text;
            RepositoryInfo repoInfo = JsonUtility.FromJson<RepositoryInfo>(jsonResponse);
            if (!string.IsNullOrEmpty(repoInfo.default_branch))
            {
                gitHubBranch = repoInfo.default_branch;
                Debug.Log($"Detected default branch: {gitHubBranch}");
            }
            else
            {
                Debug.LogWarning("Could not detect default branch, using configured branch: {gitHubBranch}");
            }
        }
        else
        {
            UpdateStatusText($"GitHub repository validation failed: {repoRequest.error}\nResponse: {repoRequest.downloadHandler.text}");
            Debug.LogError($"GitHub repository validation failed: {repoRequest.error}\nResponse: {repoRequest.downloadHandler.text}");
        }

        repoRequest.Dispose();
    }

    [System.Serializable]
    private class RepositoryInfo
    {
        public string default_branch;
    }

    private void VoiceCommandCallback(string voiceCommand)
    {
        Debug.Log($"Voice command triggered: {voiceCommand}");
        CaptureAndUpload();
    }

    public void CaptureAndUpload()
    {
        if (isCapturing)
        {
            Debug.LogWarning("Capture in progress, ignoring new request.");
            return;
        }
        isCapturing = true;
        StartCoroutine(CaptureCameraFrameAndUploadCoroutine());
    }

    private IEnumerator CaptureCameraFrameAndUploadCoroutine()
    {
        // Validate video source
        if (agoraManager == null || agoraManager._videoSource == null)
        {
            UpdateStatusText("Video source is not initialized.");
            isCapturing = false;
            yield break;
        }

        WebCamTexture videoSource = agoraManager._videoSource as WebCamTexture;
        if (videoSource == null || !videoSource.isPlaying)
        {
            UpdateStatusText("Camera is not running.");
            isCapturing = false;
            yield break;
        }

        // Wait for frame
        yield return new WaitForEndOfFrame();

        // Get camera dimensions
        int width = videoSource.width;
        int height = videoSource.height;
        if (width <= 0 || height <= 0)
        {
            UpdateStatusText("Invalid camera dimensions.");
            isCapturing = false;
            yield break;
        }

        // Capture camera frame
        Texture2D cameraFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
        try
        {
            cameraFrame.SetPixels(videoSource.GetPixels());
            cameraFrame.Apply();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to capture camera frame: {e.Message}");
            UpdateStatusText("Failed to capture camera frame.");
            Destroy(cameraFrame);
            isCapturing = false;
            yield break;
        }

        // Resize for RealWear display (854x480)
        Texture2D resizedFrame = ResizeTexture(cameraFrame, 854, 480);
        Destroy(cameraFrame);

        // Generate filename
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"CameraFrame_{timestamp}.png";

        // Determine device identifier
        string deviceIdentifier = agoraManager != null && agoraManager._activeRemoteUid.HasValue
            ? (agoraManager._assignedNames.TryGetValue(agoraManager._activeRemoteUid.Value, out string userName) && !string.IsNullOrEmpty(userName)
                ? userName.Trim()
                : $"Device_{agoraManager._activeRemoteUid.Value}")
            : $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        Debug.Log($"Determined deviceIdentifier: {deviceIdentifier}, _activeRemoteUid: {agoraManager?._activeRemoteUid}");

        // Check for existing folder with the same deviceIdentifier
        string baseScreenshotDir = Path.Combine(Application.persistentDataPath, deviceIdentifier, "Screenshots");
        string currentDate = DateTime.Now.ToString("ddMMMMyyyy"); // e.g., "01July2025" for July 01, 2025
        string targetDir = Path.Combine(baseScreenshotDir, currentDate);
        if (!Directory.Exists(targetDir))
        {
            try
            {
                Directory.CreateDirectory(targetDir);
                Debug.Log($"Created new screenshot folder: {targetDir}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create screenshot folder: {e.Message}");
                UpdateStatusText("Failed to create screenshot folder.");
                Destroy(resizedFrame);
                isCapturing = false;
                yield break;
            }
        }
        else
        {
            Debug.Log($"Reusing existing screenshot folder: {targetDir}");
        }

        // Save to file
        string path = Path.Combine(targetDir, filename);
        byte[] pngData = null;
        try
        {
            pngData = resizedFrame.EncodeToPNG();
            File.WriteAllBytes(path, pngData);
            Debug.Log($"Camera frame saved to: {path}");
            UpdateStatusText($"Camera frame saved to: {path}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save screenshot: {e.Message}");
            UpdateStatusText("Failed to save screenshot.");
            Destroy(resizedFrame);
            isCapturing = false;
            yield break;
        }

        Destroy(resizedFrame);

        // Upload to Dropbox or GitHub based on toggle
        UpdateStatusText("Uploading camera frame...");
        if (uploadToGitHub)
        {
            yield return StartCoroutine(UploadToGitHub(pngData, filename));
        }
        else
        {
            yield return StartCoroutine(UploadToDropbox(pngData, filename));
        }

        // Reset state
        isCapturing = false;
        yield return new WaitForSeconds(1f);
        if (statusText != null)
        {
            statusText.gameObject.SetActive(false);
        }

        // Re-register voice command
        m_wearHf.AddVoiceCommand(voiceCommandPhrase, VoiceCommandCallback);
    }

    private IEnumerator UploadToGitHub(byte[] fileData, string filename)
    {
        if (string.IsNullOrEmpty(gitHubAccessToken) || string.IsNullOrEmpty(gitHubOwner) || string.IsNullOrEmpty(gitHubRepo) || string.IsNullOrEmpty(gitHubBranch))
        {
            UpdateStatusText("GitHub upload failed: Missing access token, owner, repository, or branch.");
            Debug.LogError("GitHub upload failed: Missing access token, owner, repository, or branch.");
            yield break;
        }

        // Pre-upload permission check
        string repoUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}";
        UnityWebRequest repoCheck = UnityWebRequest.Get(repoUrl);
        repoCheck.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        repoCheck.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        repoCheck.SetRequestHeader("User-Agent", "RealWearScreenshotUploader");
        yield return repoCheck.SendWebRequest();

        if (repoCheck.result != UnityWebRequest.Result.Success)
        {
            UpdateStatusText($"GitHub pre-upload check failed: {repoCheck.error}\nResponse: {repoCheck.downloadHandler.text}");
            Debug.LogError($"GitHub pre-upload check failed: {repoCheck.error}\nResponse: {repoCheck.downloadHandler.text}");
            repoCheck.Dispose();
            yield break;
        }
        repoCheck.Dispose();

        // Write access check using HEAD request
        string testUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}?ref={gitHubBranch}";
        Debug.Log($"Write access check URL: {testUrl}");
        UnityWebRequest writeCheck = UnityWebRequest.Head(testUrl);
        writeCheck.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        writeCheck.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        writeCheck.SetRequestHeader("User-Agent", "RealWearScreenshotUploader");
        yield return writeCheck.SendWebRequest();

        if (writeCheck.result != UnityWebRequest.Result.Success)
        {
            UpdateStatusText($"GitHub write access check failed: {writeCheck.error}\nResponse: {writeCheck.downloadHandler.text}");
            Debug.LogError($"GitHub write access check failed: {writeCheck.error}\nResponse: {writeCheck.downloadHandler.text}");
            if (!writeCheck.downloadHandler.text.Contains("Repository not found"))
            {
                Debug.LogWarning("Proceeding with upload despite write check failure; GitHub will create the path if needed.");
            }
            else
            {
                writeCheck.Dispose();
                yield break;
            }
        }
        else
        {
            Debug.Log("GitHub write access check successful.");
        }
        writeCheck.Dispose();

        // Use device identifier with dynamic date
        string deviceIdentifier = agoraManager != null && agoraManager._activeRemoteUid.HasValue
            ? (agoraManager._assignedNames.TryGetValue(agoraManager._activeRemoteUid.Value, out string userName) && !string.IsNullOrEmpty(userName)
                ? userName.Trim()
                : $"Device_{agoraManager._activeRemoteUid.Value}")
            : $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        string currentDate = DateTime.Now.ToString("ddMMMMyyyy"); // e.g., "01July2025" for July 01, 2025
        string gitHubFilePathRaw = Path.Combine(gitHubPath, deviceIdentifier, "Screenshots", currentDate, filename).Replace("\\", "/");

        string gitHubFilePath = SanitizePath(gitHubFilePathRaw);
        if (gitHubFilePath.Length > 400) // GitHub path length limit
        {
            UpdateStatusText("GitHub upload failed: Path exceeds 400 characters limit.");
            Debug.LogError($"GitHub upload failed: Path exceeds 400 characters: {gitHubFilePath}");
            yield break;
        }
        Debug.Log($"GitHub upload path (raw): {gitHubFilePathRaw}");
        Debug.Log($"GitHub upload path (sanitized): {gitHubFilePath}");

        // Encode file to Base64
        string base64Content = Convert.ToBase64String(fileData);

        // Prepare JSON payload
        GitHubUploadPayload payload = new GitHubUploadPayload
        {
            message = $"Upload screenshot {filename} via RealWear",
            content = base64Content,
            branch = gitHubBranch
        };
        string jsonPayload = JsonUtility.ToJson(payload);
        Debug.Log($"GitHub JSON Payload: {jsonPayload}");

        // Upload to GitHub
        string apiUrl = $"https://api.github.com/repos/{gitHubOwner}/{gitHubRepo}/contents/{UnityWebRequest.EscapeURL(gitHubFilePath)}";
        Debug.Log($"GitHub API URL: {apiUrl}");

        UnityWebRequest www = new UnityWebRequest(apiUrl, "PUT");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Authorization", $"Bearer {gitHubAccessToken}");
        www.SetRequestHeader("Content-Type", "application/json");
        www.SetRequestHeader("Accept", "application/vnd.github.v3+json");
        www.SetRequestHeader("User-Agent", "RealWearScreenshotUploader");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            UpdateStatusText("GitHub upload successful!");
            Debug.Log($"GitHub upload response: {www.downloadHandler.text}");
        }
        else
        {
            string errorDetails = $"GitHub upload failed: {www.error}\nResponse: {www.downloadHandler.text}";
            UpdateStatusText(errorDetails);
            Debug.LogError(errorDetails);

            if (www.downloadHandler.text.Contains("403"))
            {
                Debug.LogError("403 Error: The PAT lacks permission to access or modify the repository. Ensure it has 'repo' scope and access to ArshadAnsari04/UploadedData.");
            }
            else if (www.downloadHandler.text.Contains("404"))
            {
                Debug.LogError("404 Error: Verify gitHubOwner, gitHubRepo, and gitHubBranch. Ensure repository exists and branch is correct.");
            }
            else if (www.downloadHandler.text.Contains("422"))
            {
                Debug.LogError("422 Error: Path is malformed. Check sanitized gitHubFilePath for invalid characters or length. GitHub should create the path if valid.");
            }
        }

        www.Dispose();
    }

    private IEnumerator UploadToDropbox(byte[] fileData, string filename)
    {
        // Use device identifier with dynamic date
        string deviceIdentifier = agoraManager != null && agoraManager._activeRemoteUid.HasValue
            ? (agoraManager._assignedNames.TryGetValue(agoraManager._activeRemoteUid.Value, out string userName) && !string.IsNullOrEmpty(userName)
                ? userName.Trim()
                : $"Device_{agoraManager._activeRemoteUid.Value}")
            : $"EditorDevice_{SystemInfo.deviceUniqueIdentifier.GetHashCode()}";
        string currentDate = DateTime.Now.ToString("ddMMMMyyyy"); // e.g., "01July2025"
        string dropboxFilePath = deviceIdentifier != "UnknownDevice"
            ? $"/{UnityWebRequest.EscapeURL(deviceIdentifier)}/Screenshots/{currentDate}/{filename}"
            : $"/{filename}";

        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
        www.uploadHandler = new UploadHandlerRaw(fileData);
        www.downloadHandler = new DownloadHandlerBuffer();

        // Set headers
        www.SetRequestHeader("Authorization", $"Bearer {dropboxAccessToken}");
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        www.SetRequestHeader("Dropbox-API-Arg", $"{{\"path\": \"{dropboxFilePath}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}}");

        // Send request
        yield return www.SendWebRequest();

        // Handle result
        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"Upload successful: {www.downloadHandler.text}");
            UpdateStatusText("Upload successful!");
        }
        else
        {
            Debug.LogError($"Upload failed: {www.error}\nResponse: {www.downloadHandler.text}");
            UpdateStatusText($"Upload failed: {www.error}");
        }

        www.Dispose();
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
            statusText.fontSize = 24; // Ensure readable on RealWear
            Debug.Log($"Status: {message}");
        }
        else
        {
            Debug.LogWarning("StatusText is not assigned!");
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
    {
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D result = new Texture2D(newWidth, newHeight, TextureFormat.RGB24, false);
        result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
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

    private void OnDestroy()
    {
        if (m_wearHf != null)
        {
            m_wearHf.ClearCommands();
        }
    }
}