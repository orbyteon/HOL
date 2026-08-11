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
// the joiner. Guess submission is also server-authoritative (submitGuess),
// closing the leave-vs-guess last-write-wins race. See playfab/cloudscript.js
// in the repo.
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
        StartCoroutine(Login(done));
    }

    // Anonymous device-bound login; shared by EnsureLogin and the 401 retry.
    IEnumerator Login(Action<bool> done)
    {
        yield return StartCoroutine(Post(Api("LoginWithCustomID"), LoginBody(), false, (ok, resp) =>
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

    string LoginBody()
    {
        return "{\"TitleId\":\"" + titleId.Trim() + "\",\"CustomId\":\"" +
               SystemInfo.deviceUniqueIdentifier + "\",\"CreateAccount\":true}";
    }

    // ------------------------------------------------ create / join

    public override void CreateRoom(string hostName, int hostSecret, Action<bool, string> done)
    {
        EnsureLogin(ok =>
        {
            if (!ok) { done?.Invoke(false, L10n.Get("pvp_network_error")); return; }

            var code = GenerateCode();
            StartCoroutine(Post(Api("CreateSharedGroup"), "{\"SharedGroupId\":\"" + code + "\"}", true, (ok2, _) =>
            {
                if (!ok2) { done?.Invoke(false, L10n.Get("pvp_network_error")); return; }

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
                    done?.Invoke(ok3, ok3 ? code : L10n.Get("pvp_network_error"));
                });
            }));
        });
    }

    public override void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        EnsureLogin(ok =>
        {
            if (!ok) { done?.Invoke(false, L10n.Get("pvp_network_error")); return; }

            // CloudScript joins us AND claims the guest slot atomically
            // (server authority) — no client-side check-then-write race.
            string body = "{\"FunctionName\":\"joinRoom\",\"FunctionParameter\":{\"roomId\":\"" + code +
                          "\",\"guestName\":\"" + EscapeJson(guestName) +
                          "\",\"guestSecret\":" + guestSecret + "}}";
            StartCoroutine(Post(Api("ExecuteCloudScript"), body, true, (ok2, resp) =>
            {
                if (!ok2)
                {
                    done?.Invoke(false, L10n.Get("pvp_network_error"));
                    return;
                }
                if (resp.Contains("\"room full\""))
                {
                    done?.Invoke(false, L10n.Get("pvp_room_full"));
                    return;
                }
                if (resp.Contains("\"room not found\"") || resp.Contains("\"room corrupt\"") || resp.Contains("\"bad secret\""))
                {
                    done?.Invoke(false, L10n.Get("pvp_room_not_found"));
                    return;
                }
                if (!resp.Contains("\"ok\":true"))
                {
                    done?.Invoke(false, L10n.Get("pvp_network_error"));
                    return;
                }

                RoomCode = code;
                IsHost = false;
                done?.Invoke(true, "");
            }));
        });
    }

    // ------------------------------------------------ gameplay

    public override void SubmitGuess(int guess, RoomState current, Action<bool> done)
    {
        // Server-authoritative submit (CloudScript submitGuess): the turn and
        // phase checks plus the state write happen atomically server-side, so
        // a leaver's closed-write can't erase a landed guess and this stale
        // client can't resurrect a closed room. REQUIRES the cloudscript.js
        // revision with handlers.submitGuess deployed — without it every
        // submit fails gracefully via done(false).
        string body = "{\"FunctionName\":\"submitGuess\",\"FunctionParameter\":{\"roomId\":\"" + RoomCode +
                      "\",\"side\":\"" + (IsHost ? "host" : "guest") +
                      "\",\"guess\":" + guess + "}}";
        StartCoroutine(Post(Api("ExecuteCloudScript"), body, true, (ok, resp) =>
        {
            // Any failure (network, or a server-side reject like "not your
            // turn") collapses onto the existing done(false) contract.
            if (!ok || !resp.Contains("\"ok\":true"))
            {
                done?.Invoke(false);
                return;
            }

            // Apply the server-returned state onto the caller's cached
            // snapshot so the next poll can't regress it to the pre-guess
            // state. (This is the only state mutation left on this path.)
            string inner = ExtractStateJson(resp);
            if (!string.IsNullOrEmpty(inner))
            {
                try
                {
                    var applied = JsonUtility.FromJson<RoomState>(inner);
                    if (applied != null)
                    {
                        current.turn = applied.turn;
                        current.phase = applied.phase;
                        current.lastGuess = applied.lastGuess;
                        current.lastBy = applied.lastBy;
                        current.winner = applied.winner;
                    }
                }
                catch { }
            }
            done?.Invoke(true);
        }));
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
        bool ok = false, expired = false;
        string text = "";
        yield return StartCoroutine(PostOnce(url, body, authed, (o, t, e) =>
        {
            ok = o; text = t; expired = e;
        }));

        // Session tickets expire (~24h) while EnsureLogin caches the first
        // one: after expiry every authed call would 401 until app restart
        // (and the poller would falsely report connection-lost). On a 401,
        // re-login once and retry the request once, then give up.
        if (!ok && expired && authed)
        {
            sessionTicket = "";
            bool loggedIn = false;
            yield return StartCoroutine(Login(r => loggedIn = r));
            if (loggedIn)
                yield return StartCoroutine(PostOnce(url, body, authed, (o, t, e2) =>
                {
                    ok = o; text = t;
                }));
        }

        done?.Invoke(ok, text);
    }

    IEnumerator PostOnce(string url, string body, bool authed, Action<bool, string, bool> done)
    {
        var req = new UnityWebRequest(url, "POST");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        if (authed) req.SetRequestHeader("X-Authorization", sessionTicket);
        req.timeout = 10; // a stalled connection must fail, not freeze the poller

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        string text = req.downloadHandler.text ?? "";
        bool expired = req.responseCode == 401 || text.Contains("InvalidSessionTicket");
        if (!ok) Debug.Log("PlayFab request failed: " + req.error + " " + text);
        done?.Invoke(ok, text, expired);
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
        return UnescapeJsonValue(resp, i + marker.Length);
    }

    // CloudScript's FunctionResult carries the state as a plain
    // "state":"..." string (no "Value" wrapper like GetSharedGroupData has) —
    // same escape-aware extraction, different anchor.
    static string ExtractStateJson(string resp)
    {
        string marker = "\"state\":\"";
        int i = resp.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        return UnescapeJsonValue(resp, i + marker.Length);
    }

    // Reads a JSON string value starting at index i, resolving escapes and
    // stopping at the closing unescaped quote.
    static string UnescapeJsonValue(string resp, int i)
    {
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
                else if (n == 'u' && i + 6 <= resp.Length)
                {
                    // \uXXXX — PlayFab escapes non-ASCII (Greek player
                    // names!) as unicode sequences; without this branch
                    // they surface as literal "u039A..." garbage.
                    int code;
                    if (int.TryParse(resp.Substring(i + 2, 4),
                            System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out code))
                    {
                        sb.Append((char)code);
                        i += 6;
                        continue;
                    }
                    sb.Append(n); // malformed escape: keep the literal
                }
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
