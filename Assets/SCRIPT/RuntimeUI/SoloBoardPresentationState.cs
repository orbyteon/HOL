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
    StarterReveal = 6,
    PlayerOutcome = 7,
    OpponentGuess = 8,
    LastLicks = 9,
    LockForfeit = 10,
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
    PlayerStarts = 15,
    OpponentStarts = 16,
    PlayerGuessedHigher = 17,
    PlayerGuessedLower = 18,
    PlayerGuessedCorrect = 19,
    OpponentGuess = 20,
    LastLicks = 21,
    PlayerLockForfeit = 22,
    OpponentLockForfeit = 23,
}

public enum SoloGuessOutcome
{
    Unknown = 0,
    Higher = 1,
    Lower = 2,
    Correct = 3,
}

public enum SoloBoardActor
{
    None = 0,
    Player = 1,
    Opponent = 2,
}

public enum SoloBoardNextAction
{
    None = 0,
    EnterSecret = 1,
    Start = 2,
    SubmitGuess = 3,
    RevealGuess = 4,
    RevealOutcome = 5,
    Continue = 6,
    Rematch = 7,
}

/// <summary>
/// One accepted DuelRules move in exact engine order. Typed facts are stored
/// instead of localized strings so EN/EL can repaint without changing state.
/// </summary>
public sealed class SoloHistoryEvent
{
    public int Sequence { get; }
    public int RoundNumber { get; }
    public SoloBoardActor Actor { get; }
    public SoloBoardActor Target { get; }
    public int Guess { get; }
    public SoloGuessOutcome Outcome { get; }
    public bool LockStaked { get; }
    public bool LockMissed { get; }
    public int CandidatesBefore { get; }

    public SoloHistoryEvent(
        int sequence,
        int roundNumber,
        SoloBoardActor actor,
        SoloBoardActor target,
        int guess,
        SoloGuessOutcome outcome,
        bool lockStaked,
        int candidatesBefore)
    {
        Sequence = sequence;
        RoundNumber = roundNumber;
        Actor = actor;
        Target = target;
        Guess = guess;
        Outcome = outcome;
        LockStaked = lockStaked;
        LockMissed = lockStaked && outcome != SoloGuessOutcome.Correct;
        CandidatesBefore = candidatesBefore;
    }
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
    public int PlayerRangeMin { get; }
    public int PlayerRangeMax { get; }
    public int AiRangeMin { get; }
    public int AiRangeMax { get; }
    public string OpponentName { get; }
    public int DetailValue { get; }
    public int PlayerSecretNumber { get; }
    public int OpponentSecretNumber { get; }
    public int LatestPlayerGuess { get; }
    public int LatestAiGuess { get; }
    public SoloGuessOutcome LatestPlayerOutcome { get; }
    public SoloGuessOutcome LatestAiOutcome { get; }
    public SoloBoardActor Starter { get; }
    public SoloBoardActor ActiveActor { get; }
    public SoloBoardActor TargetActor { get; }
    public SoloBoardNextAction NextAction { get; }
    public SoloBoardActor HandoffActor { get; }
    public bool ResultFollows { get; }
    public bool LatestAiHandoffPinned { get; }
    public bool IsLastLicks { get; }
    public bool LockRevealed { get; }
    public bool LockAvailable { get; }
    public bool LockArmed { get; }
    public bool LockSpent { get; }
    public int LockCandidates { get; }
    public DuelRules.Outcome MatchOutcome { get; }
    public int PlayerTurns { get; }
    public int AiTurns { get; }
    public IReadOnlyList<SoloHistoryEvent> History { get; }
    public IReadOnlyList<int> PlayerGuessHistory { get; }
    public IReadOnlyList<SoloGuessOutcome> PlayerGuessOutcomeHistory { get; }
    public IReadOnlyList<int> AiGuessHistory { get; }
    public IReadOnlyList<DuelRules.Hint> PlayerGuessHints { get; }
    public IReadOnlyList<DuelRules.Hint> AiGuessHints { get; }

    public bool NumericControlsAvailable =>
        Phase == SoloBoardPhase.ChooseSecret || Phase == SoloBoardPhase.PlayerGuess;

    public bool SubmitControlVisible => NumericControlsAvailable;

    public bool IsTerminal => Phase == SoloBoardPhase.MatchResult;

    public bool RequiresHumanDecision =>
        Phase == SoloBoardPhase.ChooseSecret ||
        Phase == SoloBoardPhase.PlayerGuess;

