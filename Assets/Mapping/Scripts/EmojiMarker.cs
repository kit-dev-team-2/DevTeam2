using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    public class EmojiMarker : MonoBehaviour
    {
        private string _soundLabel;
        private OVRCameraRig m_camera;

        // === 크기 / 알파 관리용 필드 ===
        private Vector3 _baseScale;

        // SpriteRenderer용
        private SpriteRenderer[] _spriteRenderers;
        private Color[] _baseSpriteColors;
        // ===============================

        private void Awake()
        {
            // 1) 기본 스케일 저장
            _baseScale = transform.localScale;

            // 2) 자식까지 포함한 SpriteRenderer들 + 원래 색 저장
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            _baseSpriteColors = new Color[_spriteRenderers.Length];
            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                _baseSpriteColors[i] = _spriteRenderers[i].color;
            }

            // 3) 현재 설정(emojiScale, emojiAlpha) 적용
            ApplySettingsFromManager();
        }

        private void Update()
        {
            if (!m_camera)
            {
                m_camera = FindFirstObjectByType<OVRCameraRig>();
            }
            else
            {
                transform.LookAt(m_camera.centerEyeAnchor);
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

        /// <summary>
        /// SettingsManager의 emojiScale / emojiAlpha를 모두 반영
        /// </summary>
        public void ApplySettingsFromManager()
        {
            float scale = 1f;
            float alpha = 1f;

            if (SettingsManager.Instance != null && SettingsManager.Instance.Current != null)
            {
                scale = SettingsManager.Instance.Current.emojiScale;
                alpha = SettingsManager.Instance.Current.emojiAlpha;
            }

            ApplyScale(scale);
            ApplyAlpha(alpha);
        }

        private void ApplyScale(float scale)
        {
            transform.localScale = _baseScale * scale;
        }

        private void ApplyAlpha(float alpha)
        {
            alpha = Mathf.Clamp01(alpha);

            for (int i = 0; i < _spriteRenderers.Length; i++)
            {
                if (_spriteRenderers[i] == null) continue;

                var baseColor = _baseSpriteColors[i];
                baseColor.a = baseColor.a * alpha; // 원래 알파에 곱하기
                _spriteRenderers[i].color = baseColor;
            }
        }
    }
}
