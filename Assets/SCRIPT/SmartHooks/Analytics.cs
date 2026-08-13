using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Anonymous, fire-and-forget gameplay telemetry through PlayFab's client
// event pipeline (Client/WritePlayerEvent) — the same anonymous session the
// PvP and force-update flows already establish. No extra SDKs and no device
// profiling: a handful of gameplay counters (session start, match results,
// daily-hunt results) so product decisions stop being guesses. If PlayFab
// isn't in the scene, isn't configured, or login fails (fresh release
// installs before provisioning ships), events are dropped silently —
// telemetry must never affect play. Disclosed in docs/privacy.html.
public static class Analytics
{
    const int MaxQueue = 32;
    const float SendTimeoutSeconds = 15f;

    class Pump : MonoBehaviour { }

    static Pump pump;
    static readonly Queue<string[]> queue = new Queue<string[]>(); // {name, body}
    static bool draining;
    static string matchMode = "solo";

    // Called once per scene load by ExtrasRuntimeWiring; the pump object
    // survives reloads, the subscription happens exactly once.
    public static void Boot()
    {
        if (pump != null) return;

        var go = new GameObject("AnalyticsPump");
        Object.DontDestroyOnLoad(go);
        pump = go.AddComponent<Pump>();

        GameEvents.OnMatchEnded += MatchEnded;
        Track("hol_session_start", "{}");
    }

    // PvP tags itself before its result fires; solo is the default. The tag
    // resets after every match so a stale mode can't leak.
    public static void SetMatchMode(string mode)
    {
        matchMode = mode;
    }

    public static void DailyEnded(int day, bool found, int guesses, bool revived)
    {
        Track("hol_daily_end", "{\"day\":" + day +
            ",\"found\":" + (found ? "true" : "false") +
            ",\"guesses\":" + guesses +
            ",\"revived\":" + (revived ? "true" : "false") + "}");
    }

    static void MatchEnded(bool won, int guesses)
    {
        Track("hol_match_end", "{\"mode\":\"" + matchMode +
            "\",\"won\":" + (won ? "true" : "false") +
            ",\"guesses\":" + guesses + "}");
        matchMode = "solo";
    }

    static void Track(string eventName, string bodyJson)
    {
        if (pump == null || queue.Count >= MaxQueue) return;
        queue.Enqueue(new[] { eventName, bodyJson });
        if (!draining) pump.StartCoroutine(Drain());
    }

    static IEnumerator Drain()
    {
        draining = true;
        while (queue.Count > 0)
        {
            // Re-resolved per event: the scene-local client dies on reload.
            var client = Object.FindObjectOfType<PlayFabPvpClient>();
            if (client == null || string.IsNullOrEmpty(client.titleId))
            {
                queue.Clear(); // not configured — drop, never block play
                break;
            }

            var evt = queue.Dequeue();
            bool finished = false;
            client.WritePlayerEvent(evt[0], evt[1], _ => finished = true);

            // A scene reload can kill the client's coroutine mid-send and
            // strand `finished` — time out instead of draining forever.
            float waited = 0f;
            while (!finished && waited < SendTimeoutSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        draining = false;
    }
}
