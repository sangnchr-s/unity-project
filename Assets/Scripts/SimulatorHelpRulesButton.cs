using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Кнопка «?»: по клику показывает панель с правилами, по PointerExit на кнопке (Event Trigger) — скрывает.
/// </summary>
[RequireComponent(typeof(Button))]
public class SimulatorHelpRulesButton : MonoBehaviour
{
    [SerializeField] GameObject rulesPopupPanel;

    public void ShowRulesPopup()
    {
        if (rulesPopupPanel != null)
            rulesPopupPanel.SetActive(true);
    }

    public void HideRulesPopup()
    {
        if (rulesPopupPanel != null)
            rulesPopupPanel.SetActive(false);
    }
}
