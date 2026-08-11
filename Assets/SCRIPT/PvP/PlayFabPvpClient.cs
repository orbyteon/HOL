using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// PvP transport on Microsoft Azure PlayFab, using the plain REST Client API —
// no SDK import needed. Rooms are Shared Group Data objects whose id is the
// invite code; the whole room state lives in one Public key ("state").
//
// Joining uses a one-line CloudScript function, because PlayFab's rule is
// "only members can add members" — the CloudScript (server authority) adds
// the joiner. See playfab/cloudscript.js in the repo.
//
// SETUP (one-time, free):
//   1. developer.playfab.com → sign in with a Microsoft account → create a
//      Studio (e.g. "Orbyteon") and a Title ("HOL"). Copy the Title ID
//      (4-6 hex chars shown in Game Manager).
//   2. Game Manager → Automation → CloudScript (Legacy) → Revisions:
//      paste the contents of playfab/cloudscript.js → Save → Deploy.
//   3. Paste the Title ID into this component's "Title Id" field.
public class PlayFabPvpClient : PvpBackend
{
    [Tooltip("Your PlayFab Title ID from Game Manager (e.g. 1A2B3)")]
    public string titleId = "";

    public float pollIntervalSeconds = 1.5f;

    string sessionTicket = "";
    string playFabId = "";
    Coroutine pollRoutine;

    string Api(string method) => "https://" + titleId.Trim() + ".playfabapi.com/Client/" + method;

    // ------------------------------------------------ login (anonymous, device-bound)

    void EnsureLogin(Action<bool> done)
    {
        if (!string.IsNullOrEmpty(sessionTicket)) { done(true); return; }

        string body = "{\"TitleId\":\"" + titleId.Trim() + "\",\"CustomId\":\"" +
                      SystemInfo.deviceUniqueIdentifier + "\",\"CreateAccount\":true}";

        StartCoroutine(Post(Api("LoginWithCustomID"), body, false, (ok, resp) =>
        {
            if (ok)
            {
                sessionTicket = ExtractString(resp, "SessionTicket");
                playFabId = ExtractString(resp, "PlayFabId");
                ok = !string.IsNullOrEmpty(sessionTicket);
            }
            done(ok);
        }));
    }

    // ------------------------------------------------ create / join

    public override void CreateRoom(string hostName, int hostSecret, Action<bool, string> done)
    {
        EnsureLogin(ok =>
        {
            if (!ok) { done?.Invoke(false, "Could not sign in. Check connection / Title ID."); return; }

            var code = GenerateCode();
            StartCoroutine(Post(Api("CreateSharedGroup"), "{\"SharedGroupId\":\"" + code + "\"}", true, (ok2, _) =>
            {
                if (!ok2) { done?.Invoke(false, "Could not create room."); return; }

                var state = new RoomState
                {
                    hostName = hostName,
                    hostSecret = hostSecret,
                    turn = "guest",
                    phase = "waiting",
                };
                WriteState(code, state, ok3 =>
                {
                    if (ok3) { RoomCode = code; IsHost = true; }
                    done?.Invoke(ok3, ok3 ? code : "Could not initialize room.");
                });
            }));
        });
    }

