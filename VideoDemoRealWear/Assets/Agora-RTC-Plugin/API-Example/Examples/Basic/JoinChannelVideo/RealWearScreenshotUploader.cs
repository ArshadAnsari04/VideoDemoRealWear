using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using WearHFPlugin;
using UnityEngine.UI;
using System.Collections;
public class RealWearScreenshotUploader : MonoBehaviour
{
    private WearHF m_wearHf;
    public TextMeshProUGUI statusText;
    [TextArea]
    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";
    [SerializeField] private JoinChannelVideoWithRealWear JoinChannelVideoWithRealWear;

    private void Start()
    {
        Button button = GetComponentInChildren<Button>();
        button.onClick.AddListener(CaptureAndUpload);

        string buttonText = GetComponentInChildren<Text>().text;

        m_wearHf = GameObject.Find("WearHF Manager").GetComponent<WearHF>();
        m_wearHf.AddVoiceCommand(buttonText, VoiceCommandCallback);
    }

    private void VoiceCommandCallback(string voiceCommand)
    {
        CaptureAndUpload();
    }

    public void CaptureAndUpload()
    {
        m_wearHf.ClearCommands();
        StartCoroutine(CaptureCameraFrameAndUploadCoroutine());
        
    }

    private IEnumerator CaptureCameraFrameAndUploadCoroutine()
    {
        if (JoinChannelVideoWithRealWear._videoSource == null || !JoinChannelVideoWithRealWear._videoSource.isPlaying)
        {
            UpdateStatusText("Camera is not running.");
            yield break;
        }

        int width = JoinChannelVideoWithRealWear._videoSource.width;
        int height = JoinChannelVideoWithRealWear._videoSource.height;
        if (width <= 0 || height <= 0)
        {
            UpdateStatusText("Invalid camera dimensions.");
            yield break;
        }

        Texture2D cameraFrame = new Texture2D(width, height, TextureFormat.RGB24, false);
        cameraFrame.SetPixels(JoinChannelVideoWithRealWear._videoSource.GetPixels());
        cameraFrame.Apply();

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"CameraFrame_{timestamp}.png";
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        string directory = System.IO.Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, "Screenshots");
#else
        string directory = System.IO.Path.Combine(Application.persistentDataPath, "Screenshots");
#endif
        if (!System.IO.Directory.Exists(directory))
            System.IO.Directory.CreateDirectory(directory);

        string path = System.IO.Path.Combine(directory, filename);
        byte[] pngData = cameraFrame.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, pngData);
        UnityEngine.Object.Destroy(cameraFrame);

        UpdateStatusText($"Camera frame saved to: {path}");
        Debug.Log($"Camera frame saved to: {path}");

        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = "Uploading camera frame...";
        }

        yield return StartCoroutine(UploadToDropbox(pngData, filename));
    }

    private IEnumerator UploadToDropbox(byte[] fileData, string filename)
    {
        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
        www.uploadHandler = new UploadHandlerRaw(fileData);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Authorization", "Bearer " + dropboxAccessToken);
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        www.SetRequestHeader("Dropbox-API-Arg", "{\"path\": \"/" + filename + "\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Upload successful: " + www.downloadHandler.text);
            if (statusText != null) statusText.text = "Upload successful!";
        }
        else
        {
            Debug.LogError("❌ Upload failed: " + www.error + "\n" + www.downloadHandler.text);
            if (statusText != null) statusText.text = "Upload failed!";
        }

        yield return new WaitForSeconds(1f);
        if (statusText != null) statusText.gameObject.SetActive(false);
    }

    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.gameObject.SetActive(true);
            statusText.text = message;
        }
    }
}