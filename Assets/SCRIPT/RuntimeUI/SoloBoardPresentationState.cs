using System;
using System.Collections.Generic;

public enum SoloBoardPhase
{
    ChooseSecret = 0,
    PlayerGuess = 1,
    OpponentThinking = 2,
    AnswerOpponent = 3,
    RoundResolution = 4,
    MatchResult = 5,
}

public enum SoloBoardPrompt
{
    EnterSecret = 0,
    YourGuess = 1,
    OpponentThinking = 2,
    AnswerOpponent = 3,
    OpponentForfeits = 4,
    MatchPoint = 5,
    MatchPointYours = 6,
    TurnForfeited = 7,
    ResolvingRound = 8,
    Win = 9,
    Loss = 10,
    Draw = 11,
    OpponentGuessedHigher = 12,
    OpponentGuessedLower = 13,
    OpponentGuessedCorrect = 14,
}

public enum SoloGuessOutcome
{
    Unknown = 0,
    Higher = 1,
    Lower = 2,
    Correct = 3,
}

/// <summary>
/// Immutable snapshot of everything the solo board is allowed to present.
/// DuelRules remains authoritative for gameplay; this state is authoritative
/// only for what the player can see and operate.
/// </summary>
public sealed class SoloBoardPresentationState
{
    public SoloBoardPhase Phase { get; }
    public SoloBoardPrompt Prompt { get; }
    public int RoundNumber { get; }
    public int RangeMin { get; }
    public int RangeMax { get; }
    public string OpponentName { get; }
    public int DetailValue { get; }
    public IReadOnlyList<int> PlayerGuessHistory { get; }
    public IReadOnlyList<SoloGuessOutcome> PlayerGuessOutcomeHistory { get; }
    public IReadOnlyList<int> AiGuessHistory { get; }
    public IReadOnlyList<DuelRules.Hint> PlayerGuessHints { get; }
    public IReadOnlyList<DuelRules.Hint> AiGuessHints { get; }

    public bool NumericControlsAvailable =>
        Phase == SoloBoardPhase.ChooseSecret || Phase == SoloBoardPhase.PlayerGuess;

    public bool SubmitControlVisible => NumericControlsAvailable;

    public SoloBoardPresentationState(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        int rangeMin,
        int rangeMax,
        string opponentName,
        int detailValue,
        int[] playerGuessHistory,
        SoloGuessOutcome[] playerGuessOutcomeHistory,
        int[] aiGuessHistory,
        DuelRules.Hint[] playerGuessHints,
        DuelRules.Hint[] aiGuessHints)
    {
        Phase = phase;
        Prompt = prompt;
        RoundNumber = roundNumber;
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        OpponentName = opponentName ?? "";
        DetailValue = detailValue;
        PlayerGuessHistory = playerGuessHistory ?? Array.Empty<int>();
        PlayerGuessOutcomeHistory =
            playerGuessOutcomeHistory ?? Array.Empty<SoloGuessOutcome>();
        AiGuessHistory = aiGuessHistory ?? Array.Empty<int>();
        PlayerGuessHints = playerGuessHints ?? Array.Empty<DuelRules.Hint>();
        AiGuessHints = aiGuessHints ?? Array.Empty<DuelRules.Hint>();
    }
}

/// <summary>
/// Owns solo-board presentation history and republishes immutable snapshots.
/// A history is cleared only by BeginNewMatch; phase changes never erase it.
/// </summary>
public sealed class SoloBoardPresentationModel
{
    readonly List<int> playerGuessHistory = new List<int>();
    readonly List<SoloGuessOutcome> playerGuessOutcomeHistory =
        new List<SoloGuessOutcome>();
    readonly List<int> aiGuessHistory = new List<int>();
    readonly List<DuelRules.Hint> playerGuessHints =
        new List<DuelRules.Hint>();
    readonly List<DuelRules.Hint> aiGuessHints =
        new List<DuelRules.Hint>();

    public SoloBoardPresentationState Current { get; private set; }

    public SoloBoardPresentationModel()
    {
        Current = Snapshot(SoloBoardPhase.ChooseSecret, SoloBoardPrompt.EnterSecret,
            0, 1, 100, "", 0);
    }

    public void BeginNewMatch(string opponentName)
    {
        playerGuessHistory.Clear();
        playerGuessOutcomeHistory.Clear();
        aiGuessHistory.Clear();
        playerGuessHints.Clear();
        aiGuessHints.Clear();
        Current = Snapshot(SoloBoardPhase.ChooseSecret, SoloBoardPrompt.EnterSecret,
            0, 1, 100, opponentName, 0);
    }

    public void Present(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        int rangeMin,
        int rangeMax,
        int detailValue = 0)
    {
        if (phase != SoloBoardPhase.ChooseSecret && roundNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(roundNumber));
        if (rangeMin < 1 || rangeMax > 100 || rangeMin > rangeMax)
            throw new ArgumentOutOfRangeException(nameof(rangeMin));

        Current = Snapshot(phase, prompt, roundNumber, rangeMin, rangeMax,
            Current.OpponentName, detailValue);
    }

    public void RecordPlayerGuess(int guess, DuelRules.Hint hint)
    {
        playerGuessHistory.Add(guess);
        playerGuessHints.Add(hint);
        playerGuessOutcomeHistory.Add(OutcomeFor(hint));
        Republish();
    }

    // Deterministic visual-fixture seam. Production gameplay uses the typed
    // DuelRules.Hint overload above so one accepted guess is recorded once.
    public void RecordPlayerGuessResult(int guess, SoloGuessOutcome outcome)
    {
        playerGuessHistory.Add(guess);
        playerGuessHints.Add(HintFor(outcome));
        playerGuessOutcomeHistory.Add(outcome);
        Republish();
    }

    public void RecordAiGuess(int guess, DuelRules.Hint hint)
    {
        aiGuessHistory.Add(guess);
        aiGuessHints.Add(hint);
        Republish();
    }

    void Republish()
    {
        Current = Snapshot(Current.Phase, Current.Prompt, Current.RoundNumber,
            Current.RangeMin, Current.RangeMax, Current.OpponentName, Current.DetailValue);
    }

    SoloBoardPresentationState Snapshot(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        int rangeMin,
        int rangeMax,
        string opponentName,
        int detailValue)
    {
        return new SoloBoardPresentationState(phase, prompt, roundNumber, rangeMin, rangeMax,
            opponentName, detailValue, playerGuessHistory.ToArray(),
            playerGuessOutcomeHistory.ToArray(), aiGuessHistory.ToArray(),
            playerGuessHints.ToArray(), aiGuessHints.ToArray());
    }

    static SoloGuessOutcome OutcomeFor(DuelRules.Hint hint)
    {
        switch (hint)
        {
            case DuelRules.Hint.Higher:
                return SoloGuessOutcome.Higher;
            case DuelRules.Hint.Lower:
                return SoloGuessOutcome.Lower;
            case DuelRules.Hint.Correct:
                return SoloGuessOutcome.Correct;
            default:
                return SoloGuessOutcome.Unknown;
        }
    }

    static DuelRules.Hint HintFor(SoloGuessOutcome outcome)
    {
        switch (outcome)
        {
            case SoloGuessOutcome.Higher:
                return DuelRules.Hint.Higher;
            case SoloGuessOutcome.Lower:
                return DuelRules.Hint.Lower;
            case SoloGuessOutcome.Correct:
                return DuelRules.Hint.Correct;
            default:
                return DuelRules.Hint.None;
        }
    }
}
