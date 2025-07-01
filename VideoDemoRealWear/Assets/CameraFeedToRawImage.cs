using UnityEngine;
using UnityEngine.UI;

public class CameraFeedToRawImage : MonoBehaviour
{
    public RawImage targetRawImage;
    private WebCamTexture webcamTexture;

    void Start()
    {
        if (targetRawImage == null)
        {
            Debug.LogError("RawImage not assigned!");
            return;
        }

        // Use the first available camera
        if (WebCamTexture.devices.Length > 0)
        {
            webcamTexture = new WebCamTexture();
            targetRawImage.texture = webcamTexture;
            targetRawImage.material.mainTexture = webcamTexture;
            webcamTexture.Play();
        }
        else
        {
            Debug.LogError("No camera devices found!");
        }
    }

    void OnDestroy()
    {
        if (webcamTexture != null && webcamTexture.isPlaying)
            webcamTexture.Stop();
    }
}