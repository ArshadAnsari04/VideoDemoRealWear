

//using FFmpegUnityBind2;
//using FFmpegUnityBind2.Android;
////using FFmpegUnityBind2.Shared;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.IO;
//using TMPro;
//using UnityEngine;
//using UnityEngine.Networking;
//using UnityEngine.UI;

//public class VideoRecorder : MonoBehaviour, IFFmpegCallbacksHandler
//{
//    [Header("References")]
//    public RawImage sourceRawImage; // Assign your RawImage in the Inspector
//    public TextMeshProUGUI statusText;

//    [Header("Dropbox")]
//    [TextArea]
//    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";

//    [Header("Recording Settings")]
//    public int frameRate = 30;
//    public int durationSeconds = 10;
//    public int width = 854;
//    public int height = 480;

//    private List<string> capturedFrames = new List<string>();
//    private bool isRecording = false;
//    private string outputFolder;
//    private string videoFilePath;
//    private long ffmpegExecutionId;
//    private bool ffmpegSuccess;
//    private string ffmpegLog = "";

//    private const string FramePattern = "frame_{0:D04}.png";
//    private const string FrameGlobPattern = "frame_%04d.png";
//    private const int MaxSaveAttempts = 3;



//    public void StartRecording()
//    {
//        UnityEngine.Debug.Log($"RawImage assigned: {sourceRawImage != null}");
//        if (sourceRawImage != null)
//            UnityEngine.Debug.Log($"RawImage.texture assigned: {sourceRawImage.texture != null}, type: {(sourceRawImage.texture != null ? sourceRawImage.texture.GetType().Name : "null")}, width: {sourceRawImage.texture.width}, height: {sourceRawImage.texture.height}");
//        if (isRecording) return;
//        if (sourceRawImage == null || sourceRawImage.texture == null)
//        {
//            UpdateStatus("RawImage or its texture is not assigned!");
//            return;
//        }
//        isRecording = true;
//        outputFolder = GetRecordingOutputFolder();
//        if (!Directory.Exists(outputFolder))
//        {
//            Directory.CreateDirectory(outputFolder);
//            UnityEngine.Debug.Log($"Created output folder: {outputFolder}");
//        }
//        capturedFrames.Clear();
//        StartCoroutine(CaptureFrames());
//        UpdateStatus("Recording started: " + outputFolder);
//    }

//    public void StopRecording()
//    {
//        if (!isRecording) return;
//        isRecording = false;
//        StopAllCoroutines();
//        StartCoroutine(EncodeAndUpload());
//    }

//    public void CancelRecording()
//    {
//        if (!isRecording) return;
//        isRecording = false;
//        StopAllCoroutines();
//        if (ffmpegExecutionId != 0)
//        {
//            FFmpegAndroid.Cancel(ffmpegExecutionId);
//            UnityEngine.Debug.Log($"Canceled FFmpeg execution ID: {ffmpegExecutionId}");
//            ffmpegExecutionId = 0;
//        }
//        foreach (var file in capturedFrames)
//        {
//            if (File.Exists(file)) File.Delete(file);
//        }
//        capturedFrames.Clear();
//        UpdateStatus("Recording canceled and frames deleted.");
//    }

//    private IEnumerator CaptureFrames()
//    {
//        float interval = 1f / frameRate;
//        int frameCount = 0;
//        float startTime = Time.time;

//        while (isRecording && (Time.time - startTime) < durationSeconds)
//        {
//            yield return new WaitForEndOfFrame();

//            Texture srcTexture = sourceRawImage.texture;
//            Texture2D tex = null;
//            int attempts = 0;

//            if (srcTexture is RenderTexture rt)
//            {
//                RenderTexture currentRT = RenderTexture.active;
//                RenderTexture.active = rt;
//                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
//                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//                tex.Apply();
//                RenderTexture.active = currentRT;
//            }
//            else if (srcTexture is Texture2D t2d)
//            {
//                tex = new Texture2D(width, height, TextureFormat.RGB24, false);
//                Color[] pixels = t2d.GetPixels(0, 0, t2d.width, t2d.height);
//                tex.SetPixels(pixels);
//                tex.Apply();
//            }
//            else if (srcTexture is WebCamTexture wct)
//            {
//                Texture2D webcamFrame = new Texture2D(wct.width, wct.height, TextureFormat.RGB24, false);
//                webcamFrame.SetPixels(wct.GetPixels());
//                webcamFrame.Apply();

