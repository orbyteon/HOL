using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

// Force-update gate. One frame after MainMenu starts, asks PlayFab
// TitleData for "minVersion" (the minimum supported app version). If this
// build is older, a blocking dialog sends the player to the store listing.
//
// Fail-open by design: no Title ID configured, login failed, network down,
// or the key missing → the player just plays. The gate only ever engages
// when PlayFab explicitly says the build is too old.
//
// Dashboard: Game Manager → Title Data → add key "minVersion" (e.g. "0.2").
[RequireComponent(typeof(Canvas))]
public class ForceUpdate : MonoBehaviour
{
    const string TitleDataKey = "minVersion";
    const string StoreUrl = "market://details?id=com.Orbyteon.HOL";

    string sessionTicket = "";

    void Start()
    {
        StartCoroutine(CheckNextFrame());
    }

    IEnumerator CheckNextFrame()
    {
        yield return null; // let PvpRuntimeUI add the backend component first

        var pvp = FindFirstObjectByType<PlayFabPvpClient>();
        string titleId = pvp != null ? pvp.titleId.Trim() : "";
        if (string.IsNullOrEmpty(titleId)) yield break; // backend not configured yet

        bool loggedIn = false;
        yield return StartCoroutine(Login(titleId, r => loggedIn = r));
        if (!loggedIn) yield break;

        string minVersion = null;
        yield return StartCoroutine(GetMinVersion(titleId, v => minVersion = v));
        if (string.IsNullOrEmpty(minVersion)) yield break; // no gate published

        if (IsOutdated(Application.version, minVersion))
            ShowBlockingDialog();
    }

    // ---------------------------------------------------------- PlayFab REST

    string Api(string titleId, string method) =>
        "https://" + titleId + ".playfabapi.com/Client/" + method;

    IEnumerator Login(string titleId, Action<bool> done)
    {
        string body = "{\"TitleId\":\"" + titleId + "\",\"CustomId\":\"" +
                      SystemInfo.deviceUniqueIdentifier + "\",\"CreateAccount\":true}";
        yield return StartCoroutine(Post(Api(titleId, "LoginWithCustomID"), body, false, (ok, resp) =>
        {
            if (ok) sessionTicket = ExtractString(resp, "SessionTicket");
            done(ok && !string.IsNullOrEmpty(sessionTicket));
        }));
    }

    IEnumerator GetMinVersion(string titleId, Action<string> done)
    {
        string body = "{\"Keys\":[\"" + TitleDataKey + "\"]}";
        yield return StartCoroutine(Post(Api(titleId, "GetTitleData"), body, true, (ok, resp) =>
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

    IEnumerator Post(string url, string body, bool authed, Action<bool, string> done)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (authed) req.SetRequestHeader("X-Authorization", sessionTicket);

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        done(ok, ok ? req.downloadHandler.text : "");
        req.Dispose();
    }

    static string ExtractString(string json, string name)
    {
        string marker = "\"" + name + "\":\"";
        int i = json.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        i += marker.Length;
        int end = json.IndexOf('"', i);
        return end > i ? json.Substring(i, end - i) : "";
    }

    // ---------------------------------------------------------- version compare

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

    // Segments may carry a non-numeric suffix (e.g. "0.2b"): compare on the
    // leading digits so the tail doesn't collapse the whole segment to 0.
    static int ParseSegment(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;

        int len = 0;
        while (len < s.Length && char.IsDigit(s[len])) len++;
        if (len == 0) return 0;

        int v;
        return int.TryParse(s.Substring(0, len), out v) ? v : 0;
    }

    // ---------------------------------------------------------- blocking dialog

    void ShowBlockingDialog()
    {
        // Scrim on this object's own canvas (sorting order 5, above the menu).
        var panel = RuntimeUI.FullscreenPanel(transform, "ForceUpdatePanel",
            ConvergingLight.WithAlpha(ConvergingLight.ScrimIndigo, 0.96f));

        var card = RuntimeUI.CreateObject("Card", panel.transform);
        ConvergingLight.Center(card, Vector2.zero, new Vector2(640f, 460f));
        var cardImage = card.AddComponent<Image>();
        cardImage.sprite = RuntimeUI.RoundedRectSprite;
        cardImage.type = Image.Type.Sliced;
        cardImage.color = ConvergingLight.PanelIndigo;

        RuntimeUI.CreateText(card.transform, "Message", L10n.Get("update_required"),
            34, new Vector2(0f, 80f), new Vector2(560f, 200f));

        var update = RuntimeUI.CreateButton(card.transform, "UpdateButton",
            L10n.Get("update_now"), new Vector2(0f, -110f), new Vector2(420f, 100f),
            ConvergingLight.Gold, ConvergingLight.WithAlpha(ConvergingLight.PanelIndigo, 1f));
        update.onClick.AddListener(OpenStore);
    }

    void OpenStore()
    {
        Application.OpenURL(StoreUrl); // Play Store listing
    }
}
