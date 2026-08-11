using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Attaches the UIJuice components at runtime so no scene edits are needed:
//   - ButtonJuice on every Button (press squash + springy release)
//   - PanelAnimator on the menu panels (fade + rise on open)
//   - ConfettiBurst on the game panel, wired into GameManager.winConfetti
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
    }
}