//                if (wct.width != width || wct.height != height)
//                {
//                    RenderTexture rtResize = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
//                    RenderTexture.active = rtResize;
//                    Graphics.Blit(webcamFrame, rtResize);
//                    tex = new Texture2D(width, height, TextureFormat.RGB24, false);
//                    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
//                    tex.Apply();
//                    RenderTexture.active = null;
//                    RenderTexture.ReleaseTemporary(rtResize);
//                    UnityEngine.Object.Destroy(webcamFrame);
//                }
//                else
//                {
//                    tex = webcamFrame;
//                }
//            }
//            else
//            {
//                UpdateStatus("Unsupported texture type for RawImage.");
//                yield break;
//            }

//            string framePath = Path.Combine(outputFolder, string.Format(FramePattern, frameCount));
//            byte[] pngData = tex.EncodeToPNG();
//            while (attempts < MaxSaveAttempts && (!File.Exists(framePath) || new FileInfo(framePath).Length < 100))
//            {
//                try
//                {
//                    File.WriteAllBytes(framePath, pngData);
//                }
//                catch (Exception ex)
//                {
//                    UnityEngine.Debug.LogError($"Frame {frameCount} save attempt {attempts + 1} failed: {ex.Message}");
//                }
//                attempts++;
//                yield return new WaitForSeconds(0.1f); // Brief delay between attempts
//            }
//            Destroy(tex);
//            capturedFrames.Add(framePath);

//            long fileSize = File.Exists(framePath) ? new FileInfo(framePath).Length : 0;
//            if (!File.Exists(framePath) || fileSize < 100)
//            {
//                UpdateStatus($"Warning: Frame {frameCount} not saved correctly: {framePath} (Size: {fileSize} bytes, Attempts: {attempts})");
//                UnityEngine.Debug.LogError($"Frame {frameCount} not saved: {framePath} (Size: {fileSize} bytes, Attempts: {attempts})");
//            }
//            else
//            {
//                UnityEngine.Debug.Log($"Frame {frameCount} saved: {framePath} (Size: {fileSize} bytes, Attempts: {attempts})");
//            }

//            frameCount++;
//            yield return new WaitForSeconds(interval);
//        }
//        isRecording = false;
//        StartCoroutine(EncodeAndUpload());
//    }
//    private IEnumerator EncodeAndUpload()
//    {
//        UpdateStatus("Encoding video...");
//        videoFilePath = GetRecordingOutputPath();
//        ffmpegSuccess = false;
//        ffmpegLog = "";

//        // Sanitize paths for Android compatibility
//        string inputPattern = Path.Combine(outputFolder, FrameGlobPattern).Replace("\\", "/").Replace(" ", "\\ ").Replace("(", "\\(").Replace(")", "\\)");
//        string outputPath = videoFilePath.Replace("\\", "/").Replace(" ", "\\ ").Replace("(", "\\(").Replace(")", "\\)");

//        UnityEngine.Debug.Log($"inputPattern: {inputPattern}");
//        UnityEngine.Debug.Log($"outputPath: {outputPath}");

//#if UNITY_EDITOR || UNITY_STANDALONE_WIN
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

//        string ffmpegPath = GetFFmpegPath();
//        if (!File.Exists(ffmpegPath))
//        {
//            UpdateStatus("ffmpeg.exe not found at: " + ffmpegPath);
//        }
//        else
//        {
//            Process ffmpeg = new Process();
//            ffmpeg.StartInfo.FileName = ffmpegPath;
//            ffmpeg.StartInfo.Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 \"{outputPath}\"";
//            ffmpeg.StartInfo.CreateNoWindow = true;
//            ffmpeg.StartInfo.UseShellExecute = false;
//            ffmpeg.StartInfo.RedirectStandardOutput = true;
//            ffmpeg.StartInfo.RedirectStandardError = true;
//            ffmpeg.Start();
//            string output = ffmpeg.StandardError.ReadToEnd();
//            ffmpeg.WaitForExit();
//            ffmpegSuccess = File.Exists(videoFilePath);
//            if (!ffmpegSuccess)
//            {
//                UpdateStatus("FFmpeg error: " + output);
//                UnityEngine.Debug.LogError("FFmpeg error: " + output);
//            }
//        }
//#elif UNITY_ANDROID
//    if (capturedFrames.Count == 0)
//    {
//        UpdateStatus("No frames captured for encoding.");
//        yield break;
//    }
//    foreach (var frame in capturedFrames)
//    {
//        if (!File.Exists(frame))
//        {
//            UpdateStatus($"Frame missing: {frame}");
//            UnityEngine.Debug.LogError($"Frame missing: {frame}");
//            yield break;
//        }
//    }

