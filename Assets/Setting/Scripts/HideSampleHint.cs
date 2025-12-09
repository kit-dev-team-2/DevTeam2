using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HideSampleHint : MonoBehaviour
{
    // 전체 문장을 몰라도 "press menu" 만 들어가면 잡도록
    private const string TargetSubstring = "press menu";

    // 얼마나 자주 / 얼마나 오래 스캔할지
    [SerializeField] private float scanIntervalSeconds = 0.5f;
    [SerializeField] private float maxScanDurationSeconds = 30f;

    private bool _hintDisabled = false;

    private void Start()
    {
        Debug.Log("[HideSampleHint] Start in scene: " + gameObject.scene.name);
        StartCoroutine(ScanRoutine());
    }

    private IEnumerator ScanRoutine()
    {
        float elapsed = 0f;

        while (elapsed < maxScanDurationSeconds && !_hintDisabled)
        {
            bool foundThisScan = false;

            // 1) UGUI Text
            var texts = FindObjectsOfType<Text>(true);
            foreach (var t in texts)
            {
                if (string.IsNullOrEmpty(t.text)) continue;

                var lower = t.text.ToLower();
                if (lower.Contains(TargetSubstring))
                {
                    Debug.Log("[HideSampleHint] Disable UGUI Text: \"" + t.text + "\" on " + t.gameObject.name);
                    t.gameObject.SetActive(false);
                    foundThisScan = true;
                }
            }

            // 2) TextMeshProUGUI
            var tmps = FindObjectsOfType<TextMeshProUGUI>(true);
            foreach (var tmp in tmps)
            {
                if (string.IsNullOrEmpty(tmp.text)) continue;

                var lower = tmp.text.ToLower();
                if (lower.Contains(TargetSubstring))
                {
                    Debug.Log("[HideSampleHint] Disable TMP: \"" + tmp.text + "\" on " + tmp.gameObject.name);
                    tmp.gameObject.SetActive(false);
                    foundThisScan = true;
                }
            }

            if (foundThisScan)
            {
                _hintDisabled = true;
                Debug.Log("[HideSampleHint] Hint text found and disabled. Stop scanning.");
                yield break;
            }

            yield return new WaitForSeconds(scanIntervalSeconds);
            elapsed += scanIntervalSeconds;
        }

        Debug.Log("[HideSampleHint] Scan finished. Hint not found within " + maxScanDurationSeconds + " seconds.");
    }
}
