// Unity adapter for HOL's fixed PvP Signal protocol.
//
// The ordered vocabulary and cap live in Unity-free HOL.Application so protocol
// drift fails at compile time and in Node contracts. This adapter owns only
// localization-facing behavior used by the existing UI.
public static class Signals
{
    // Compatibility alias for existing runtime callers. Do not mutate or
    // replace it; ids are positional and shared with CloudScript.
    public static readonly string[] Table = PvpSignalProtocol.Keys;

    public static int Count { get { return PvpSignalProtocol.Count; } }

    public const int CapPerSide = PvpSignalProtocol.CapPerSide;

    public static bool IsValid(int id)
    {
        return PvpSignalProtocol.IsValid(id);
    }

    public static string Key(int id)
    {
        return PvpSignalProtocol.Key(id);
    }

    public static string Text(int id)
    {
        string key = PvpSignalProtocol.Key(id);
        return string.IsNullOrEmpty(key) ? "" : L10n.Get(key);
    }
}
