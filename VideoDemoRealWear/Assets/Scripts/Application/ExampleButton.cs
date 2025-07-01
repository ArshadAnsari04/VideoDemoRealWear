
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using WearHFPlugin;

/// <summary>
/// Script used for navigating to each example from the home screen.
/// </summary>
public class ExampleButton : MonoBehaviour
{
    /// <summary>
    /// The scene to load when activated.
    /// </summary>
    public string m_scene;

    private WearHF m_wearHf;

    /// <summary>
    /// See Unity docs.
    /// </summary>
    void Start()
    {
        Button button = GetComponentInChildren<Button>();
        button.onClick.AddListener(OnApplicationClose);

        string buttonText = GetComponentInChildren<Text>().text;

        m_wearHf = GameObject.Find("WearHF Manager").GetComponent<WearHF>();
        m_wearHf.AddVoiceCommand(buttonText, VoiceCommandCallback);
    }

    /// <summary>
    /// Called when a voice command is recognized.
    /// </summary>
    /// <param name="voiceCommand">The voice command that was recognized.</param>
    private void VoiceCommandCallback(string voiceCommand)
    {
        OnApplicationClose();
       // ChangeScene();
    }

    public void OnApplicationClose()
    {
        m_wearHf.ClearCommands();
        Application.Quit();
    }

    /// <summary>
    /// Load the configured scene.
    /// </summary>
    public void ChangeScene()
    {
        m_wearHf.ClearCommands();
        SceneManager.LoadSceneAsync(m_scene);
    }
}