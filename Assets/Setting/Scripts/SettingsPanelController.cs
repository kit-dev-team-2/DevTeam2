using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using PassthroughCameraSamples.MultiObjectDetection;

public class SettingsPanelController : MonoBehaviour
{
    [Header("Root")]
    public GameObject settingsPanel;    // SettingsPanel만 참조 (없으면 gameObject 써도 됨)

    [Header("Emoji Settings")]
    public Slider emojiScaleSlider;
    public Text emojiScaleValueText;

    public Slider emojiAlphaSlider;
    public Text emojiAlphaValueText;

    [Header("Model Config")]
    public Slider confThreshSlider;
    public Text confThreshValueText;

    public Slider MessageAlphaSlider;
    public Text MessageAlphaValueText;

    public Slider preBufferDurationSlider;
    public Text preBufferDurationValueText;

    [Header("Events")]
    public UnityEvent OnPanelClosed;

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
        MessageAlphaSlider.value = s.messageAlpha;
        preBufferDurationSlider.value = s.PRE_BUFFER_DURATION;
    }

    private void UpdateAllValueTexts()
    {
        UpdateEmojiScaleValueText(emojiScaleSlider.value);
        UpdateEmojiAlphaValueText(emojiAlphaSlider.value);

        UpdateConfThreshValueText(confThreshSlider.value);
        UpdateMessageAlphaValueText(MessageAlphaSlider.value);
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

    public void OnMessageAlphaSliderChanged(float value) => UpdateMessageAlphaValueText(value);
    private void UpdateMessageAlphaValueText(float value)
    {
        MessageAlphaValueText.text = value.ToString("0.00");
    }

    public void OnPreBufferDurationSliderChanged(float value) => UpdatePreBufferDurationValueText(value);
    private void UpdatePreBufferDurationValueText(float value)
    {
        preBufferDurationValueText.text = value.ToString("0.00") + " s";
    }

    private void ApplyMatchingAngleThresholdToDetectionManager()
    {
        var dm = FindObjectOfType<DetectionManager>(true);
        if (dm == null)
        {
            Debug.LogWarning("[SettingsPanel] DetectionManager not found.");
            return;
        }

        if (SettingsManager.Instance == null || SettingsManager.Instance.Current == null)
            return;

        // A안: PRE_BUFFER_DURATION을 "각도 임계값(°)"으로 의미 재사용
        float angleThres = SettingsManager.Instance.Current.PRE_BUFFER_DURATION;

        dm.SetMatchingAngleThreshold(angleThres);
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

        // 1) 슬라이더 값 → SettingsManager에 반영
        s.emojiScale = emojiScaleSlider.value;
        s.emojiAlpha = emojiAlphaSlider.value;
        s.CONF_THRESH = confThreshSlider.value;
        s.messageAlpha = MessageAlphaSlider.value;
        s.PRE_BUFFER_DURATION = preBufferDurationSlider.value;

        SettingsManager.Instance.Save();
        ApplyMatchingAngleThresholdToDetectionManager();

        // 2) 이모지 프리팹 스케일 적용
        var markerManager = FindObjectOfType<MarkerPrefabManager>();
        if (markerManager != null)
        {
            markerManager.ApplyEmojiScale(s.emojiScale);
        }

        var warning = FindObjectOfType<WarningMessageAlphaController>(true);
        if (warning != null)
        {
            warning.ApplyAlphaFromSettings();
        }

        if (QuestWsClient.Instance != null && QuestWsClient.Instance.IsConnected())
        {
            // fire-and-forget 방식으로 호출 (await 안 함)
            _ = QuestWsClient.Instance.SendConfigUpdateFromSettings();
        }
        else
        {
            Debug.LogWarning("[SettingsPanel] WS not connected. Cannot send config_update.");
        }

        Close();
    }


    public void OnClickCancel()
    {
        Close();
    }

    private void Close()
    {
        OnPanelClosed?.Invoke();

        // 패널 끄기
        var panel = settingsPanel != null ? settingsPanel : gameObject;
        panel.SetActive(false);
    }
}
