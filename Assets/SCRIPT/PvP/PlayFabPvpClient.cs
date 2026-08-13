using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// PlayFab PvP transport using Client/ExecuteCloudScript only for room access.
// The CloudScript revision in playfab/cloudscript.js owns every Shared Group
// read/write. Client Shared Group Data APIs can therefore be disabled in the
// PlayFab API Access Policy before release.
public class PlayFabPvpClient : PvpBackend
{
    // CloudScript adjudicates every room operation, so the Lock and Signals
    // can be trusted here.
    public override bool IsServerAuthoritative { get { return true; } }

    [Tooltip("Your PlayFab Title ID from Game Manager (e.g. 1A2B3)")]
    public string titleId = "";

    public float pollIntervalSeconds = 1.5f;

    [Tooltip("Debug builds may create anonymous players client-side for local testing. Release builds always use CreateAccount=false and require server-side provisioning.")]
    public bool allowClientAccountCreationInDebugBuilds = true;

    [Header("Production first-install provisioning")]
    [Tooltip("Optional debug/local override. Empty uses ReleaseConfig.ProvisioningUrl in a configured production build.")]
    public string provisioningUrl = "";

    [Tooltip("Optional debug/local override. Zero uses the required positive ReleaseConfig.GoogleCloudProjectNumber in production.")]
    public long googleCloudProjectNumber;

    string sessionTicket = "";
    Coroutine pollRoutine;
    PlayIntegrityProvisioner provisioner;

    string Api(string method) => "https://" + titleId.Trim() + ".playfabapi.com/Client/" + method;
    string CustomId => SystemInfo.deviceUniqueIdentifier;

    // ------------------------------------------------ login (anonymous, device-bound)

    void EnsureLogin(Action<bool> done)
    {
        if (!string.IsNullOrEmpty(sessionTicket)) { done(true); return; }
        StartCoroutine(Login(done));
    }

    // Release builds never create accounts from the client. If a fresh release
    // install has no linked Custom ID, an attested server bootstrap creates it
    // and this method retries the same CreateAccount=false login once.
    IEnumerator Login(Action<bool> done, bool allowProvisioning = true)
    {
        bool requestOk = false;
        string response = "";
        yield return StartCoroutine(Post(Api("LoginWithCustomID"), LoginBody(), false, (ok, resp) =>
        {
            requestOk = ok;
            response = resp ?? "";
        }));

        if (requestOk)
        {
            sessionTicket = ExtractString(response, "SessionTicket");
            done(!string.IsNullOrEmpty(sessionTicket));
            yield break;
        }

        bool clientCreates = ClientAccountCreationEnabled();
        bool missingAccount = response.Contains("AccountNotFound") || response.Contains("PlayerCreationDisabled");
        if (!clientCreates && allowProvisioning && missingAccount)
        {
            bool provisionFinished = false;
            bool provisioned = false;
            var bootstrap = GetProvisioner();
            bootstrap.Provision(CustomId, ok =>
            {
                provisioned = ok;
                provisionFinished = true;
            });
            while (!provisionFinished) yield return null;

            if (provisioned)
            {
                yield return StartCoroutine(Login(done, false));
                yield break;
            }

            Debug.LogError("PlayFab first-install provisioning failed; login remains closed.");
            done(false);
            yield break;
        }

        if (response.Contains("PlayerCreationDisabled"))
            Debug.LogError("PlayFab client-side account creation is disabled. Use the production provisioning flow.");
        else if (response.Contains("AccountNotFound"))
            Debug.LogError("No PlayFab account is linked to this Custom ID.");

        done(false);
    }

    bool ClientAccountCreationEnabled()
    {
        return Debug.isDebugBuild && allowClientAccountCreationInDebugBuilds;
    }

    string LoginBody()
    {
        return "{\"TitleId\":\"" + titleId.Trim() + "\",\"CustomId\":\"" +
               EscapeJson(CustomId) + "\",\"CreateAccount\":" +
               (ClientAccountCreationEnabled() ? "true" : "false") + "}";
    }

    PlayIntegrityProvisioner GetProvisioner()
    {
        if (provisioner == null)
            provisioner = GetComponent<PlayIntegrityProvisioner>();
        if (provisioner == null)
            provisioner = gameObject.AddComponent<PlayIntegrityProvisioner>();

        provisioner.endpointUrl = provisioningUrl;
        provisioner.cloudProjectNumber = googleCloudProjectNumber;
        return provisioner;
    }

    // ForceUpdate reuses the same session instead of creating a second account
    // login path with slightly different behavior.
    public void GetSessionTicket(Action<bool, string> done)
    {
        EnsureLogin(ok => done?.Invoke(ok, ok ? sessionTicket : ""));
    }

    // ------------------------------------------------ create / join

