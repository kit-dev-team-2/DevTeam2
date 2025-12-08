using UnityEngine;
using UnityEngine.UI; // UI Image 사용하는 경우

public class EmojiMarker : MonoBehaviour
{
    private string _soundLabel;
    private OVRCameraRig m_camera;

    private void Start()
    {
        ApplySettings();
    }

    private void ApplySettings()
    {
        if (SettingsManager.Instance == null || SettingsManager.Instance.Current == null) return;

        AppSettings s = SettingsManager.Instance.Current;

        // 1. 크기 적용
        transform.localScale = Vector3.one * s.emojiScale;

        // 2. 알파 적용
        // SpriteRenderer 사용하는 경우
        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = s.emojiAlpha;
            sr.color = c;
        }

        // UI Image 사용하는 경우
        Image img = GetComponentInChildren<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = s.emojiAlpha;
            img.color = c;
        }
    }

    private void Update()
    {
        if (!m_camera)
        {
            m_camera = FindFirstObjectByType<OVRCameraRig>();
        }
        else
        {
            Transform cam = m_camera.centerEyeAnchor;
            Vector3 dir = transform.position - cam.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(dir);
            }
        }
    }

    public void SetSoundLabel(string label)
    {
        _soundLabel = label;
    }

    public string GetSoundLabel()
    {
        return _soundLabel;
    }
}
