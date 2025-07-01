

using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using WearHFPlugin;
using UnityEngine.UI;

public class RealWearScreenshotUploader : MonoBehaviour
{
    private WearHF m_wearHf;
    public TextMeshProUGUI statusText;
    [TextArea]
    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN"; // Replace with valid token
    [SerializeField] private JoinChannelVideoWithRealWear joinChannelVideoWithRealWear;
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
       // m_wearHf.ClearCommands();
        StartCoroutine(CaptureCameraFrameAndUploadCoroutine());
    }

    private IEnumerator CaptureCameraFrameAndUploadCoroutine()
    {
        // Validate video source
        if (joinChannelVideoWithRealWear == null || joinChannelVideoWithRealWear._videoSource == null)
        {
            UpdateStatusText("Video source is not initialized.");
            isCapturing = false;
            yield break;
        }

        WebCamTexture videoSource = joinChannelVideoWithRealWear._videoSource as WebCamTexture;
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

        // Save to file
        string directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
        try
        {
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to create directory: {e.Message}");
            UpdateStatusText("Failed to create screenshot directory.");
            Destroy(resizedFrame);
            isCapturing = false;
            yield break;
        }

        string path = System.IO.Path.Combine(directory, filename);
        byte[] pngData = null;
        try
        {
            pngData = resizedFrame.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, pngData);
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

        // Upload to Dropbox
        UpdateStatusText("Uploading camera frame...");
        yield return StartCoroutine(UploadToDropbox(pngData, filename));

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

    private IEnumerator UploadToDropbox(byte[] fileData, string filename)
    {
        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
        www.uploadHandler = new UploadHandlerRaw(fileData);
        www.downloadHandler = new DownloadHandlerBuffer();

        // Set headers
        www.SetRequestHeader("Authorization", $"Bearer {dropboxAccessToken}");
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        www.SetRequestHeader("Dropbox-API-Arg", $"{{\"path\": \"/{filename}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}}");

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

    private void OnDestroy()
    {
        if (m_wearHf != null)
        {
            m_wearHf.ClearCommands();
        }
    }
}