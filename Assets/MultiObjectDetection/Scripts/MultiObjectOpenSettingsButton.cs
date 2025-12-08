using UnityEngine;

public class MultiObjectOpenSettingsButton : MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;    // SettingsPanel
    [SerializeField] private GameObject settingsButton;   // 자기 자신(Setting 버튼)

    public void OnClickOpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);

        if (settingsButton != null)
            settingsButton.SetActive(false);
    }
}
