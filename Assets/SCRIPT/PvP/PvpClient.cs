using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// Real-multiplayer transport for HOL, using Firebase Realtime Database's
// plain REST API (no SDK needed). One JSON document per room:
//
//   <databaseUrl>/rooms/<CODE>.json
//
// SETUP (one-time, ~5 minutes, free):
//   1. console.firebase.google.com → Add project (name: HOL, Analytics off)
//   2. Build → Realtime Database → Create database → Start in TEST MODE
//   3. Copy the database URL (looks like https://hol-xxxxx-default-rtdb.europe-west1.firebasedatabase.app)
//   4. Paste it into this component's "Database Url" field in the Inspector
//
// SECURITY NOTE (v1, friends-and-family grade): test-mode rules are open and
// secrets travel through the room document, so a technical user could snoop.
// Fine for playing with friends; before a public launch, add Firebase Auth
// and security rules, or move to a server-authoritative backend.
public class PvpClient : PvpBackend
{
    [Tooltip("Your Firebase Realtime Database URL, no trailing slash")]
    public string databaseUrl = "https://YOUR-PROJECT-default-rtdb.europe-west1.firebasedatabase.app";

    public float pollIntervalSeconds = 1.5f;

    Coroutine pollRoutine;

    string RoomUrl(string code) => databaseUrl.TrimEnd('/') + "/rooms/" + code + ".json";

    // ------------------------------------------------ create / join

    public override void CreateRoom(string hostName, int hostSecret, Action<bool, string> done)
    {
        var code = GenerateCode();
        var state = new RoomState
        {
            hostName = hostName,
            hostSecret = hostSecret,
            // Random first mover, mirroring CloudScript's createRoom — a fixed
            // starting side is a systematic advantage in a symmetric race.
            turn = UnityEngine.Random.value < 0.5f ? "host" : "guest",
            phase = "waiting",
        };
        StartCoroutine(PutRoom(code, state, ok =>
        {
            if (ok) { RoomCode = code; IsHost = true; }
            done?.Invoke(ok, code);
        }));
    }

    public override void JoinRoom(string code, string guestName, int guestSecret, Action<bool, string> done)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        StartCoroutine(GetRoom(code, (ok, state) =>
        {
            if (!ok || state == null || string.IsNullOrEmpty(state.hostName))
            {
                done?.Invoke(false, L10n.Get("pvp_room_not_found"));
                return;
            }
            if (state.phase == "closed")
            {
                done?.Invoke(false, L10n.Get("pvp_room_not_found"));
                return;
            }
            if (!string.IsNullOrEmpty(state.guestName))
            {
                done?.Invoke(false, L10n.Get("pvp_room_full"));
                return;
            }
            var patch = "{\"guestName\":\"" + Escape(guestName) + "\",\"guestSecret\":" + guestSecret + ",\"phase\":\"play\"}";
            StartCoroutine(PatchRoom(code, patch, ok2 =>
            {
                if (ok2) { RoomCode = code; IsHost = false; }
                done?.Invoke(ok2, ok2 ? "" : L10n.Get("pvp_network_error"));
            }));
        }));
    }

    // ------------------------------------------------ gameplay

    // Submit my guess; flips the turn. If it hits the opponent's secret, marks the win.
    public override void SubmitGuess(int guess, RoomState current, Action<bool> done)
    {
        string me = IsHost ? "host" : "guest";
        string other = IsHost ? "guest" : "host";
        int opponentSecret = IsHost ? current.guestSecret : current.hostSecret;

        var sb = new StringBuilder("{");
        sb.Append("\"lastGuess\":").Append(guess).Append(',');
        sb.Append("\"lastBy\":\"").Append(me).Append("\",");
        if (guess == opponentSecret)
            sb.Append("\"phase\":\"done\",\"winner\":\"").Append(me).Append("\"");
        else
            sb.Append("\"turn\":\"").Append(other).Append("\"");
        sb.Append('}');

        StartCoroutine(PatchRoom(RoomCode, sb.ToString(), ok =>
        {
            // Mirror the accepted patch into the caller's state so a winning
            // guess ends the match from this response, like the PlayFab
            // backend's returned view does — without waiting on the next poll.
            if (ok && current != null)
            {
                current.lastGuess = guess;
                current.lastBy = me;
                if (guess == opponentSecret)
                {
                    current.phase = "done";
                    current.winner = me;
                }
                else current.turn = other;
            }
            done?.Invoke(ok);
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
        if (!string.IsNullOrEmpty(RoomCode))
            StartCoroutine(Send(UnityWebRequest.Delete(RoomUrl(RoomCode)), null));
        RoomCode = "";
    }

    IEnumerator Poll(Action<RoomState> onState)
    {
        const int maxConsecutiveFailures = 10;
        int failures = 0;
        float waitingSince = -1f;

        while (true)
        {
            bool finished = false;
            bool ok = false;
            RoomState state = null;
            StartCoroutine(GetRoom(RoomCode, (o, s) =>
            {
                ok = o; state = s;
                finished = true;
            }));
            while (!finished) yield return null;

            if (!ok)
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

            if (state == null || string.IsNullOrEmpty(state.hostName))
            {
                // GET succeeded but the room is gone — the host left. A PATCH
                // landing after the leaver's DELETE can also recreate a ghost
                // document holding only guess fields (no hostName); treating
                // it as valid would strand the player against a phantom.
                pollRoutine = null;
                OnRoomClosed?.Invoke();
                yield break;
            }

            failures = 0;
            onState?.Invoke(state);

            float wait = pollIntervalSeconds;
            if (state.phase == "waiting")
            {
                // An empty room does not need match-speed polling for long.
                if (waitingSince < 0f) waitingSince = Time.unscaledTime;
                if (Time.unscaledTime - waitingSince >= 30f)
                    wait = Mathf.Max(wait, 4f);
            }
            else waitingSince = -1f;
            yield return new WaitForSeconds(wait);
        }
    }

    // ------------------------------------------------ HTTP plumbing

    IEnumerator PutRoom(string code, RoomState state, Action<bool> done)
    {
        var req = UnityWebRequest.Put(RoomUrl(code), JsonUtility.ToJson(state));
        req.SetRequestHeader("Content-Type", "application/json");
        yield return Send(req, done);
    }

    IEnumerator PatchRoom(string code, string jsonPatch, Action<bool> done)
    {
        var req = new UnityWebRequest(RoomUrl(code), "PATCH");
        req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonPatch));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        yield return Send(req, done);
    }

    IEnumerator GetRoom(string code, Action<bool, RoomState> done)
    {
        var req = UnityWebRequest.Get(RoomUrl(code));
        req.timeout = 10; // a stalled connection must fail, not freeze the poller
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success;
        RoomState state = null;
        if (ok)
        {
            var body = req.downloadHandler.text;
            if (!string.IsNullOrEmpty(body) && body != "null")
            {
                try { state = JsonUtility.FromJson<RoomState>(body); }
                catch { ok = false; }
            }
        }
        done?.Invoke(ok, state);
        req.Dispose();
    }

    IEnumerator Send(UnityWebRequest req, Action<bool> done)
    {
        req.timeout = 10; // a stalled connection must fail, not freeze the poller
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success;
        if (!ok) Debug.Log("PvP request failed: " + req.error + " " + req.downloadHandler?.text);
        done?.Invoke(ok);
        req.Dispose();
    }

    // ------------------------------------------------ helpers

    static string Escape(string s)
    {
        return (s ?? "").Replace("\\", "").Replace("\"", "").Trim();
    }
}
