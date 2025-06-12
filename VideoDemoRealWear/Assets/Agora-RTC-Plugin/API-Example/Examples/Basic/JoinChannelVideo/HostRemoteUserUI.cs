using UnityEngine;
using UnityEngine.UI;
using TMPro;
/// <summary>
/// Attach this script to your Host Remote User Prefab.
/// It exposes references to the call button, the button's text, and a name text field.
/// </summary>
public class HostRemoteUserUI : MonoBehaviour
{
    [Header("Assign in Inspector")]
    public Button callButton;      // The button to connect/disconnect
    public TextMeshProUGUI callButtonText;    // The text on the call button ("Connect"/"Connected")
    public TextMeshProUGUI nameText;          // The text showing the user's name or UID

    /// <summary>
    /// Set the display name for this remote user.
    /// </summary>
    public void SetUserName(string userName)
    {
        if (nameText != null)
            nameText.text = userName;
    }

    /// <summary>
    /// Set the call button's text.
    /// </summary>
    public void SetCallButtonText(string text)
    {
        if (callButtonText != null)
            callButtonText.text = text;
    }

    /// <summary>
    /// Enable or disable the call button.
    /// </summary>
    public void SetCallButtonInteractable(bool interactable)
    {
        if (callButton != null)
            callButton.interactable = interactable;
    }
}