//    // Use LGPL-compatible codecs only
//    string[] commands = new[]
//    {
//   // $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libvpx -b:v 800k -threads 4 \"{outputPath}.webm\""
//        $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 5 \"{outputPath}\"",
//        $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libvpx -b:v 800k -c:a libmp3lame \"{outputPath}.webm\"",
//        $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 5 \"{outputPath}.avi\""
//    };
//    int attempt = 0;

//    while (attempt < commands.Length && !ffmpegSuccess)
//    {
//        string command = commands[attempt];
//        UnityEngine.Debug.Log($"FFmpeg attempt {attempt + 1} command: {command}");
//        try
//        {
//            List<IFFmpegCallbacksHandler> handlers = new List<IFFmpegCallbacksHandler> { this };
//            ffmpegExecutionId = FFmpegAndroid.Execute(command, handlers);
//        }
//        catch (Exception ex)
//        {
//            ffmpegLog = $"FFmpegUnityBind2 error (attempt {attempt + 1}): {ex.Message}\nStackTrace: {ex.StackTrace}";
//            UpdateStatus(ffmpegLog);
//            UnityEngine.Debug.LogError(ffmpegLog);
//            ffmpegSuccess = false;
//            yield break;
//        }

//        float timeout = 60f;
//        float timer = 0f;
//        while (ffmpegExecutionId != 0 && timer < timeout)
//        {
//            yield return new WaitForSeconds(1f);
//            timer += 1f;
//        }

//        videoFilePath = (attempt == 1) ? $"{outputPath}.webm" : (attempt == 2) ? $"{outputPath}.avi" : outputPath;
//        ffmpegSuccess = File.Exists(videoFilePath);
//        if (!ffmpegSuccess)
//        {
//            UpdateStatus($"FFmpeg attempt {attempt + 1} failed: Did not produce output file.\n" + ffmpegLog);
//            UnityEngine.Debug.LogError($"Encoding attempt {attempt + 1} failed: " + ffmpegLog);
//            attempt++;
//            if (attempt < commands.Length)
//            {
//                UnityEngine.Debug.Log($"Retrying with next format...");
//            }
//        }
//    }

//    if (!ffmpegSuccess)
//    {
//        UpdateStatus("FFmpegUnityBind2 did not produce output file after all attempts.\n" + ffmpegLog);
//        UnityEngine.Debug.LogError("Encoding failed after all attempts: " + ffmpegLog);
//    }
//    else if (!File.Exists(videoFilePath))
//    {
//        UpdateStatus("Output file not found after encoding: " + videoFilePath);
//        UnityEngine.Debug.LogError($"Output file missing: {videoFilePath}");
//        ffmpegSuccess = false;
//    }
//#else
//    ffmpegSuccess = false;
//    UpdateStatus("FFmpeg encoding not supported on this platform.");
//#endif

//        // Clean up frames
//        foreach (var file in capturedFrames)
//        {
//            if (File.Exists(file)) File.Delete(file);
//        }
//        capturedFrames.Clear();

//        if (ffmpegSuccess)
//        {
//            UpdateStatus("Encoding complete. Uploading to Dropbox...");
//            yield return StartCoroutine(UploadToDropboxCoroutine(videoFilePath));
//        }
//        else
//        {
//            UpdateStatus("Encoding failed.\n" + ffmpegLog);
//        }
//    }
//    //    private IEnumerator EncodeAndUpload()
//    //    {
//    //        UpdateStatus("Encoding video...");
//    //        videoFilePath = GetRecordingOutputPath();
//    //        ffmpegSuccess = false;
//    //        ffmpegLog = "";

