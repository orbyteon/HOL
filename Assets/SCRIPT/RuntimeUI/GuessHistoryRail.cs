using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// A server-accepted guess is identified by match, side and that side's
// authoritative guess count. Localized text is presentation data only: it can
// change languages without changing identity or duplicating the event.
[Serializable]
public struct PvpGuessHistoryEvent
{
    public int matchIndex;
    public string side;
    public int ordinal;
    public int authoritativeTotal;
    public int value;
    public string hint;
    public bool locked;
    public bool isMine;
    public string opponentName;
}

public class GuessHistoryRail : MonoBehaviour
{
    public const int VisibleCapacity = 4;

    public TMP_Text source; // latest accepted event
    public TMP_Text target; // three preceding events, newest first

    readonly List<PvpGuessHistoryEvent> events =
        new List<PvpGuessHistoryEvent>(VisibleCapacity);
    readonly Dictionary<string, int> highestOrdinalBySide =
        new Dictionary<string, int>();
    int highestAuthoritativeTotal;

    public int MatchIndex { get; private set; } = -1;
    public int EventCount { get { return events.Count; } }

    void OnEnable()
    {
        L10n.OnLanguageChanged -= Repaint;
        L10n.OnLanguageChanged += Repaint;
        Repaint();
    }

    void OnDisable()
    {
        L10n.OnLanguageChanged -= Repaint;
    }

    public void ResetForMatch(int matchIndex)
    {
        MatchIndex = matchIndex;
        events.Clear();
        highestOrdinalBySide.Clear();
        highestAuthoritativeTotal = 0;
        Repaint();
    }

    public bool Record(int matchIndex, string side, int ordinal,
        int authoritativeTotal, int value, string hint, bool locked,
        bool isMine, string opponentName)
    {
        if (matchIndex < MatchIndex || ordinal <= 0 || string.IsNullOrEmpty(side))
            return false;

        if (matchIndex > MatchIndex)
            ResetForMatch(matchIndex);

        // The side ordinal is identity. The total is only a monotonic snapshot
        // fence, preventing a late poll from repainting an older room view.
        if (authoritativeTotal <= highestAuthoritativeTotal &&
            highestAuthoritativeTotal > 0)
            return false;

        int highest;
        if (highestOrdinalBySide.TryGetValue(side, out highest) &&
            ordinal <= highest)
            return false;

        highestOrdinalBySide[side] = ordinal;
        highestAuthoritativeTotal = authoritativeTotal;
        events.Insert(0, new PvpGuessHistoryEvent
        {
            matchIndex = matchIndex,
            side = side,
            ordinal = ordinal,
            authoritativeTotal = authoritativeTotal,
            value = value,
            hint = hint ?? "",
            locked = locked,
            isMine = isMine,
            opponentName = opponentName ?? "",
        });
        while (events.Count > VisibleCapacity)
            events.RemoveAt(events.Count - 1);

        Repaint();
        return true;
    }

    // Test and diagnostics seam: returns the stable identity of a retained
    // event without exposing localized text as data.
    public string IdentityAt(int newestFirstIndex)
    {
        if (newestFirstIndex < 0 || newestFirstIndex >= events.Count) return "";
        var item = events[newestFirstIndex];
        return item.matchIndex + ":" + item.side + ":" + item.ordinal;
    }

    public int ValueAt(int newestFirstIndex)
    {
        return newestFirstIndex >= 0 && newestFirstIndex < events.Count
            ? events[newestFirstIndex].value
            : 0;
    }

    public void Repaint()
    {
        if (source != null)
            source.text = events.Count > 0 ? Render(events[0]) : "";

        if (target == null) return;
        var sb = new StringBuilder();
        for (int i = 1; i < events.Count; i++)
        {
            if (sb.Length > 0) sb.AppendLine();
            sb.Append(Render(events[i]));
        }
        target.text = sb.ToString();
    }

    static string Render(PvpGuessHistoryEvent item)
    {
        string who = item.isMine ? L10n.Get("you") : item.opponentName;
        if (item.locked) who += " [" + L10n.Get("lock_armed") + "]";

        string line = who + ": " + item.value + "  →  " +
                      LocalizedHint(item.hint);
        if (item.locked && item.hint != "correct")
            line += "\n" + (item.isMine
                ? L10n.Get("lock_missed")
                : L10n.Get("opponent_forfeits", item.opponentName));
        return line;
    }

    static string LocalizedHint(string hint)
    {
        if (hint == "correct") return L10n.Get("correct") + "!";
        if (hint == "higher") return L10n.Get("higher");
        if (hint == "lower") return L10n.Get("lower");
        return "";
    }
}
