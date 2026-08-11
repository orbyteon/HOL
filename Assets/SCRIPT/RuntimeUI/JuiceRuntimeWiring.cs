using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Attaches the UIJuice components at runtime so no scene edits are needed:
//   - ButtonJuice on every Button (press squash + springy release)
//   - PanelAnimator on the menu panels (fade + rise on open)
//   - ConfettiBurst on the game panel, wired into GameManager.winConfetti
//   - the scene's click AudioSource handed to every unwired ButtonJuice
// Runs one frame after Start so runtime-built UI (PvP panels, consent
// dialog) already exists and gets juice too.
public class JuiceRuntimeWiring : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(WireNextFrame());
    }

    IEnumerator WireNextFrame()
    {
        yield return null; // let every other Start() finish first

        AddButtonJuice();
        AddPanelAnimators();
        AddConfetti();
        WireClickSounds();
    }

    // The scene has a dedicated click AudioSource, but only a few buttons
    // call Play() on it via serialized onClick — the rest are silent.
    // Discover that source from the existing persistent calls and hand it
    // (plus its clip) to every ButtonJuice whose button lacks the wiring,
    // so every button clicks exactly once.
    void WireClickSounds()
    {
        AudioSource clickSource = null;
        var buttons = FindObjectsOfType<Button>(true);

        foreach (var b in buttons)
        {
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
            {
                if (b.onClick.GetPersistentMethodName(i) == "Play"
                    && b.onClick.GetPersistentTarget(i) is AudioSource)
                {
                    clickSource = (AudioSource)b.onClick.GetPersistentTarget(i);
                    break;
                }
            }
            if (clickSource != null) break;
        }

        if (clickSource == null || clickSource.clip == null) return;

        // Cache for buttons built later at runtime (see RuntimeUI.AttachJuice).
        RuntimeUI.SharedClickSource = clickSource;
        RuntimeUI.SharedClickClip = clickSource.clip;

        foreach (var b in buttons)
        {
            bool alreadyWired = false;
            for (int i = 0; i < b.onClick.GetPersistentEventCount(); i++)
            {
                if (b.onClick.GetPersistentMethodName(i) == "Play"
                    && b.onClick.GetPersistentTarget(i) == clickSource)
                {
                    alreadyWired = true;
                    break;
                }
            }
            if (alreadyWired) continue;

            var juice = b.GetComponent<ButtonJuice>();
            if (juice != null && juice.clickSound == null)
            {
                juice.audioSource = clickSource;
                juice.clickSound = clickSource.clip;
            }
        }
    }

    void AddButtonJuice()
    {
        foreach (var button in FindObjectsOfType<Button>(true))
        {
            if (button.GetComponent<ButtonJuice>() == null)
                button.gameObject.AddComponent<ButtonJuice>();
        }
    }

    void AddPanelAnimators()
    {
        var menu = FindObjectOfType<MenuManager>();
        if (menu == null)
            return;

        var panels = new HashSet<GameObject>();
        if (menu.mainMenuPanel != null) panels.Add(menu.mainMenuPanel);
        if (menu.settingsPanel != null) panels.Add(menu.settingsPanel);
        if (menu.panelPlay != null) panels.Add(menu.panelPlay);
        if (menu.panelSearching != null) panels.Add(menu.panelSearching);

        foreach (var panel in panels)
        {
            if (panel.GetComponent<PanelAnimator>() == null)
                panel.AddComponent<PanelAnimator>();
        }
    }

    void AddConfetti()
    {
        var gm = FindObjectOfType<GameManager>();
        var mm = FindObjectOfType<FakeMatchmaking>();
        if (gm != null && mm != null && mm.panelGame != null && gm.winConfetti == null)
        {
            // Burst origin: invisible, centered on the game panel.
            var origin = RuntimeUI.CreateObject("ConfettiBurst", mm.panelGame.transform);
            RuntimeUI.Stretch(origin);

            gm.winConfetti = origin.AddComponent<ConfettiBurst>();
        }

        // PvP match panel gets its own burst origin for duel wins.
        var pvp = FindObjectOfType<PvpGameController>();
        if (pvp != null && pvp.matchPanel != null && pvp.winConfetti == null)
        {
            var origin = RuntimeUI.CreateObject("PvpConfettiBurst", pvp.matchPanel.transform);
            RuntimeUI.Stretch(origin);

            pvp.winConfetti = origin.AddComponent<ConfettiBurst>();
        }

        // PvP endgame reuses solo's scene-wired stingers.
        if (pvp != null && gm != null && pvp.audioSource == null)
        {
            pvp.audioSource = gm.audioSource;
            pvp.winSound = gm.winSound;
            pvp.loseSound = gm.loseSound;
        }
    }
}