//    //        // Sanitize paths for Android compatibility
//    //        string inputPattern = Path.Combine(outputFolder, FrameGlobPattern).Replace("\\", "/").Replace(" ", "\\ ").Replace("(", "\\(").Replace(")", "\\)");
//    //        string outputPath = videoFilePath.Replace("\\", "/").Replace(" ", "\\ ").Replace("(", "\\(").Replace(")", "\\)");

//    //        UnityEngine.Debug.Log($"inputPattern: {inputPattern}");
//    //        UnityEngine.Debug.Log($"outputPath: {outputPath}");

//    //#if UNITY_EDITOR || UNITY_STANDALONE_WIN
//    //        if (capturedFrames.Count == 0)
//    //        {
//    //            UpdateStatus("No frames captured for encoding.");
//    //            yield break;
//    //        }
//    //        foreach (var frame in capturedFrames)
//    //        {
//    //            if (!File.Exists(frame))
//    //            {
//    //                UpdateStatus($"Frame missing: {frame}");
//    //                UnityEngine.Debug.LogError($"Frame missing: {frame}");
//    //                yield break;
//    //            }
//    //        }

//    //        string ffmpegPath = GetFFmpegPath();
//    //        if (!File.Exists(ffmpegPath))
//    //        {
//    //            UpdateStatus("ffmpeg.exe not found at: " + ffmpegPath);
//    //        }
//    //        else
//    //        {
//    //            Process ffmpeg = new Process();
//    //            ffmpeg.StartInfo.FileName = ffmpegPath;
//    //            ffmpeg.StartInfo.Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p \"{outputPath}\"";
//    //            ffmpeg.StartInfo.CreateNoWindow = true;
//    //            ffmpeg.StartInfo.UseShellExecute = false;
//    //            ffmpeg.StartInfo.RedirectStandardOutput = true;
//    //            ffmpeg.StartInfo.RedirectStandardError = true;
//    //            ffmpeg.Start();
//    //            string output = ffmpeg.StandardError.ReadToEnd();
//    //            ffmpeg.WaitForExit();
//    //            ffmpegSuccess = File.Exists(videoFilePath);
//    //            if (!ffmpegSuccess)
//    //            {
//    //                UpdateStatus("FFmpeg error: " + output);
//    //                UnityEngine.Debug.LogError("FFmpeg error: " + output);
//    //            }
//    //        }
//    //#elif UNITY_ANDROID
//    //        if (capturedFrames.Count == 0)
//    //        {
//    //            UpdateStatus("No frames captured for encoding.");
//    //            yield break;
//    //        }
//    //        foreach (var frame in capturedFrames)
//    //        {
//    //            if (!File.Exists(frame))
//    //            {
//    //                UpdateStatus($"Frame missing: {frame}");
//    //                UnityEngine.Debug.LogError($"Frame missing: {frame}");
//    //                yield break;
//    //            }
//    //        }
//    //        string[] commands = new[]
//    //        {
//    //            $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 \"{outputPath}\"",
//    //            $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 \"{outputPath}.avi\""
//    //        };
//    //        // Try multiple formats with fallback
//    //        //string[] commands = new[]
//    //        //{
//    //        //    $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 \"{outputPath}\"",
//    //        //    $"-framerate {frameRate} -i \"{inputPattern}\" -c:v libx264 -pix_fmt yuv420p \"{outputPath}\"",
//    //        //    $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 \"{outputPath}.avi\"" // Fallback to AVI
//    //        //};
//    //        int attempt = 0;

//    //        while (attempt < commands.Length && !ffmpegSuccess)
//    //        {
//    //            string command = commands[attempt];
//    //            UnityEngine.Debug.Log($"FFmpeg attempt {attempt + 1} command: {command}");
//    //            try
//    //            {
//    //                List<IFFmpegCallbacksHandler> handlers = new List<IFFmpegCallbacksHandler> { this };
//    //                ffmpegExecutionId = FFmpegAndroid.Execute(command, handlers);
//    //            }
//    //            catch (Exception ex)
//    //            {
//    //                ffmpegLog = $"FFmpegUnityBind2 error (attempt {attempt + 1}): {ex.Message}\nStackTrace: {ex.StackTrace}";
//    //                UpdateStatus(ffmpegLog);
//    //                UnityEngine.Debug.LogError(ffmpegLog);
//    //                ffmpegSuccess = false;
//    //                yield break;
//    //            }

