using UnityEngine;
using UnityEngine.UI;  // Image, Text 등 UI용

public class WarningMessageAlphaController : MonoBehaviour
{
    private Graphic[] _graphics;
    private Color[] _baseColors;

    private void Awake()
    {
        // WarningMessage 자식들에서 모든 UI Graphic(Image, Text 등) 모으기
        _graphics = GetComponentsInChildren<Graphic>(true);

        _baseColors = new Color[_graphics.Length];
        for (int i = 0; i < _graphics.Length; i++)
        {
            if (_graphics[i] != null)
            {
                _baseColors[i] = _graphics[i].color;
            }
        }

        ApplyAlphaFromSettings();
    }

    public void ApplyAlphaFromSettings()
    {
        float alpha = 1f;

        if (SettingsManager.Instance != null && SettingsManager.Instance.Current != null)
        {
            // 이제 DETECT_DURATION을 메시지 박스 알파로 사용
            alpha = SettingsManager.Instance.Current.messageAlpha;
        }

        alpha = Mathf.Clamp01(alpha);

        if (_graphics == null || _baseColors == null) return;

        for (int i = 0; i < _graphics.Length; i++)
        {
            if (_graphics[i] == null) continue;

            var c = _baseColors[i];
            c.a = c.a * alpha;   // 원래 알파에 곱하기
            _graphics[i].color = c;
        }
    }
}