    public bool RequiresAutomaticTransition =>
        !RequiresHumanDecision && !IsTerminal;

    // Routine presentation facts are paced by GameManager's single bounded
    // transition owner. They must never look like permission requests.
    public bool AcknowledgeControlVisible => false;

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
        : this(
            phase, prompt, roundNumber,
            rangeMin, rangeMax, 1, 100,
            opponentName, detailValue,
            0, 0, 0, 0,
            SoloGuessOutcome.Unknown, SoloGuessOutcome.Unknown,
            SoloBoardActor.None, SoloBoardActor.None, SoloBoardActor.None,
            DefaultActionFor(phase), SoloBoardActor.None, false, false, false,
            false, false, false, false, 100,
            DuelRules.Outcome.Undecided, 0, 0,
            Array.Empty<SoloHistoryEvent>(),
            playerGuessHistory, playerGuessOutcomeHistory, aiGuessHistory,
            playerGuessHints, aiGuessHints)
    {
    }

    internal SoloBoardPresentationState(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        int playerRangeMin,
        int playerRangeMax,
        int aiRangeMin,
        int aiRangeMax,
        string opponentName,
        int detailValue,
        int playerSecretNumber,
        int opponentSecretNumber,
        int latestPlayerGuess,
        int latestAiGuess,
        SoloGuessOutcome latestPlayerOutcome,
        SoloGuessOutcome latestAiOutcome,
        SoloBoardActor starter,
        SoloBoardActor activeActor,
        SoloBoardActor targetActor,
        SoloBoardNextAction nextAction,
        SoloBoardActor handoffActor,
        bool resultFollows,
        bool latestAiHandoffPinned,
        bool isLastLicks,
        bool lockRevealed,
        bool lockAvailable,
        bool lockArmed,
        bool lockSpent,
        int lockCandidates,
        DuelRules.Outcome matchOutcome,
        int playerTurns,
        int aiTurns,
        SoloHistoryEvent[] history,
        int[] playerGuessHistory,
        SoloGuessOutcome[] playerGuessOutcomeHistory,
        int[] aiGuessHistory,
        DuelRules.Hint[] playerGuessHints,
        DuelRules.Hint[] aiGuessHints)
    {
        Phase = phase;
        Prompt = prompt;
        RoundNumber = roundNumber;
        RangeMin = playerRangeMin;
        RangeMax = playerRangeMax;
        PlayerRangeMin = playerRangeMin;
        PlayerRangeMax = playerRangeMax;
        AiRangeMin = aiRangeMin;
        AiRangeMax = aiRangeMax;
        OpponentName = opponentName ?? "";
        DetailValue = detailValue;
        PlayerSecretNumber = playerSecretNumber;
        OpponentSecretNumber = opponentSecretNumber;
        LatestPlayerGuess = latestPlayerGuess;
        LatestAiGuess = latestAiGuess;
        LatestPlayerOutcome = latestPlayerOutcome;
        LatestAiOutcome = latestAiOutcome;
        Starter = starter;
        ActiveActor = activeActor;
        TargetActor = targetActor;
        NextAction = nextAction;
        HandoffActor = handoffActor;
        ResultFollows = resultFollows;
        LatestAiHandoffPinned = latestAiHandoffPinned;
        IsLastLicks = isLastLicks;
        LockRevealed = lockRevealed;
        LockAvailable = lockAvailable;
        LockArmed = lockArmed;
        LockSpent = lockSpent;
        LockCandidates = lockCandidates;
        MatchOutcome = matchOutcome;
        PlayerTurns = playerTurns;
        AiTurns = aiTurns;
        History = history ?? Array.Empty<SoloHistoryEvent>();
        PlayerGuessHistory = playerGuessHistory ?? Array.Empty<int>();
        PlayerGuessOutcomeHistory =
            playerGuessOutcomeHistory ?? Array.Empty<SoloGuessOutcome>();
        AiGuessHistory = aiGuessHistory ?? Array.Empty<int>();
        PlayerGuessHints = playerGuessHints ?? Array.Empty<DuelRules.Hint>();
        AiGuessHints = aiGuessHints ?? Array.Empty<DuelRules.Hint>();
    }

