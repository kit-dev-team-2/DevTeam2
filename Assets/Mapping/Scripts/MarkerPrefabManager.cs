using System;
using System.Collections.Generic;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [System.Serializable]
    public class MarkerPrefabMapping
    {
        [Tooltip("소리 레이블")]
        public string soundLabel;
        [Tooltip("이모지(마커) 프리팹")]
        public GameObject prefab;
    }

    public class MarkerPrefabManager : MonoBehaviour
    {
        [Tooltip("기본 프리팹")]
        [SerializeField] private GameObject m_defaultMarkerPrefab;
        [Tooltip("이모지(마커) 프리팹 목록")]
        [SerializeField] private List<MarkerPrefabMapping> m_markerPrefabMappings;

        // 각 프리팹의 "원래 scale"을 기억해 둘 딕셔너리
        private readonly Dictionary<GameObject, Vector3> _baseScales = new();

        private void Awake()
        {
            // 1) 모든 프리팹의 기본 스케일 캐싱
            CacheBaseScale(m_defaultMarkerPrefab);

            if (m_markerPrefabMappings != null)
            {
                foreach (var mapping in m_markerPrefabMappings)
                {
                    CacheBaseScale(mapping.prefab);
                }
            }

            // 2) 앱 시작 시 현재 설정값으로 한 번 적용
            if (SettingsManager.Instance != null && SettingsManager.Instance.Current != null)
            {
                ApplyEmojiScale(SettingsManager.Instance.Current.emojiScale);
            }
        }

        private void CacheBaseScale(GameObject prefab)
        {
            if (prefab == null) return;
            if (_baseScales.ContainsKey(prefab)) return;

            _baseScales[prefab] = prefab.transform.localScale;
        }

        /// <summary>
        /// Settings의 emojiScale 값에 따라 모든 이모지 프리팹의 스케일을 조정
        /// </summary>
        public void ApplyEmojiScale(float emojiScale)
        {
            // 기본 프리팹
            if (m_defaultMarkerPrefab != null &&
                _baseScales.TryGetValue(m_defaultMarkerPrefab, out var baseScale))
            {
                m_defaultMarkerPrefab.transform.localScale = baseScale * emojiScale;
            }

            // 매핑된 프리팹들
            if (m_markerPrefabMappings != null)
            {
                foreach (var mapping in m_markerPrefabMappings)
                {
                    if (mapping.prefab == null) continue;

                    if (_baseScales.TryGetValue(mapping.prefab, out var s))
                    {
                        mapping.prefab.transform.localScale = s * emojiScale;
                    }
                }
            }

            Debug.Log($"[MarkerPrefabManager] ApplyEmojiScale({emojiScale}) 완료");
        }

        public GameObject GetPrefabForSoundLabel(string label)
        {
            if (!string.IsNullOrEmpty(label))
            {
                foreach (var mapping in m_markerPrefabMappings)
                {
                    if (string.Equals(mapping.soundLabel, label, StringComparison.OrdinalIgnoreCase))
                    {
                        return mapping.prefab;
                    }
                }
            }
            return m_defaultMarkerPrefab;
        }
    }
}