    public override void CreateRoom(string hostName, int hostSecret, Action<bool, string> done)
    {
        EnsureLogin(ok =>
        {
            if (!ok) { done?.Invoke(false, L10n.Get("pvp_network_error")); return; }

            string args = "{\"hostName\":\"" + EscapeJson(hostName) +
                          "\",\"hostSecret\":" + hostSecret + "}";
            ExecuteCloudScript("createRoom", args, (ok2, resp) =>
            {
                if (!ok2 || !CloudOk(resp))
                {
                    done?.Invoke(false, L10n.Get("pvp_network_error"));
                    return;
                }

                string code = ExtractString(resp, "roomId");
                if (string.IsNullOrEmpty(code))
                {
                    done?.Invoke(false, L10n.Get("pvp_network_error"));
                    return;
                }

                RoomCode = code;
                IsHost = true;
                done?.Invoke(true, code);
            });
        });
    }

    public override void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        EnsureLogin(ok =>
        {
            if (!ok) { done?.Invoke(false, L10n.Get("pvp_network_error")); return; }

            string args = "{\"roomId\":\"" + EscapeJson(code) +
                          "\",\"guestName\":\"" + EscapeJson(guestName) +
                          "\",\"guestSecret\":" + guestSecret + "}";
            ExecuteCloudScript("joinRoom", args, (ok2, resp) =>
            {
                if (!ok2)
                {
                    done?.Invoke(false, L10n.Get("pvp_network_error"));
                    return;
                }
                if (HasCloudError(resp, "room full"))
                {
                    done?.Invoke(false, L10n.Get("pvp_room_full"));
                    return;
                }
                if (HasCloudError(resp, "room not found") || HasCloudError(resp, "bad secret"))
                {
                    done?.Invoke(false, L10n.Get("pvp_room_not_found"));
                    return;
                }
                if (!CloudOk(resp))
                {
                    done?.Invoke(false, L10n.Get("pvp_network_error"));
                    return;
                }

                RoomCode = code;
                IsHost = false;
                done?.Invoke(true, "");
            });
        });
    }

    // ------------------------------------------------ gameplay

    public override void SubmitGuess(int guess, bool useLock, RoomState current, Action<bool> done)
    {
        if (string.IsNullOrEmpty(RoomCode)) { done?.Invoke(false); return; }

        string args = "{\"roomId\":\"" + EscapeJson(RoomCode) +
                      "\",\"guess\":" + guess +
                      ",\"lock\":" + (useLock ? "true" : "false") + "}";
        ExecuteCloudScript("submitGuess", args, (ok, resp) =>
        {
            if (!ok || !CloudOk(resp))
            {
                done?.Invoke(false);
                return;
            }

            ApplyReturnedState(current, resp);
            done?.Invoke(true);
        });
    }

    public override void SendSignal(int signalId, Action<bool> done)
    {
        if (string.IsNullOrEmpty(RoomCode) || !Signals.IsValid(signalId))
        {
            done?.Invoke(false);
            return;
        }

        string args = "{\"roomId\":\"" + EscapeJson(RoomCode) +
                      "\",\"signalId\":" + signalId + "}";
        ExecuteCloudScript("sendSignal", args, (ok, resp) => done?.Invoke(ok && CloudOk(resp)));
    }

    public override void RequestRematch(int secret, Action<bool> done)
    {
        if (string.IsNullOrEmpty(RoomCode) || secret < 1 || secret > 100)
        {
            done?.Invoke(false);
            return;
        }

        string args = "{\"roomId\":\"" + EscapeJson(RoomCode) +
                      "\",\"secret\":" + secret + "}";
        ExecuteCloudScript("requestRematch", args, (ok, resp) => done?.Invoke(ok && CloudOk(resp)));
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
        if (!string.IsNullOrEmpty(RoomCode))
        {
            string code = RoomCode;
            string args = "{\"roomId\":\"" + EscapeJson(code) + "\"}";
            ExecuteCloudScript("leaveRoom", args, (_, __) => { });
        }
        RoomCode = "";
    }

    public override void AcknowledgeResult()
    {
        if (string.IsNullOrEmpty(RoomCode)) return;

        string code = RoomCode;
        string args = "{\"roomId\":\"" + EscapeJson(code) + "\"}";
        ExecuteCloudScript("ackResult", args, (ok, resp) =>
        {
            if (ok && CloudOk(resp) && resp.Contains("\"deleted\":true") && RoomCode == code)
                RoomCode = "";
        });
    }

    IEnumerator Poll(Action<RoomState> onState)
    {
        const int maxConsecutiveFailures = 10;
        int failures = 0;

        while (true)
        {
            string code = RoomCode;
            if (string.IsNullOrEmpty(code)) yield break;

            bool finished = false;
            bool ok = false;
            RoomState state = null;
            ReadState(code, (o, s) =>
            {
                ok = o;
                state = s;
                finished = true;
            });
            while (!finished) yield return null;

            if (ok && state == null)
            {
                pollRoutine = null;
                OnRoomClosed?.Invoke();
                yield break;
            }

            if (!ok)
            {
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

    // ------------------------------------------------ CloudScript state access

    void ReadState(string code, Action<bool, RoomState> done)
    {
        string args = "{\"roomId\":\"" + EscapeJson(code) + "\"}";
        ExecuteCloudScript("getRoom", args, (ok, resp) =>
        {
            if (!ok) { done?.Invoke(false, null); return; }
            if (HasCloudError(resp, "room not found")) { done?.Invoke(true, null); return; }
            if (!CloudOk(resp)) { done?.Invoke(false, null); return; }

            string inner = ExtractStateJson(resp);
            RoomState state = null;
            if (!string.IsNullOrEmpty(inner))
            {
                try { state = JsonUtility.FromJson<RoomState>(inner); }
                catch { }
            }
            done?.Invoke(state != null, state);
        });
    }

    void ApplyReturnedState(RoomState current, string resp)
    {
        if (current == null) return;
        string inner = ExtractStateJson(resp);
        if (string.IsNullOrEmpty(inner)) return;

        try
        {
            var applied = JsonUtility.FromJson<RoomState>(inner);
            if (applied == null) return;

            current.hostName = applied.hostName;
            current.guestName = applied.guestName;
            current.turn = applied.turn;
            current.phase = applied.phase;
            current.lastGuess = applied.lastGuess;
            current.lastBy = applied.lastBy;
            current.winner = applied.winner;
            current.lastHint = applied.lastHint;
            current.revealedSecret = applied.revealedSecret;
            current.hostGuessCount = applied.hostGuessCount;
            current.guestGuessCount = applied.guestGuessCount;

            current.opener = applied.opener;
            current.pendingWin = applied.pendingWin;
            current.lastLocked = applied.lastLocked;
            current.hostLockUsed = applied.hostLockUsed;
            current.guestLockUsed = applied.guestLockUsed;
            current.hostSkipNext = applied.hostSkipNext;
            current.guestSkipNext = applied.guestSkipNext;
            current.roundIndex = applied.roundIndex;
            current.signalBy = applied.signalBy;
            current.signalId = applied.signalId;
            current.signalSeq = applied.signalSeq;
            current.matchIndex = applied.matchIndex;
            current.iWantRematch = applied.iWantRematch;
            current.theyWantRematch = applied.theyWantRematch;
            current.opponentLeft = applied.opponentLeft;
        }
        catch { }
    }

    void ExecuteCloudScript(string functionName, string functionArgsJson, Action<bool, string> done)
    {
        string body = "{\"FunctionName\":\"" + functionName +
                      "\",\"FunctionParameter\":" + functionArgsJson + "}";
        StartCoroutine(Post(Api("ExecuteCloudScript"), body, true, done));
    }

    static bool CloudOk(string resp)
    {
        return !string.IsNullOrEmpty(resp) && resp.Contains("\"ok\":true");
    }

    static bool HasCloudError(string resp, string error)
    {
        return !string.IsNullOrEmpty(resp) && resp.Contains("\"error\":\"" + error + "\"");
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

        if (!ok && expired && authed)
        {
            sessionTicket = "";
            bool loggedIn = false;
            yield return StartCoroutine(Login(r => loggedIn = r));
            if (loggedIn)
                yield return StartCoroutine(PostOnce(url, body, authed, (o, t, _) =>
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
        req.timeout = 10;

        yield return req.SendWebRequest();

        bool ok = req.result == UnityWebRequest.Result.Success;
        string text = req.downloadHandler.text ?? "";
        bool expired = req.responseCode == 401 || text.Contains("InvalidSessionTicket");
        if (!ok) Debug.Log("PlayFab request failed: " + req.error + " " + text);
        done?.Invoke(ok, text, expired);
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

    // CloudScript's FunctionResult carries state as an escaped JSON string.
    static string ExtractStateJson(string resp)
    {
        string marker = "\"state\":\"";
        int i = resp.IndexOf(marker, StringComparison.Ordinal);
        if (i < 0) return "";
        return UnescapeJsonValue(resp, i + marker.Length);
    }

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
                else if (n == 'r') sb.Append('\r');
                else if (n == 't') sb.Append('\t');
                else if (n == 'u' && i + 6 <= resp.Length)
                {
                    int code;
                    if (int.TryParse(resp.Substring(i + 2, 4),
                            System.Globalization.NumberStyles.HexNumber,
                            System.Globalization.CultureInfo.InvariantCulture, out code))
                    {
                        sb.Append((char)code);
                        i += 6;
                        continue;
                    }
                    sb.Append(n);
                }
                else sb.Append(n);
                i += 2;
                continue;
            }
            if (c == '"') break;
            sb.Append(c);
            i++;
        }
        return sb.ToString();
    }

    static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }
}
