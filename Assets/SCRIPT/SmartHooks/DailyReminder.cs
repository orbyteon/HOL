using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

// One respectful local notification: when tomorrow's Daily Hunt unlocks.
// The permission prompt is contextual — it appears only after the player
// completes their first hunt, the moment the reminder's value is obvious.
// A decline is remembered and never re-prompted (the OS settings remain
// the master switch). Scheduling is rebuilt on every completion and every
// launch, so at most one reminder is ever pending. No-op off Android.
public static class DailyReminder
{
    const string AskedKey = "DailyReminderAsked";
    const string ChannelId = "daily_hunt";
    const int ReminderHour = 18; // local evening, when tomorrow's hunt is live

#if UNITY_ANDROID
    static bool channelReady;

    static void EnsureChannel()
    {
        if (channelReady) return;
        AndroidNotificationCenter.RegisterNotificationChannel(new AndroidNotificationChannel
        {
            Id = ChannelId,
            Name = "Daily Hunt",
            Importance = Importance.Default,
            Description = "Daily challenge reminder",
        });
        channelReady = true;
    }

    static void Schedule()
    {
        EnsureChannel();
        AndroidNotificationCenter.CancelAllScheduledNotifications();

        AndroidNotificationCenter.SendNotification(new AndroidNotification
        {
            Title = L10n.Get("notif_daily_title"),
            Text = L10n.Get("notif_daily_body"),
            FireTime = DateTime.Now.Date.AddDays(1).AddHours(ReminderHour),
        }, ChannelId);
    }

    static IEnumerator RequestThenSchedule()
    {
        var request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
            yield return null;
        if (request.Status == PermissionStatus.Allowed)
            Schedule();
    }
#endif

    // Called each launch (ExtrasRuntimeWiring): refresh the pending reminder
    // if permission already exists. Never prompts.
    public static void EnsureScheduled()
    {
#if UNITY_ANDROID
        if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed)
            Schedule();
#endif
    }

    // Called by DailyHunt when a hunt finishes. First completion asks for
    // permission (coroutine host = the hunt panel); afterwards it only
    // reschedules.
    public static void OnHuntCompleted(MonoBehaviour host)
    {
#if UNITY_ANDROID
        if (AndroidNotificationCenter.UserPermissionToPost == PermissionStatus.Allowed)
        {
            Schedule();
            return;
        }

        if (PlayerPrefs.GetInt(AskedKey, 0) == 1)
            return; // asked once before — respect the answer

        PlayerPrefs.SetInt(AskedKey, 1);
        PlayerPrefs.Save();
        if (host != null && host.isActiveAndEnabled)
            host.StartCoroutine(RequestThenSchedule());
#endif
    }
}
