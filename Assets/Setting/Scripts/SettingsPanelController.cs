using UnityEngine;
using UnityEngine.UI;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject settingsPanel;    // SettingsPanel만 참조 (없으면 gameObject 써도 됨)
    public GameObject settingsButton;     // SettingsButton

    [Header("Emoji Settings")]
    public Slider emojiScaleSlider;
    public Text emojiScaleValueText;

    public Slider emojiAlphaSlider;
    public Text emojiAlphaValueText;

    [Header("Model Config")]
    public Slider confThreshSlider;
    public Text confThreshValueText;

    public Slider detectDurationSlider;
    public Text detectDurationValueText;

    public Slider preBufferDurationSlider;
    public Text preBufferDurationValueText;

    private void OnEnable()
    {
        if (SettingsManager.Instance != null && SettingsManager.Instance.Current != null)
        {
            LoadFromSettings();
        }

        UpdateAllValueTexts();
    }

    private void LoadFromSettings()
    {
        var s = SettingsManager.Instance.Current;

        emojiScaleSlider.value = s.emojiScale;
        emojiAlphaSlider.value = s.emojiAlpha;

        confThreshSlider.value = s.CONF_THRESH;
        detectDurationSlider.value = s.DETECT_DURATION;
        preBufferDurationSlider.value = s.PRE_BUFFER_DURATION;
    }

    private void UpdateAllValueTexts()
    {
        UpdateEmojiScaleValueText(emojiScaleSlider.value);
        UpdateEmojiAlphaValueText(emojiAlphaSlider.value);

        UpdateConfThreshValueText(confThreshSlider.value);
        UpdateDetectDurationValueText(detectDurationSlider.value);
        UpdatePreBufferDurationValueText(preBufferDurationSlider.value);
    }

    // 슬라이더 핸들러들은 그대로 유지
    public void OnEmojiScaleSliderChanged(float value) => UpdateEmojiScaleValueText(value);
    private void UpdateEmojiScaleValueText(float value)
    {
        emojiScaleValueText.text = value.ToString("0.00") + "x";
    }

    public void OnEmojiAlphaSliderChanged(float value) => UpdateEmojiAlphaValueText(value);
    private void UpdateEmojiAlphaValueText(float value)
    {
        emojiAlphaValueText.text = value.ToString("0.00");
    }

    public void OnConfThreshSliderChanged(float value) => UpdateConfThreshValueText(value);
    private void UpdateConfThreshValueText(float value)
    {
        confThreshValueText.text = value.ToString("0.00");
    }

    public void OnDetectDurationSliderChanged(float value) => UpdateDetectDurationValueText(value);
    private void UpdateDetectDurationValueText(float value)
    {
        detectDurationValueText.text = value.ToString("0.00") + " s";
    }

    public void OnPreBufferDurationSliderChanged(float value) => UpdatePreBufferDurationValueText(value);
    private void UpdatePreBufferDurationValueText(float value)
    {
        preBufferDurationValueText.text = value.ToString("0.00") + " s";
    }

    // 버튼
    public void OnClickSave()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.Current == null)
        {
            Debug.LogWarning("SettingsManager or Current is null. Cannot save.");
            Close();
            return;
        }

        var s = SettingsManager.Instance.Current;

        s.emojiScale = emojiScaleSlider.value;
        s.emojiAlpha = emojiAlphaSlider.value;

        s.CONF_THRESH = confThreshSlider.value;
        s.DETECT_DURATION = detectDurationSlider.value;
        s.PRE_BUFFER_DURATION = preBufferDurationSlider.value;

        SettingsManager.Instance.Save();

        if (QuestWsClient.Instance != null)
        {
            // QuestWsClient.Instance.SendConfigUpdateFromSettings();
        }

        Close();
    }

    public void OnClickCancel()
    {
        Close();
    }

    private void Close()
    {
        // 자기 자신만 끄면 됨
        var panel = settingsPanel != null ? settingsPanel : gameObject;
        panel.SetActive(false);

        // Setting 버튼 다시 켜기
        if (settingsButton != null)
            settingsButton.SetActive(true);
    }
}