//    //            float timeout = 60f;
//    //            float timer = 0f;
//    //            while (ffmpegExecutionId != 0 && timer < timeout)
//    //            {
//    //                yield return new WaitForSeconds(1f);
//    //                timer += 1f;
//    //            }

//    //            videoFilePath = (attempt == 2) ? $"{outputPath}.avi" : outputPath; // Adjust for AVI fallback
//    //            ffmpegSuccess = File.Exists(videoFilePath);
//    //            if (!ffmpegSuccess)
//    //            {
//    //                UpdateStatus($"FFmpeg attempt {attempt + 1} failed: Did not produce output file.\n" + ffmpegLog);
//    //                UnityEngine.Debug.LogError($"Encoding attempt {attempt + 1} failed: " + ffmpegLog);
//    //                attempt++;
//    //                if (attempt < commands.Length)
//    //                {
//    //                    UnityEngine.Debug.Log($"Retrying with next format...");
//    //                }
//    //            }
//    //        }

//    //        if (!ffmpegSuccess)
//    //        {
//    //            UpdateStatus("FFmpegUnityBind2 did not produce output file after all attempts.\n" + ffmpegLog);
//    //            UnityEngine.Debug.LogError("Encoding failed after all attempts: " + ffmpegLog);
//    //        }
//    //        else if (!File.Exists(videoFilePath))
//    //        {
//    //            UpdateStatus("Output file not found after encoding: " + videoFilePath);
//    //            UnityEngine.Debug.LogError($"Output file missing: {videoFilePath}");
//    //            ffmpegSuccess = false;
//    //        }
//    //#else
//    //        ffmpegSuccess = false;
//    //        UpdateStatus("FFmpeg encoding not supported on this platform.");
//    //#endif

//    //        // Clean up frames
//    //        foreach (var file in capturedFrames)
//    //        {
//    //            if (File.Exists(file)) File.Delete(file);
//    //        }
//    //        capturedFrames.Clear();

//    //        if (ffmpegSuccess)
//    //        {
//    //            UpdateStatus("Encoding complete. Uploading to Dropbox...");
//    //            yield return StartCoroutine(UploadToDropboxCoroutine(videoFilePath));
//    //        }
//    //        else
//    //        {
//    //            UpdateStatus("Encoding failed.\n" + ffmpegLog);
//    //        }
//    //    }

//    private string GetRecordingOutputFolder()
//    {
//#if UNITY_EDITOR
//        return Path.Combine(Application.dataPath, "Recordings");
//#elif UNITY_ANDROID
//        string basePath = Application.persistentDataPath;
//        string folderPath = Path.Combine(basePath, "Recordings");
//        if (!Directory.Exists(folderPath))
//        {
//            Directory.CreateDirectory(folderPath);
//            UnityEngine.Debug.Log($"Created output folder: {folderPath} (Base: {basePath})");
//        }
//        return folderPath;
//#else
//        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");
//#endif
//    }

//    private string GetRecordingOutputPath()
//    {
//        string folder = GetRecordingOutputFolder();
//        string fileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
//        return Path.Combine(folder, fileName);
//    }

//    private string GetFFmpegPath()
//    {
//#if UNITY_EDITOR || UNITY_STANDALONE_WIN
//        string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "Desktop/Win/ffmpeg.exe");
//        return ffmpegPath;
//#else
//        return string.Empty;
//#endif
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
//        {
//            UpdateStatus("Upload failed: " + www.error);
//            // Retry upload once if file exists but upload failed
//            if (File.Exists(filePath))
//            {
//                UnityEngine.Debug.Log("Retrying upload...");
//                yield return www.SendWebRequest();
//                if (www.result == UnityWebRequest.Result.Success)
//                    UpdateStatus("Upload successful on retry!");
//                else
//                    UpdateStatus("Upload failed again: " + www.error);
//            }
//        }

//        www.Dispose();
//    }