    static SoloBoardNextAction DefaultActionFor(SoloBoardPhase phase)
    {
        if (phase == SoloBoardPhase.ChooseSecret)
            return SoloBoardNextAction.EnterSecret;
        if (phase == SoloBoardPhase.PlayerGuess)
            return SoloBoardNextAction.SubmitGuess;
        if (phase == SoloBoardPhase.OpponentThinking)
            return SoloBoardNextAction.RevealGuess;
        if (phase == SoloBoardPhase.OpponentGuess)
            return SoloBoardNextAction.RevealOutcome;
        if (phase == SoloBoardPhase.MatchResult)
            return SoloBoardNextAction.Rematch;
        return SoloBoardNextAction.Continue;
    }
}

/// <summary>
/// Deterministic reading-time policy for automatic Solo presentation beats.
/// Gameplay never depends on these values; GameManager uses them only to hold
/// already-authoritative facts on screen long enough to be read.
/// </summary>
public static class SoloPresentationTiming
{
    public static float MinimumFor(SoloBoardPhase phase)
    {
        if (phase == SoloBoardPhase.OpponentThinking)
            return 1.2f;
        if (phase == SoloBoardPhase.OpponentGuess)
            return 1.5f;
        if (phase == SoloBoardPhase.AnswerOpponent ||
            phase == SoloBoardPhase.LockForfeit)
            return 2.5f;
        return 2.2f;
    }

    public static float MaximumFor(SoloBoardPhase phase)
    {
        if (phase == SoloBoardPhase.OpponentThinking)
            return 1.8f;
        if (phase == SoloBoardPhase.OpponentGuess)
            return 2.2f;
        if (phase == SoloBoardPhase.AnswerOpponent ||
            phase == SoloBoardPhase.LockForfeit)
            return 3.5f;
        return 3.2f;
    }

    public static float DurationFor(
        SoloBoardPhase phase,
        string finalVisibleCopy)
    {
        float minimum = MinimumFor(phase);
        float maximum = MaximumFor(phase);
        string copy = finalVisibleCopy ?? string.Empty;
        int lineBreaks = 0;
        for (int index = 0; index < copy.Length; index++)
            if (copy[index] == '\n') lineBreaks++;

        // Short beats sit at their required minimum. Longer localized or
        // wrapped facts earn deterministic reading time, capped so gameplay
        // never feels stalled.
        float extraCharacters = Math.Max(0, copy.Length - 28) * 0.0125f;
        float extraLines = lineBreaks * 0.18f;
        return Math.Max(
            minimum,
            Math.Min(maximum, minimum + extraCharacters + extraLines));
    }
}

/// <summary>
/// Owns solo-board presentation history and republishes immutable snapshots.
/// A history is cleared only by BeginNewMatch; phase changes never erase it.
/// </summary>
public sealed class SoloBoardPresentationModel
{
    readonly List<SoloHistoryEvent> history = new List<SoloHistoryEvent>();

    string opponentName = "";
    int playerRangeMin = 1;
    int playerRangeMax = 100;
    int aiRangeMin = 1;
    int aiRangeMax = 100;
    int playerSecretNumber;
    int opponentSecretNumber;
    int latestPlayerGuess;
    int latestAiGuess;
    SoloGuessOutcome latestPlayerOutcome;
    SoloGuessOutcome latestAiOutcome;
    SoloBoardActor starter;
    SoloBoardActor handoffActor;
    bool resultFollows;
    bool latestAiHandoffPinned;
    bool lockRevealed;
    bool lockAvailable;
    bool lockArmed;
    bool lockSpent;
    int lockCandidates = 100;
    DuelRules.Outcome matchOutcome;
    int playerTurns;
    int aiTurns;

    public SoloBoardPresentationState Current { get; private set; }

    public SoloBoardPresentationModel()
    {
        Current = Snapshot(
            SoloBoardPhase.ChooseSecret, SoloBoardPrompt.EnterSecret, 0,
            SoloBoardActor.None, SoloBoardActor.None,
            SoloBoardNextAction.EnterSecret, 0, false);
    }

    public void BeginNewMatch(string name)
    {
        history.Clear();
        opponentName = name ?? "";
        playerRangeMin = aiRangeMin = 1;
        playerRangeMax = aiRangeMax = 100;
        playerSecretNumber = 0;
        opponentSecretNumber = 0;
        latestPlayerGuess = latestAiGuess = 0;
        latestPlayerOutcome = latestAiOutcome = SoloGuessOutcome.Unknown;
        starter = SoloBoardActor.None;
        handoffActor = SoloBoardActor.None;
        resultFollows = false;
        latestAiHandoffPinned = false;
        lockRevealed = lockAvailable = lockArmed = lockSpent = false;
        lockCandidates = 100;
        matchOutcome = DuelRules.Outcome.Undecided;
        playerTurns = aiTurns = 0;
        Current = Snapshot(
            SoloBoardPhase.ChooseSecret, SoloBoardPrompt.EnterSecret, 0,
            SoloBoardActor.None, SoloBoardActor.None,
            SoloBoardNextAction.EnterSecret, 0, false);
    }

