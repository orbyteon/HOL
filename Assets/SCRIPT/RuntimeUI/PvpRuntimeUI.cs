using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Runtime wiring only. PvpDuelCartoonVisuals owns match/result;
// PrivateRoomVisuals owns the complete landing/Create/Join/Waiting flow.
[RequireComponent(typeof(Canvas), typeof(GraphicRaycaster))]
public class PvpRuntimeUI : MonoBehaviour
{
    [Tooltip("Public PlayFab Title ID copied to the production transport")]
    public string playFabTitleId = "";

    void Start()
    {
        var backend = gameObject.AddComponent<PlayFabPvpClient>();
        backend.titleId = playFabTitleId;
        var controller = gameObject.AddComponent<PvpGameController>();
        controller.client = backend;
        BuildPanels(controller);
        InjectEntryButton(controller);
    }

    void BuildPanels(PvpGameController controller)
    {
        BuildMatchPanel(controller);
        ReplacePrivateRoomPanels(controller);
    }

    void BuildMatchPanel(PvpGameController controller)
    {
        var visuals = GetComponent<PvpDuelCartoonVisuals>();
        if (visuals == null) visuals = gameObject.AddComponent<PvpDuelCartoonVisuals>();
        visuals.Build(controller);
    }

    void ReplacePrivateRoomPanels(PvpGameController controller)
    {
        // Neutral callback-bearing controls only; the landing owner supplies
        // the existing Private Room composition before this hidden panel opens.
        var menu = RuntimeUI.FullscreenPanel(transform, "PvPMenuPanel", Color.white);
        var create = RuntimeUI.CreateButton(menu.transform, "CreateButton",
            L10n.Get("pvp_create_room"), Vector2.zero, new Vector2(360f, 104f), Color.white);
        var join = RuntimeUI.CreateButton(menu.transform, "JoinButton",
            L10n.Get("pvp_join_room"), Vector2.zero, new Vector2(430f, 104f), Color.white);
        var back = RuntimeUI.CreateButton(menu.transform, "BackButton",
            L10n.Get("back"), Vector2.zero, new Vector2(90f, 90f), Color.white);
        var privateVisuals = GetComponent<PrivateRoomVisuals>();
        if (privateVisuals == null) privateVisuals = gameObject.AddComponent<PrivateRoomVisuals>();
        var prebattleCreate = privateVisuals.BuildPrebattlePanel("PvPCreatePanel", true);
        var prebattleJoin = privateVisuals.BuildPrebattlePanel("PvPJoinPanel", false);
        controller.pvpMenuPanel = menu;
        controller.createPanel = prebattleCreate.panel;
        controller.joinPanel = prebattleJoin.panel;
        controller.createSecretInput = prebattleCreate.secret;
        controller.createConfirmButton = prebattleCreate.confirm;
        controller.createEntryRoot = prebattleCreate.entryRoot;
        controller.createWaitingRoot = prebattleCreate.waitingRoot;
        controller.createEntryStatusText = prebattleCreate.entryStatus;
        controller.createOpponentStatusText =
            prebattleCreate.opponentStatus;
        controller.roomCodeText = prebattleCreate.codeText;
        controller.createStatusText = prebattleCreate.status;
        controller.createCopyButton = prebattleCreate.copy.gameObject;
        controller.joinCodeInput = prebattleJoin.codeInput;
        controller.joinSecretInput = prebattleJoin.secret;
        controller.joinConfirmButton = prebattleJoin.confirm;
        controller.joinEntryRoot = prebattleJoin.entryRoot;
        controller.joinWaitingRoot = prebattleJoin.waitingRoot;
        controller.joinEntryStatusText = prebattleJoin.entryStatus;
        controller.joinOpponentStatusText = prebattleJoin.opponentStatus;
        controller.joinStatusText = prebattleJoin.status;
        var prebattleEllipsis = prebattleCreate.status.gameObject
            .AddComponent<AnimatedEllipsis>();
        prebattleEllipsis.text = prebattleCreate.status;
        prebattleEllipsis.enabled = false;
        controller.createStatusEllipsis = prebattleEllipsis;

        privateVisuals.Build(controller);

        create.onClick.AddListener(() => ShowOnly(controller, prebattleCreate.panel));
        join.onClick.AddListener(() => ShowOnly(controller, prebattleJoin.panel));
        back.onClick.AddListener(controller.ClosePvpMenu);
        prebattleCreate.confirm.GetComponent<Button>().onClick.AddListener(
            controller.OnCreateRoomPressed);
        prebattleCreate.copy.onClick.AddListener(controller.OnCopyInvitePressed);
        prebattleCreate.back.onClick.AddListener(controller.CancelRoomAndLeave);
        prebattleJoin.confirm.GetComponent<Button>().onClick.AddListener(
            controller.OnJoinRoomPressed);
        prebattleJoin.back.onClick.AddListener(controller.CancelRoomAndLeave);
        prebattleCreate.secret.onSubmit.AddListener(
            _ => controller.OnCreateRoomPressed());
        prebattleJoin.codeInput.onSubmit.AddListener(
            _ => controller.OnJoinRoomPressed());
        prebattleJoin.secret.onSubmit.AddListener(
            _ => controller.OnJoinRoomPressed());

        menu.SetActive(false);
        prebattleCreate.panel.SetActive(false);
        prebattleJoin.panel.SetActive(false);
    }
    void InjectEntryButton(PvpGameController controller)
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogWarning("PvpRuntimeUI: no Canvas found for the PvP entry.");
            return;
        }
        var entry = RuntimeUI.CreateButton(canvas.transform, "ButtonPvP",
            L10n.Get("pvp_duel"), new Vector2(0f, -620f),
            new Vector2(460f, 100f), Color.white);
        entry.onClick.AddListener(controller.OpenPvpMenu);
        RuntimeUI.Localize(entry, "pvp_duel");
        var settings = GameObject.Find("Buttonsettings");
        if (settings != null && settings.transform.parent == canvas.transform)
            entry.transform.SetSiblingIndex(settings.transform.GetSiblingIndex() + 1);
    }

    static void ShowOnly(PvpGameController controller, GameObject panel)
    {
        controller.pvpMenuPanel.SetActive(false);
        controller.createPanel.SetActive(false);
        controller.joinPanel.SetActive(false);
        controller.matchPanel.SetActive(false);
        panel.SetActive(true);
    }
}