//    private void UpdateStatus(string message)
//    {
//#if UNITY_ANDROID && !UNITY_EDITOR
//        if (statusText != null)
//        {
//            statusText.text += message + "\n";
//            statusText.gameObject.SetActive(true);
//        }
//#else
//        if (statusText != null)
//        {
//            statusText.text = message;
//            statusText.gameObject.SetActive(true);
//        }
//#endif
//        UnityEngine.Debug.Log(message);
//    }

//    // IFFmpegCallbacksHandler implementation
//    public void OnStart(long executionId)
//    {
//        ffmpegExecutionId = executionId;
//        UpdateStatus($"FFmpeg started with execution ID: {executionId}");
//    }

//    public void OnProgress(long executionId, string message)
//    {
//        UnityEngine.Debug.Log($"FFmpeg progress: {message}");
//        ffmpegLog += $"FFmpeg progress: {message}\n";
//    }

//    public void OnSuccess(long executionId)
//    {
//        ffmpegSuccess = File.Exists(videoFilePath);
//        if (ffmpegSuccess)
//            UpdateStatus("FFmpeg encoding successful");
//        else
//            ffmpegLog = "FFmpeg reported success but output file missing: " + videoFilePath;
//        ffmpegExecutionId = 0;
//    }

//    public void OnError(long executionId, string message)
//    {
//        ffmpegLog = $"FFmpeg error: {message}";
//        UpdateStatus(ffmpegLog);
//        UnityEngine.Debug.LogError(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }

//    public void OnCancel(long executionId)
//    {
//        ffmpegLog = "FFmpeg execution canceled";
//        UpdateStatus(ffmpegLog);
//        UnityEngine.Debug.LogError(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }

//    public void OnLog(long executionId, string message)
//    {
//        ffmpegLog += $"FFmpeg log: {message}\n";
//        UnityEngine.Debug.Log($"FFmpeg log: {message}");
//    }

//    public void OnWarning(long executionId, string message)
//    {
//        ffmpegLog += $"FFmpeg warning: {message}\n";
//        UnityEngine.Debug.LogWarning($"FFmpeg warning: {message}");
//    }

//    public void OnCanceled(long executionId)
//    {
//        ffmpegLog = "FFmpeg execution canceled";
//        UpdateStatus(ffmpegLog);
//        UnityEngine.Debug.LogError(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }

//    public void OnFail(long executionId)
//    {
//        ffmpegLog = "FFmpeg execution failed";
//        UpdateStatus(ffmpegLog);
//        UnityEngine.Debug.LogError(ffmpegLog);
//        ffmpegSuccess = false;
//        ffmpegExecutionId = 0;
//    }
//}


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

//public class VideoRecorder : MonoBehaviour, IFFmpegCallbacksHandler
//{
//    [Header("References")]
//    public RawImage sourceRawImage;
//    public TextMeshProUGUI statusText;
//    public Button startButton;
//    public Button stopButton;
//    public Slider encodingProgressBar;
//    public WearHF wearHf;

//    [Header("Dropbox")]
//    [TextArea]
//    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";

//    [Header("Recording Settings")]
//    public int frameRate = 30;
//    public int width = 854;
//    public int height = 480;

//    private List<string> capturedFrames = new List<string>();
//    private bool isRecording = false;
//    private string outputFolder;
//    private string videoFilePath;
//    private long ffmpegExecutionId;
//    private bool ffmpegSuccess;
//    private string ffmpegLog = "";
//    private const string FramePattern = "frame_{0:D04}.png";
//    private const string FrameGlobPattern = "frame_%04d.png";
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
//        if (sourceRawImage == null || sourceRawImage.texture == null)
//        {
//            UpdateStatus("RawImage or its texture is not assigned!");
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

//            Texture srcTexture = sourceRawImage.texture;
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
//            byte[] pngData = tex.EncodeToPNG();
//            int attempts = 0;

//            while (attempts < MaxSaveAttempts)
//            {
//                try
//                {
//                    File.WriteAllBytes(framePath, pngData);
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
//        ffmpeg.StartInfo.Arguments = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 3 -pix_fmt yuv420p \"{outputPath}\"";
//        ffmpeg.StartInfo.CreateNoWindow = true;
//        ffmpeg.StartInfo.UseShellExecute = false;
//        ffmpeg.StartInfo.RedirectStandardOutput = true;
//        ffmpeg.StartInfo.RedirectStandardError = true;

