using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Ships finished-match outcomes to PlayFab so the 0.3.0 duel rules can be read
// from evidence rather than from reviews.
//
// The rules were tuned against a simulation: the opener coin flip took the
// first-mover advantage from 63.7% down to about even, and draws sit near 7% at
// human accuracy but climb toward 23% as both players approach a flawless
// binary search. That last number is the one that matters and the one a
// simulation cannot settle, because it moves with the real population's skill.
// Without this, the first signal would be a review.
//
// Installs itself rather than living in a scene, the way ReleaseBootstrap does,
// so no scene wiring can silently drop it. Every failure path is a no-op:
// telemetry must never cost a player a match.
public class MatchTelemetry : MonoBehaviour
{
    const string EventName = "match_completed";
    const int TimeoutSeconds = 10;

    static MatchTelemetry instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (instance != null) return;

        var go = new GameObject("MatchTelemetry");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MatchTelemetry>();
    }

    void OnEnable()
    {
        GameEvents.OnMatchCompleted += Record;
    }

    void OnDisable()
    {
        GameEvents.OnMatchCompleted -= Record;
    }

    void Record(MatchOutcome outcome)
    {
        if (!isActiveAndEnabled) return;
        StartCoroutine(Send(outcome));
    }

    IEnumerator Send(MatchOutcome outcome)
    {
        var pvp = FindFirstObjectByType<PlayFabPvpClient>();
        string titleId = pvp != null ? pvp.titleId.Trim() : "";
        if (string.IsNullOrEmpty(titleId)) yield break;

        // Reuses the session this launch already holds — ForceUpdate signs in
        // at startup for the version check — and never triggers a login itself.
        // Analytics must not be the reason a player who only plays offline ends
        // up with a provisioned PlayFab account.
        if (!pvp.HasSession) yield break;

        bool authFinished = false;
        string ticket = "";
        pvp.GetSessionTicket((ok, t) =>
        {
            ticket = ok ? t : "";
            authFinished = true;
        });
        while (!authFinished) yield return null;
        if (string.IsNullOrEmpty(ticket)) yield break;

        // Dropped rather than queued: a queue that outlives the process is a
        // privacy surface of its own, and a lost row costs a dashboard nothing.

        string url = "https://" + titleId + ".playfabapi.com/Client/" + "WritePlayerEvent";
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(outcome.PlayerEventJson(EventName)));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Authorization", ticket);
        req.timeout = TimeoutSeconds;

        yield return req.SendWebRequest();

        // Nothing retries and nothing surfaces. A dropped event costs a row in
        // a dashboard; a retry loop on a dead title costs battery and a
        // player's patience.
        if (req.result != UnityWebRequest.Result.Success && Debug.isDebugBuild)
            Debug.Log("Match telemetry not recorded: " + req.error);

        req.Dispose();
    }
}