    public bool SetPlayerSecret(int value)
    {
        if (Current.Phase != SoloBoardPhase.ChooseSecret ||
            value < 1 || value > 100)
            return false;
        playerSecretNumber = value;
        Republish();
        return true;
    }

    public bool RevealStarter(
        SoloBoardActor openingActor,
        int roundNumber,
        int playerMin,
        int playerMax,
        int opponentMin,
        int opponentMax)
    {
        if (openingActor != SoloBoardActor.Player &&
            openingActor != SoloBoardActor.Opponent)
            return false;
        if (!CanTransitionTo(SoloBoardPhase.StarterReveal, roundNumber) ||
            !ValidRanges(playerMin, playerMax, opponentMin, opponentMax))
            return false;

        ApplyRanges(playerMin, playerMax, opponentMin, opponentMax);
        starter = openingActor;
        CommitTransition(
            SoloBoardPhase.StarterReveal,
            openingActor == SoloBoardActor.Player
                ? SoloBoardPrompt.PlayerStarts
                : SoloBoardPrompt.OpponentStarts,
            roundNumber, openingActor,
            openingActor == SoloBoardActor.Player
                ? SoloBoardActor.Opponent
                : SoloBoardActor.Player,
            SoloBoardNextAction.Start, 0, false);
        return true;
    }

    public bool BeginPlayerTurn(
        int roundNumber,
        int playerMin,
        int playerMax,
        int opponentMin,
        int opponentMax,
        bool lastLicks)
    {
        if (!CanTransitionTo(SoloBoardPhase.PlayerGuess, roundNumber) ||
            !ValidRanges(playerMin, playerMax, opponentMin, opponentMax))
            return false;

        bool preserveAiHandoff = latestAiHandoffPinned &&
                                 (Current.Phase == SoloBoardPhase.AnswerOpponent ||
                                  Current.Phase == SoloBoardPhase.LockForfeit ||
                                  Current.Phase == SoloBoardPhase.LastLicks);
        ApplyRanges(playerMin, playerMax, opponentMin, opponentMax);
        latestAiHandoffPinned = preserveAiHandoff;
        resultFollows = false;
        handoffActor = preserveAiHandoff
            ? SoloBoardActor.Player
            : SoloBoardActor.None;
        CommitTransition(
            SoloBoardPhase.PlayerGuess,
            lastLicks ? SoloBoardPrompt.MatchPoint : SoloBoardPrompt.YourGuess,
            roundNumber, SoloBoardActor.Player, SoloBoardActor.Opponent,
            SoloBoardNextAction.SubmitGuess, 0, lastLicks);
        return true;
    }

    public bool BeginOpponentThinking(
        int roundNumber,
        int playerMin,
        int playerMax,
        int opponentMin,
        int opponentMax)
    {
        if (!CanTransitionTo(SoloBoardPhase.OpponentThinking, roundNumber) ||
            !ValidRanges(playerMin, playerMax, opponentMin, opponentMax))
            return false;

        bool answeringPlayerCorrect =
            Current.Phase == SoloBoardPhase.PlayerOutcome &&
            latestPlayerOutcome == SoloGuessOutcome.Correct;
        latestAiHandoffPinned = false;
        resultFollows = false;
        handoffActor = SoloBoardActor.None;
        ApplyRanges(playerMin, playerMax, opponentMin, opponentMax);
        CommitTransition(
            SoloBoardPhase.OpponentThinking,
            answeringPlayerCorrect
                ? SoloBoardPrompt.MatchPointYours
                : SoloBoardPrompt.OpponentThinking,
            roundNumber, SoloBoardActor.Opponent, SoloBoardActor.Player,
            SoloBoardNextAction.RevealGuess, 0, false);
        return true;
    }

