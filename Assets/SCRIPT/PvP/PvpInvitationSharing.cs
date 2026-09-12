using UnityEngine;

// Launching a chooser proves neither recipient selection nor message delivery.
// Never emits RoomShared or changes room/account state. Clipboard is a separate
// deliberate action owned by the controller.
public static class PvpInvitationSharing
{
    public static bool OpenChooser(string invitation, string chooserTitle)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.SEND"))
            {
                using (intent.Call<AndroidJavaObject>("setType", "text/plain")) { }
                using (intent.Call<AndroidJavaObject>("putExtra", "android.intent.extra.TEXT", invitation)) { }
                using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, chooserTitle))
                    activity.Call("startActivity", chooser);
            }
            return true;
        }
        catch (AndroidJavaException)
        {
            // Do not log invitation content or platform exception payloads.
            Debug.LogWarning("HOL: Android sharing unavailable; clipboard action remains available.");
        }
#endif
        return false;
    }
}
