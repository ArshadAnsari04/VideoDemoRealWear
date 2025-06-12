using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideoWithRealWear;
using UnityEngine;

public class InviteButtonHelper : MonoBehaviour
{
    public JoinChannelVideoWithRealWear joinChannelVideoWithRealWear; // Assign in Inspector
    public uint remoteUid; // Set this in Inspector or via code

    public void OnButtonClick()
    {
        //if (joinChannelVideoWithRealWear != null)
        //    joinChannelVideoWithRealWear.OnInviteButtonClicked(remoteUid);
    }
}