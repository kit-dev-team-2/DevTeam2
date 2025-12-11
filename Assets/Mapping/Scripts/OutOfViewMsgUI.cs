using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    public class OutOfViewMsgUI : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _warningText;

        private readonly Dictionary<string, string> _OutOfViewMsgMap = new()
        {
            { "Bark", "개"},
            { "Dog", "개" },
            { "Siren", "사이렌" },
            { "Vehicle horn", "경적" },
            { "Vehicle", "차" },
            { "Explosion", "폭발" },
        };

        public void SetWarningText(string direction, string soundLabel)
        {
            string displaySoundLabel = _OutOfViewMsgMap[soundLabel];

            _warningText.text = $"{displaySoundLabel} 소리가\n 들리는 것 같습니다.\n 주위를 살펴보세요!";
        }
    }
}
