using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace PassthroughCameraSamples.MultiObjectDetection
{

    public class InViewWarningUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _warningText;

        private static readonly Dictionary<string, string> _soundObjectMap = new()
        {
            { "Speech", "말"},
            { "Bark", "개"},
            { "Dog", "개" },
            { "Siren", "사이렌" },
            { "Vehicle horn", "경적" },
            { "Vehicle", "차" },
        };

        public void SetWarningText(string direction, string soundLabel)
        {
            string displaySoundLabel = _soundObjectMap[soundLabel];

            _warningText.text = $"{displaySoundLabel} 소리가\n 들리는 것 같습니다.\n 주위를 살펴보세요!";
        }
    }
}
