using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Accumulates the duel's guess history for the HISTORY card.
//
// The controller writes one localized line per accepted guess into its history
// label, and its repaints are signature-gated, so every change to that label is
// a real event. Watching the label therefore needs no controller surface at
// all — which matters while PR #11 rewrites PvpGameController: this component
// survives that merge untouched.
//
// Known blind spot, accepted: two consecutive guesses that render the exact
// same line (same player, same value, same hint — possible only after a
// forfeited turn returns the turn order to the same player) are seen as one
// change. Cosmetic, and the latest-guess line above the rail is always right.
public class GuessHistoryRail : MonoBehaviour
{
    public TMP_Text source; // the controller's latest-guess line
    public Text target;     // renders the guesses before it, newest first
    public int keep = 3;

    readonly List<string> previous = new List<string>();
    string lastSeen = "";

    void LateUpdate()
    {
        if (source == null || target == null) return;

        string current = source.text ?? "";
        if (current == lastSeen) return;

        if (string.IsNullOrEmpty(current))
        {
            // A fresh match (or rematch reset): the story starts over.
            previous.Clear();
        }
        else if (!string.IsNullOrEmpty(lastSeen))
        {
            previous.Add(lastSeen);
            while (previous.Count > keep) previous.RemoveAt(0);
        }
        lastSeen = current;

        var sb = new System.Text.StringBuilder();
        for (int i = previous.Count - 1; i >= 0; i--)
            sb.AppendLine(previous[i]);
        target.text = sb.ToString();
    }
}
