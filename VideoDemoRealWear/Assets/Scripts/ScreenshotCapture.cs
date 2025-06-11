using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;

public class ScreenshotCapture : MonoBehaviour
{
    public Button captureButton;

    private void Start()
    {
        if (captureButton != null)
        {
            captureButton.onClick.AddListener(CaptureScreenshot);
        }
    }

    public void CaptureScreenshot()
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"Screenshot_{timestamp}.png";
        string directory;

#if UNITY_EDITOR
        // Project root (one level up from Assets)
        directory = Directory.GetParent(Application.dataPath).FullName;
#else
        // Build root (one level up from Data folder)
        directory = Directory.GetParent(Application.dataPath).FullName;
#endif

        string path = Path.Combine(directory, filename);

        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"Screenshot saved to: {path}");
    }
}