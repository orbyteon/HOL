using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Force-update gate. Reuses PlayFabPvpClient's authenticated session so PvP
// and version checks share one identity/auth policy.
[RequireComponent(typeof(Canvas))]
public class ForceUpdate : MonoBehaviour
{
    const string TitleDataKey = "minVersion";
    const string StoreUrl = "https://play.google.com/store/apps/details?id=com.Orbyteon.HOL";

    string sessionTicket = "";

    void Start()
    {
        StartCoroutine(CheckNextFrame());
    }

    IEnumerator CheckNextFrame()
    {
        yield return null; // let PvpRuntimeUI add the PlayFab backend first

        var pvp = FindFirstObjectByType<PlayFabPvpClient>();
        string titleId = pvp != null ? pvp.titleId.Trim() : "";
        if (string.IsNullOrEmpty(titleId)) yield break;

        bool authFinished = false;
        bool loggedIn = false;
        pvp.GetSessionTicket((ok, ticket) =>
        {
            loggedIn = ok && !string.IsNullOrEmpty(ticket);
            sessionTicket = ticket;
            authFinished = true;
        });
        while (!authFinished) yield return null;
        if (!loggedIn) yield break;

        string minVersion = null;
        yield return StartCoroutine(GetMinVersion(titleId, v => minVersion = v));
        if (string.IsNullOrEmpty(minVersion)) yield break;

        if (IsOutdated(Application.version, minVersion))
            ShowBlockingDialog();
    }

    string Api(string titleId, string method) =>
        "https://" + titleId + ".playfabapi.com/Client/" + method;

    IEnumerator GetMinVersion(string titleId, Action<string> done)
    {
        string body = "{\"Keys\":[\"" + TitleDataKey + "\"]}";
        yield return StartCoroutine(Post(Api(titleId, "GetTitleData"), body, (ok, resp) =>
        {
            string value = null;
            if (ok)
            {
                int i = resp.IndexOf("\"" + TitleDataKey + "\":\"", StringComparison.Ordinal);
                if (i >= 0)
                {
                    i += TitleDataKey.Length + 4;
                    int end = resp.IndexOf('"', i);
                    if (end > i) value = resp.Substring(i, end - i);
                }
            }
            done(value);
        }));
    }

    IEnumerator Post(string url, string body, Action<bool, string> done)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-Authorization", sessionTicket);
        req.timeout = 10;

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        done(ok, ok ? req.downloadHandler.text : "");
        req.Dispose();
    }

    // "0.1" vs "0.2" style numeric versions, segment by segment.
    static bool IsOutdated(string current, string minimum)
    {
        var c = (current ?? "0").Split('.');
        var m = (minimum ?? "0").Split('.');
        for (int i = 0; i < Mathf.Max(c.Length, m.Length); i++)
        {
            int cv = i < c.Length ? ParseSegment(c[i]) : 0;
            int mv = i < m.Length ? ParseSegment(m[i]) : 0;
            if (cv < mv) return true;
            if (cv > mv) return false;
        }
        return false;
    }

    static int ParseSegment(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int len = 0;
        while (len < s.Length && char.IsDigit(s[len])) len++;
        if (len == 0) return 0;

        int v;
        return int.TryParse(s.Substring(0, len), out v) ? v : 0;
    }

    void ShowBlockingDialog()
    {
        var panel = RuntimeUI.FullscreenPanel(transform, "ForceUpdatePanel",
            ConsumerTokens.WithAlpha(ConsumerTokens.Background0, 0.96f));

        var card = NeonFrame.Frame(panel.transform, "Card", Vector2.zero,
            new Vector2(640f, 560f), ConsumerTokens.Gold, 0.97f, true,
            ConsumerTokens.Surface);

        RuntimeUI.CreateText(card.transform, "Message", L10n.Get("update_required"),
            34, new Vector2(0f, 80f), new Vector2(560f, 200f));

        // "Confirm" keeps this on the primary design sprite; gold is this
        // screen's one action that matters.
        var update = RuntimeUI.CreateButton(card.transform, "ConfirmUpdateButton",
            L10n.Get("update_now"), new Vector2(0f, -110f), new Vector2(420f, 100f),
            ConsumerTokens.Gold, ConsumerTokens.WithAlpha(ConsumerTokens.Surface, 1f));
        update.onClick.AddListener(OpenStore);

        var quit = RuntimeUI.CreateButton(card.transform, "QuitButton",
            L10n.Get("quit"), new Vector2(0f, -220f), new Vector2(420f, 100f),
            ConsumerTokens.SurfaceElevated);
        quit.onClick.AddListener(Application.Quit);
    }

    void OpenStore()
    {
        Application.OpenURL(StoreUrl);
    }
}
