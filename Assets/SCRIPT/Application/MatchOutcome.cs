using System.Text;

// A finished match, in the terms the product actually has to reason about.
//
// GameEvents.OnMatchEnded is Action<bool, int>, which cannot express a draw.
// Both the solo and the PvP draw paths therefore route around it and raise only
// StatsChanged — see the comment in PvpGameController. The consequence is that
// the headline outcome of the 0.3.0 duel rules is invisible to every listener,
// analytics included, and a loss throws its guess count away as well.
//
// This record is the shape that can carry both. It is deliberately free of
// UnityEngine so the wire format can be tested without opening the editor, and
// deliberately free of anything identifying: no player name, no secret, no room
// code, no opponent id. What ships is the arithmetic of the duel and nothing
// else.
public struct MatchOutcome
{
    public enum Mode { Solo = 0, Pvp = 1 }
    public enum Result { Loss = 0, Win = 1, Draw = 2 }

    public Mode PlayMode;
    public Result Outcome;

    // This player's guesses, kept on a loss as well as a win. The draw rate is
    // only interpretable against how long the matches ran.
    public int Guesses;
    public int OpponentGuesses;

    // Whether this player moved first. The 0.3.0 rules made the opener a coin
    // flip precisely because it used to decide the duel; this is the field that
    // proves the fix holds outside the simulation.
    public bool Opened;

    // Whether the Lock was staked at all. Whether staking it pays is answered
    // by correlating this with Outcome across many matches, not per match.
    public bool LockStaked;

    // 0 for the first match in a room, incrementing per rematch.
    public int RematchIndex;

    public string AppVersion;

    public static string ModeName(Mode mode)
    {
        switch (mode)
        {
            case Mode.Pvp: return "pvp";
            default: return "solo";
        }
    }

    public static string ResultName(Result result)
    {
        switch (result)
        {
            case Result.Win: return "win";
            case Result.Draw: return "draw";
            default: return "loss";
        }
    }

    // The event body. Names are the wire contract and are written out
    // literally rather than derived from the field names, so renaming a C#
    // field cannot silently split a metric in two halfway through a release.
    public string BodyJson()
    {
        var sb = new StringBuilder(160);
        sb.Append("{\"mode\":\"").Append(ModeName(PlayMode));
        sb.Append("\",\"result\":\"").Append(ResultName(Outcome));
        sb.Append("\",\"guesses\":").Append(Guesses);
        sb.Append(",\"opponentGuesses\":").Append(OpponentGuesses);
        sb.Append(",\"opened\":").Append(Opened ? "true" : "false");
        sb.Append(",\"lockStaked\":").Append(LockStaked ? "true" : "false");
        sb.Append(",\"rematchIndex\":").Append(RematchIndex);
        sb.Append(",\"appVersion\":\"").Append(Escape(AppVersion)).Append("\"}");
        return sb.ToString();
    }

    // PlayFab Client/WritePlayerEvent. The event name must be alphanumeric or
    // underscore and must not lead with a digit.
    public string PlayerEventJson(string eventName)
    {
        return "{\"EventName\":\"" + Escape(eventName) + "\",\"Body\":" + BodyJson() + "}";
    }

    static string Escape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        var sb = new StringBuilder(value.Length + 8);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (c == '"' || c == '\\') sb.Append('\\').Append(c);
            else if (c == '\n') sb.Append("\\n");
            else if (c == '\r') sb.Append("\\r");
            else if (c == '\t') sb.Append("\\t");
            else if (c < ' ') sb.Append(' ');
            else sb.Append(c);
        }
        return sb.ToString();
    }
}