    public override void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        EnsureLogin(ok =>
        {
            if (!ok) { done?.Invoke(false, "Could not sign in. Check connection / Title ID."); return; }

            // CloudScript adds us as a member (server authority).
            string body = "{\"FunctionName\":\"joinRoom\",\"FunctionParameter\":{\"roomId\":\"" + code + "\"}}";
            StartCoroutine(Post(Api("ExecuteCloudScript"), body, true, (ok2, resp) =>
            {
                if (!ok2 || resp.Contains("\"Error\"") && !resp.Contains("\"Error\":null"))
                {
                    done?.Invoke(false, "Room not found. Check the code.");
                    return;
                }

                ReadState(code, (ok3, state) =>
                {
                    if (!ok3 || state == null || string.IsNullOrEmpty(state.hostName))
                    {
                        done?.Invoke(false, "Room not found. Check the code.");
                        return;
                    }
                    if (!string.IsNullOrEmpty(state.guestName))
                    {
                        done?.Invoke(false, "Room is already full.");
                        return;
                    }

                    state.guestName = guestName;
                    state.guestSecret = guestSecret;
                    state.phase = "play";
                    WriteState(code, state, ok4 =>
                    {
                        if (ok4) { RoomCode = code; IsHost = false; }
                        done?.Invoke(ok4, ok4 ? "" : "Could not join. Try again.");
                    });
                });
            }));
        });
    }

    // ------------------------------------------------ gameplay

    public override void SubmitGuess(int guess, RoomState current, Action<bool> done)
    {
        string me = IsHost ? "host" : "guest";
        int opponentSecret = IsHost ? current.guestSecret : current.hostSecret;

        current.lastGuess = guess;
        current.lastBy = me;
        if (guess == opponentSecret)
        {
            current.phase = "done";
            current.winner = me;
        }
        else
        {
            current.turn = IsHost ? "guest" : "host";
        }
        WriteState(RoomCode, current, done);
    }

    public override void StartPolling(Action<RoomState> onState)
    {
        StopPolling();
        pollRoutine = StartCoroutine(Poll(onState));
    }

    public override void StopPolling()
    {
        if (pollRoutine != null) StopCoroutine(pollRoutine);
        pollRoutine = null;
    }

    public override void DeleteRoom()
    {
        // Shared groups cannot be deleted from the Client API; mark the room
        // closed instead so the other player's poller sees phase == "closed".
        if (!string.IsNullOrEmpty(RoomCode))
        {
            string code = RoomCode;
            ReadState(code, (ok, state) =>
            {
                if (ok && state != null)
                {
                    state.phase = "closed";
                    WriteState(code, state, _ => { });
                }
            });
        }
        RoomCode = "";
    }

    IEnumerator Poll(Action<RoomState> onState)
    {
        const int maxConsecutiveFailures = 10;
        int failures = 0;

        while (true)
        {
            bool finished = false;
            bool ok = false;
            RoomState state = null;
            ReadState(RoomCode, (o, s) =>
            {
                ok = o; state = s;
                finished = true;
            });
            while (!finished) yield return null;

            if (!ok || state == null)
            {
                // Network/backend failure: back off, and give up (loudly)
                // after too many in a row instead of hammering forever.
                if (++failures >= maxConsecutiveFailures)
                {
                    pollRoutine = null;
                    OnConnectionLost?.Invoke();
                    yield break;
                }
                yield return new WaitForSeconds(
                    Mathf.Min(pollIntervalSeconds * (1f + failures * 0.5f), 10f));
                continue;
            }

            failures = 0;
            onState?.Invoke(state);
            yield return new WaitForSeconds(pollIntervalSeconds);
        }
    }

    // ------------------------------------------------ state read/write

    void WriteState(string code, RoomState state, Action<bool> done)
    {
        string json = JsonUtility.ToJson(state);
        string body = "{\"SharedGroupId\":\"" + code + "\",\"Data\":{\"state\":\"" +
                      EscapeJson(json) + "\"},\"Permission\":\"Public\"}";
        StartCoroutine(Post(Api("UpdateSharedGroupData"), body, true, (ok, _) => done?.Invoke(ok)));
    }

    void ReadState(string code, Action<bool, RoomState> done)
    {
        string body = "{\"SharedGroupId\":\"" + code + "\",\"Keys\":[\"state\"]}";
        StartCoroutine(Post(Api("GetSharedGroupData"), body, true, (ok, resp) =>
        {
            RoomState state = null;
            if (ok)
            {
                string inner = ExtractStateValue(resp);
                if (!string.IsNullOrEmpty(inner))
                {
                    try { state = JsonUtility.FromJson<RoomState>(inner); }
                    catch { }
                }
            }
            done?.Invoke(ok && state != null, state);
        }));
    }

    // ------------------------------------------------ HTTP + JSON plumbing

    IEnumerator Post(string url, string body, bool authed, Action<bool, string> done)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (authed) req.SetRequestHeader("X-Authorization", sessionTicket);

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        string text = req.downloadHandler.text ?? "";
        if (!ok) Debug.Log("PlayFab request failed: " + req.error + " " + text);
        done?.Invoke(ok, text);
        req.Dispose();
    }

    // Pulls "Name":"value" out of a JSON blob (top-level string values only).
    static string ExtractString(string json, string name)
    {
        string marker = "\"" + name + "\":\"";
        int i = json.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        i += marker.Length;
        int end = json.IndexOf('"', i);
        return end > i ? json.Substring(i, end - i) : "";
    }

    // Finds the "state" entry's "Value":"..." and unescapes the embedded JSON.
    static string ExtractStateValue(string resp)
    {
        int s = resp.IndexOf("\"state\"", StringComparison.Ordinal);
        if (s < 0) return "";
        string marker = "\"Value\":\"";
        int i = resp.IndexOf(marker, s, StringComparison.Ordinal);
        if (i < 0) return "";
        i += marker.Length;

        var sb = new StringBuilder();
        while (i < resp.Length)
        {
            char c = resp[i];
            if (c == '\\' && i + 1 < resp.Length)
            {
                char n = resp[i + 1];
                if (n == '"') sb.Append('"');
                else if (n == '\\') sb.Append('\\');
                else if (n == 'n') sb.Append('\n');
                else if (n == 't') sb.Append('\t');
                else sb.Append(n);
                i += 2;
                continue;
            }
            if (c == '"') break; // unescaped quote = end of value
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    static string EscapeJson(string s)
    {
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