    public bool RecordPlayerMove(
        int roundNumber,
        int guess,
        DuelRules.Hint hint,
        bool usedLock,
        int candidatesBefore,
        int newPlayerMin,
        int newPlayerMax,
        int opponentMin,
        int opponentMax)
    {
        if (!CanTransitionTo(SoloBoardPhase.PlayerOutcome, roundNumber) ||
            !ValidRanges(
                newPlayerMin, newPlayerMax, opponentMin, opponentMax) ||
            !ValidGuess(guess) || !ValidHint(hint) ||
            !ValidCandidates(candidatesBefore))
            return false;

        SoloGuessOutcome outcome = OutcomeFor(hint);
        bool wasLastLicks = Current.IsLastLicks;
        latestAiHandoffPinned = false;
        resultFollows = false;
        handoffActor = SoloBoardActor.Opponent;
        ApplyRanges(newPlayerMin, newPlayerMax, opponentMin, opponentMax);
        AppendEvent(roundNumber, SoloBoardActor.Player,
            SoloBoardActor.Opponent, guess, outcome, usedLock,
            candidatesBefore);
        latestPlayerGuess = guess;
        latestPlayerOutcome = outcome;
        playerTurns++;
        CommitTransition(
            SoloBoardPhase.PlayerOutcome, PlayerPromptFor(outcome),
            roundNumber, SoloBoardActor.Player, SoloBoardActor.Opponent,
            SoloBoardNextAction.Continue, guess, wasLastLicks);
        return true;
    }

    public bool RecordOpponentMove(
        int roundNumber,
        int guess,
        DuelRules.Hint hint,
        bool usedLock,
        int candidatesBefore,
        int playerMin,
        int playerMax,
        int newOpponentMin,
        int newOpponentMax)
    {
        if (!CanTransitionTo(SoloBoardPhase.OpponentGuess, roundNumber) ||
            !ValidRanges(
                playerMin, playerMax, newOpponentMin, newOpponentMax) ||
            !ValidGuess(guess) || !ValidHint(hint) ||
            !ValidCandidates(candidatesBefore))
            return false;

        SoloGuessOutcome outcome = OutcomeFor(hint);
        latestAiHandoffPinned = false;
        resultFollows = false;
        handoffActor = SoloBoardActor.Player;
        ApplyRanges(playerMin, playerMax, newOpponentMin, newOpponentMax);
        AppendEvent(roundNumber, SoloBoardActor.Opponent,
            SoloBoardActor.Player, guess, outcome, usedLock,
            candidatesBefore);
        latestAiGuess = guess;
        latestAiOutcome = outcome;
        aiTurns++;
        CommitTransition(
            SoloBoardPhase.OpponentGuess, SoloBoardPrompt.OpponentGuess,
            roundNumber, SoloBoardActor.Opponent, SoloBoardActor.Player,
            SoloBoardNextAction.RevealOutcome, guess, false);
        return true;
    }

    public bool RevealOpponentOutcome()
    {
        if (!CanTransitionTo(
                SoloBoardPhase.AnswerOpponent, Current.RoundNumber) ||
            !ValidGuess(latestAiGuess) ||
            !ValidGuessOutcome(latestAiOutcome))
            return false;
        latestAiHandoffPinned = true;
        CommitTransition(
            SoloBoardPhase.AnswerOpponent,
            OpponentPromptFor(latestAiOutcome), Current.RoundNumber,
            SoloBoardActor.Opponent, SoloBoardActor.Player,
            SoloBoardNextAction.Continue, latestAiGuess, false);
        return true;
    }

    public bool SetOutcomeDestination(
        bool terminalResultFollows,
        SoloBoardActor nextActor)
    {
        if (Current.Phase != SoloBoardPhase.PlayerOutcome &&
            Current.Phase != SoloBoardPhase.OpponentGuess &&
            Current.Phase != SoloBoardPhase.AnswerOpponent)
            return false;
        if (!terminalResultFollows &&
            nextActor != SoloBoardActor.Player &&
            nextActor != SoloBoardActor.Opponent)
            return false;

        resultFollows = terminalResultFollows;
        handoffActor = terminalResultFollows
            ? SoloBoardActor.None
            : nextActor;
        Republish();
        return true;
    }

    public bool DismissLatestAiHandoff()
    {
        if (!latestAiHandoffPinned ||
            Current.Phase != SoloBoardPhase.PlayerGuess)
            return false;
        latestAiHandoffPinned = false;
        handoffActor = SoloBoardActor.None;
        Republish();
        return true;
    }

    public bool ShowLastLicks(int roundNumber)
    {
        if (!CanTransitionTo(SoloBoardPhase.LastLicks, roundNumber))
            return false;
        return Transition(
            SoloBoardPhase.LastLicks, SoloBoardPrompt.LastLicks,
            roundNumber, SoloBoardActor.Player, SoloBoardActor.Opponent,
            SoloBoardNextAction.Continue, 0, true);
    }

