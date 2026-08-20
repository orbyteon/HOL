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
    public IReadOnlyList<int> AiGuessHistory { get; }

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
        int[] aiGuessHistory)
    {
        Phase = phase;
        Prompt = prompt;
        RoundNumber = roundNumber;
        RangeMin = rangeMin;
        RangeMax = rangeMax;
        OpponentName = opponentName ?? "";
        DetailValue = detailValue;
        PlayerGuessHistory = playerGuessHistory ?? Array.Empty<int>();
        AiGuessHistory = aiGuessHistory ?? Array.Empty<int>();
    }
}

/// <summary>
/// Owns solo-board presentation history and republishes immutable snapshots.
/// A history is cleared only by BeginNewMatch; phase changes never erase it.
/// </summary>
public sealed class SoloBoardPresentationModel
{
    readonly List<int> playerGuessHistory = new List<int>();
    readonly List<int> aiGuessHistory = new List<int>();

    public SoloBoardPresentationState Current { get; private set; }

    public SoloBoardPresentationModel()
    {
        Current = Snapshot(SoloBoardPhase.ChooseSecret, SoloBoardPrompt.EnterSecret,
            0, 1, 100, "", 0);
    }

    public void BeginNewMatch(string opponentName)
    {
        playerGuessHistory.Clear();
        aiGuessHistory.Clear();
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

    public void RecordPlayerGuess(int guess)
    {
        playerGuessHistory.Add(guess);
        Republish();
    }

    public void RecordAiGuess(int guess)
    {
        aiGuessHistory.Add(guess);
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
            opponentName, detailValue, playerGuessHistory.ToArray(), aiGuessHistory.ToArray());
    }
}