//        UnityEngine.Debug.Log($"FFmpeg command: {ffmpeg.StartInfo.Arguments}");

//        string outputLog = "";
//        ffmpeg.OutputDataReceived += (sender, args) => { if (args.Data != null) outputLog += args.Data + "\n"; };
//        ffmpeg.ErrorDataReceived += (sender, args) => { if (args.Data != null) outputLog += args.Data + "\n"; };

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
//            // Progress simulation outside try-catch
//            float elapsed = 0f;
//            while (!ffmpeg.HasExited)
//            {
//                elapsed += 0.5f;
//                encodingProgress = Mathf.Clamp01(elapsed / 30f); // Estimate 30s max encoding
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
//        string command = $"-framerate {frameRate} -i \"{inputPattern}\" -c:v mpeg4 -q:v 3 -pix_fmt yuv420p -threads 2 \"{outputPath}\"";
//        try
//        {
//            List<IFFmpegCallbacksHandler> handlers = new List<IFFmpegCallbacksHandler> { this };
//            ffmpegExecutionId = FFmpegAndroid.Execute(command, handlers);
//        }
//        catch (Exception ex)
//        {
//            ffmpegLog = $"FFmpeg error: {ex.Message}";
//            UpdateStatus(ffmpegLog);
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
//        if (message.Contains("frame="))
//        {
//            try
//            {
//                int frameIndex = message.IndexOf("frame=");
//                string frameStr = message.Substring(frameIndex + 6).Split(' ')[0];
//                if (int.TryParse(frameStr, out int currentFrame))
//                {
//                    encodingProgress = Mathf.Clamp01((float)currentFrame / capturedFrames.Count);
//                }
//            }
//            catch { }
//        }
//    }

//    public void OnSuccess(long executionId)
//    {
//        ffmpegSuccess = File.Exists(videoFilePath);
//        ffmpegExecutionId = 0;
//        encodingProgress = 1f;
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


using FFmpegUnityBind2;
using FFmpegUnityBind2.Android;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using WearHFPlugin;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;

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

    [Header("Dropbox")]
    [TextArea]
    public string dropboxAccessToken = "YOUR_DROPBOX_ACCESS_TOKEN";

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
    }

    public void StartRecording()
    {
        if (isRecording) return;

        Texture sourceTexture = GetRecordingTexture();
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

    private Texture GetRecordingTexture()
    {
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
                            UnityEngine.Debug.Log($"Recording remote user video (UID: {activeUid}, {texture.width}x{texture.height})");
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
                UnityEngine.Debug.LogWarning("No active remote user");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("Agora manager not assigned");
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
    }

    private IEnumerator CaptureFrames()
    {
        float interval = 1f / frameRate;
        int frameCount = 0;
        RenderTexture bufferRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);

        while (isRecording)
        {
            yield return new WaitForEndOfFrame();

            Texture srcTexture = GetRecordingTexture();
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
            UpdateStatus("Encoding complete. Uploading to Dropbox...");
            yield return StartCoroutine(UploadToDropboxCoroutine(videoFilePath));
        }
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
        string folderPath = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
        return folderPath;
    }

    private string GetRecordingOutputPath()
    {
        string folder = GetRecordingOutputFolder();
        string fileName = $"Recording_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
        return Path.Combine(folder, fileName);
    }

    private string GetFFmpegPath()
    {
        string ffmpegPath = Path.Combine(Application.streamingAssetsPath, "Desktop/Win/ffmpeg.exe");
        return ffmpegPath;
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

        UnityWebRequest www = new UnityWebRequest("https://content.dropboxapi.com/2/files/upload", "POST");
        www.uploadHandler = new UploadHandlerRaw(fileData);
        www.downloadHandler = new DownloadHandlerBuffer();

        www.SetRequestHeader("Authorization", $"Bearer {dropboxAccessToken}");
        www.SetRequestHeader("Content-Type", "application/octet-stream");
        www.SetRequestHeader("Dropbox-API-Arg", $"{{\"path\": \"/{fileName}\",\"mode\": \"add\",\"autorename\": true,\"mute\": false}}");

        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
            UpdateStatus("Upload successful!");
        else
            UpdateStatus("Upload failed: " + www.error);

        www.Dispose();
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