    public bool ShowLockForfeit(SoloBoardActor actor, int roundNumber)
    {
        if (actor != SoloBoardActor.Player &&
            actor != SoloBoardActor.Opponent)
            return false;
        if (!CanTransitionTo(SoloBoardPhase.LockForfeit, roundNumber))
            return false;
        return Transition(
            SoloBoardPhase.LockForfeit,
            actor == SoloBoardActor.Player
                ? SoloBoardPrompt.PlayerLockForfeit
                : SoloBoardPrompt.OpponentLockForfeit,
            roundNumber,
            actor == SoloBoardActor.Player
                ? SoloBoardActor.Opponent
                : SoloBoardActor.Player,
            actor,
            SoloBoardNextAction.Continue, 0, false);
    }

    public bool CompleteMatch(
        DuelRules.Outcome outcome,
        int playerSecret,
        int opponentSecret,
        int playerGuessCount,
        int opponentGuessCount)
    {
        int resultRound = Current.RoundNumber;
        if (!CanTransitionTo(SoloBoardPhase.MatchResult, resultRound) ||
            !ValidMatchOutcome(outcome) ||
            !ValidGuess(playerSecret) || !ValidGuess(opponentSecret) ||
            playerGuessCount < 0 || opponentGuessCount < 0)
            return false;

        SoloBoardPrompt prompt = outcome == DuelRules.Outcome.Draw
            ? SoloBoardPrompt.Draw
            : outcome == DuelRules.Outcome.HostWins
                ? SoloBoardPrompt.Win
                : SoloBoardPrompt.Loss;
        matchOutcome = outcome;
        resultFollows = false;
        handoffActor = SoloBoardActor.None;
        latestAiHandoffPinned = false;
        playerSecretNumber = playerSecret;
        opponentSecretNumber = opponentSecret;
        playerTurns = playerGuessCount;
        aiTurns = opponentGuessCount;
        CommitTransition(
            SoloBoardPhase.MatchResult, prompt,
            resultRound,
            SoloBoardActor.None, SoloBoardActor.None,
            SoloBoardNextAction.Rematch, 0, false);
        return true;
    }

    public void UpdateLockState(
        bool revealed,
        bool available,
        bool armed,
        bool spent,
        int candidates)
    {
        lockRevealed = revealed;
        lockAvailable = available;
        lockArmed = armed;
        lockSpent = spent;
        lockCandidates = Math.Max(0, candidates);
        Republish();
    }

    // Compatibility seam for the existing deterministic capture player.
    // Production uses only the semantic transition methods above.
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
        if (!SetRanges(rangeMin, rangeMax, aiRangeMin, aiRangeMax))
            throw new ArgumentOutOfRangeException(nameof(rangeMin));

        SoloBoardActor actor = SoloBoardActor.None;
        SoloBoardActor target = SoloBoardActor.None;
        if (phase == SoloBoardPhase.PlayerGuess ||
            phase == SoloBoardPhase.PlayerOutcome ||
            phase == SoloBoardPhase.LastLicks)
        {
            actor = SoloBoardActor.Player;
            target = SoloBoardActor.Opponent;
        }
        else if (phase == SoloBoardPhase.OpponentThinking ||
                 phase == SoloBoardPhase.OpponentGuess ||
                 phase == SoloBoardPhase.AnswerOpponent)
        {
            actor = SoloBoardActor.Opponent;
            target = SoloBoardActor.Player;
        }

