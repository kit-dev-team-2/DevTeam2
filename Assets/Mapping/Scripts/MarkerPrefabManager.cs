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
