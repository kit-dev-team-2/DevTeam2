using UnityEngine;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [RequireComponent(typeof(Image))]
    public class EdgeWarningEffect : MonoBehaviour
    {
        [Tooltip("반짝이는 속도")]
        [SerializeField] private float m_pulseSpeed = 2.0f;
        [Tooltip("최소 밝기 (알파값)")]
        [Range(0, 1)]
        [SerializeField] private float m_minAlpha = 0.1f;
        [Tooltip("최대 밝기 (알파값)")]
        [Range(0, 1)]
        [SerializeField] private float m_maxAlpha = 0.7f;

        private Image m_image;
        private Color m_originalColor;

        private void Awake()
        {
            m_image = GetComponent<Image>();
            if (m_image)
            {
                m_originalColor = m_image.color;
            }
        }

        private void OnEnable()
        {
            m_image.enabled = true;
        }

        private void Update()
        {
            float normalizedPulse = (Mathf.Sin(Time.time * m_pulseSpeed) + 1.0f) / 2.0f;
            float targetAlpha = Mathf.Lerp(m_minAlpha, m_maxAlpha, normalizedPulse);
            m_image.color = new Color(m_originalColor.r, m_originalColor.g, m_originalColor.b, targetAlpha);
        }
    }
}