        Current = Snapshot(
            phase, prompt, roundNumber, actor, target,
            DefaultActionFor(phase), detailValue,
            phase == SoloBoardPhase.LastLicks);
    }

    public void RecordPlayerGuess(int guess, DuelRules.Hint hint)
    {
        AppendEvent(Math.Max(1, Current.RoundNumber), SoloBoardActor.Player,
            SoloBoardActor.Opponent, guess, OutcomeFor(hint), false,
            playerRangeMax - playerRangeMin + 1);
        latestPlayerGuess = guess;
        latestPlayerOutcome = OutcomeFor(hint);
        Republish();
    }

    // Deterministic visual-fixture seam. Production gameplay uses the typed
    // DuelRules.Hint overload above so one accepted guess is recorded once.
    public void RecordPlayerGuessResult(int guess, SoloGuessOutcome outcome)
    {
        AppendEvent(Math.Max(1, Current.RoundNumber), SoloBoardActor.Player,
            SoloBoardActor.Opponent, guess, outcome, false,
            playerRangeMax - playerRangeMin + 1);
        latestPlayerGuess = guess;
        latestPlayerOutcome = outcome;
        Republish();
    }

    public void RecordAiGuess(int guess, DuelRules.Hint hint)
    {
        AppendEvent(Math.Max(1, Current.RoundNumber), SoloBoardActor.Opponent,
            SoloBoardActor.Player, guess, OutcomeFor(hint), false,
            aiRangeMax - aiRangeMin + 1);
        latestAiGuess = guess;
        latestAiOutcome = OutcomeFor(hint);
        Republish();
    }

    bool Transition(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        SoloBoardActor actor,
        SoloBoardActor target,
        SoloBoardNextAction action,
        int detailValue,
        bool lastLicks)
    {
        if (!CanTransitionTo(phase, roundNumber))
            return false;
        CommitTransition(
            phase, prompt, roundNumber, actor, target, action,
            detailValue, lastLicks);
        return true;
    }

    bool CanTransitionTo(SoloBoardPhase phase, int roundNumber)
    {
        if (phase != SoloBoardPhase.ChooseSecret && roundNumber < 1)
            return false;
        return CanTransition(Current.Phase, phase);
    }

    void CommitTransition(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        SoloBoardActor actor,
        SoloBoardActor target,
        SoloBoardNextAction action,
        int detailValue,
        bool lastLicks)
    {
        Current = Snapshot(
            phase, prompt, roundNumber, actor, target, action,
            detailValue, lastLicks);
    }

    static bool CanTransition(SoloBoardPhase from, SoloBoardPhase to)
    {
        if (from == SoloBoardPhase.ChooseSecret)
            return to == SoloBoardPhase.StarterReveal;
        if (from == SoloBoardPhase.StarterReveal)
            return to == SoloBoardPhase.PlayerGuess ||
                   to == SoloBoardPhase.OpponentThinking;
        if (from == SoloBoardPhase.PlayerGuess)
            return to == SoloBoardPhase.PlayerOutcome;
        if (from == SoloBoardPhase.PlayerOutcome ||
            from == SoloBoardPhase.AnswerOpponent ||
            from == SoloBoardPhase.LockForfeit)
        {
            return to == SoloBoardPhase.PlayerGuess ||
                   to == SoloBoardPhase.OpponentThinking ||
                   to == SoloBoardPhase.LastLicks ||
                   to == SoloBoardPhase.LockForfeit ||
                   to == SoloBoardPhase.MatchResult;
        }
        if (from == SoloBoardPhase.OpponentThinking)
            return to == SoloBoardPhase.OpponentGuess;
        if (from == SoloBoardPhase.OpponentGuess)
            return to == SoloBoardPhase.AnswerOpponent;
        if (from == SoloBoardPhase.LastLicks)
            return to == SoloBoardPhase.PlayerGuess;
        return false;
    }

    bool SetRanges(
        int newPlayerMin,
        int newPlayerMax,
        int newAiMin,
        int newAiMax)
    {
        if (!ValidRanges(
                newPlayerMin, newPlayerMax, newAiMin, newAiMax))
            return false;
        ApplyRanges(newPlayerMin, newPlayerMax, newAiMin, newAiMax);
        return true;
    }

    static bool ValidRanges(
        int newPlayerMin,
        int newPlayerMax,
        int newAiMin,
        int newAiMax)
    {
        return ValidRange(newPlayerMin, newPlayerMax) &&
               ValidRange(newAiMin, newAiMax);
    }

    void ApplyRanges(
        int newPlayerMin,
        int newPlayerMax,
        int newAiMin,
        int newAiMax)
    {
        playerRangeMin = newPlayerMin;
        playerRangeMax = newPlayerMax;
        aiRangeMin = newAiMin;
        aiRangeMax = newAiMax;
    }

    static bool ValidRange(int minimum, int maximum)
    {
        return minimum >= 1 && maximum <= 100 && minimum <= maximum;
    }

    static bool ValidGuess(int guess)
    {
        return guess >= 1 && guess <= 100;
    }

    static bool ValidHint(DuelRules.Hint hint)
    {
        return hint == DuelRules.Hint.Higher ||
               hint == DuelRules.Hint.Lower ||
               hint == DuelRules.Hint.Correct;
    }

    static bool ValidGuessOutcome(SoloGuessOutcome outcome)
    {
        return outcome == SoloGuessOutcome.Higher ||
               outcome == SoloGuessOutcome.Lower ||
               outcome == SoloGuessOutcome.Correct;
    }

    static bool ValidCandidates(int candidates)
    {
        return candidates >= 1 && candidates <= 100;
    }

    static bool ValidMatchOutcome(DuelRules.Outcome outcome)
    {
        return outcome == DuelRules.Outcome.HostWins ||
               outcome == DuelRules.Outcome.GuestWins ||
               outcome == DuelRules.Outcome.Draw;
    }

    void AppendEvent(
        int roundNumber,
        SoloBoardActor actor,
        SoloBoardActor target,
        int guess,
        SoloGuessOutcome outcome,
        bool usedLock,
        int candidatesBefore)
    {
        history.Add(new SoloHistoryEvent(
            history.Count + 1, roundNumber, actor, target, guess, outcome,
            usedLock, candidatesBefore));
    }

    void Republish()
    {
        Current = Snapshot(
            Current.Phase, Current.Prompt, Current.RoundNumber,
            Current.ActiveActor, Current.TargetActor, Current.NextAction,
            Current.DetailValue, Current.IsLastLicks);
    }

    SoloBoardPresentationState Snapshot(
        SoloBoardPhase phase,
        SoloBoardPrompt prompt,
        int roundNumber,
        SoloBoardActor actor,
        SoloBoardActor target,
        SoloBoardNextAction action,
        int detailValue,
        bool lastLicks)
    {
        var playerGuesses = new List<int>();
        var playerOutcomes = new List<SoloGuessOutcome>();
        var aiGuesses = new List<int>();
        var playerHints = new List<DuelRules.Hint>();
        var aiHints = new List<DuelRules.Hint>();
        for (int index = 0; index < history.Count; index++)
        {
            SoloHistoryEvent item = history[index];
            if (item.Actor == SoloBoardActor.Player)
            {
                playerGuesses.Add(item.Guess);
                playerOutcomes.Add(item.Outcome);
                playerHints.Add(HintFor(item.Outcome));
            }
            else if (item.Actor == SoloBoardActor.Opponent)
            {
                aiGuesses.Add(item.Guess);
                aiHints.Add(HintFor(item.Outcome));
            }
        }

        return new SoloBoardPresentationState(
            phase, prompt, roundNumber,
            playerRangeMin, playerRangeMax, aiRangeMin, aiRangeMax,
            opponentName, detailValue,
            playerSecretNumber, opponentSecretNumber,
            latestPlayerGuess, latestAiGuess,
            latestPlayerOutcome, latestAiOutcome,
            starter, actor, target, action,
            handoffActor, resultFollows, latestAiHandoffPinned, lastLicks,
            lockRevealed, lockAvailable, lockArmed, lockSpent,
            lockCandidates, matchOutcome, playerTurns, aiTurns,
            history.ToArray(), playerGuesses.ToArray(),
            playerOutcomes.ToArray(), aiGuesses.ToArray(),
            playerHints.ToArray(), aiHints.ToArray());
    }

    static SoloBoardPrompt PlayerPromptFor(SoloGuessOutcome outcome)
    {
        if (outcome == SoloGuessOutcome.Higher)
            return SoloBoardPrompt.PlayerGuessedHigher;
        if (outcome == SoloGuessOutcome.Lower)
            return SoloBoardPrompt.PlayerGuessedLower;
        return SoloBoardPrompt.PlayerGuessedCorrect;
    }

    static SoloBoardPrompt OpponentPromptFor(SoloGuessOutcome outcome)
    {
        if (outcome == SoloGuessOutcome.Higher)
            return SoloBoardPrompt.OpponentGuessedHigher;
        if (outcome == SoloGuessOutcome.Lower)
            return SoloBoardPrompt.OpponentGuessedLower;
        return SoloBoardPrompt.OpponentGuessedCorrect;
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

    static SoloBoardNextAction DefaultActionFor(SoloBoardPhase phase)
    {
        if (phase == SoloBoardPhase.ChooseSecret)
            return SoloBoardNextAction.EnterSecret;
        if (phase == SoloBoardPhase.PlayerGuess)
            return SoloBoardNextAction.SubmitGuess;
        if (phase == SoloBoardPhase.OpponentThinking)
            return SoloBoardNextAction.RevealGuess;
        if (phase == SoloBoardPhase.OpponentGuess)
            return SoloBoardNextAction.RevealOutcome;
        if (phase == SoloBoardPhase.MatchResult)
            return SoloBoardNextAction.Rematch;
        return SoloBoardNextAction.Continue;
    }
}
