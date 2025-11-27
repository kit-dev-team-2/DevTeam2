using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PassthroughCameraSamples.MultiObjectDetection; // BoundingBox를 위해 필요
public enum SoundMatchResultType
{
    NoNewSound,         // 새로운 소리가 안들어온 경우
    NoMatchingRule,     // 딕셔너리에 존재하지 않는 소리가 들어온 없는 경우
    OutOfView,          // doa가 시야 밖인 경우
    NoObjectInView,     // doa가 시야 내지만, 시야에 객체가 존재하지 않는 경우
    MatchFound,          // doa가 시야 내이며, 시야에 객체가 존재하는 경우
}

public class SoundMatchResult
{
    public SoundMatchResultType ResultType { get; }
    public string SoundLabel { get; }
    public int Doa { get; }
    public List<SentisInferenceUiManager.BoundingBox> MatchedObjects { get; }

    public SoundMatchResult(SoundMatchResultType resultType, string soundLabel = "", int doa = 0, List<SentisInferenceUiManager.BoundingBox> matchedObjects = null)
    {
        ResultType = resultType;
        SoundLabel = soundLabel;
        Doa = doa;
        MatchedObjects = matchedObjects ?? new List<SentisInferenceUiManager.BoundingBox>();
    }
}

public class SoundObjectMatcher : MonoBehaviour
{
    [SerializeField] public int sight = 50; // 시야 각도 설정 +-sight

    /// ["소리에서 인식된 라벨", "객체 인식된 라벨"]
    private readonly Dictionary<string, string> _soundObjectMap = new()
    {
        { "Speech", "person" },
        { "Bark", "dog"},
        { "Dog", "dog" },
        { "Vehicle horn", "car" },
        { "Vehicle", "car" },
        { "Siren", "car" },
    };
    /// <summary>
    /// QuestWsClient에서 새로운 소리 데이터를 가져와, 그 소리와 매칭되는 객체를 찾아 결과를 반환합니다.
    /// </summary>
    /// <param name="allDetectedObjects">현재 화면에서 감지된 모든 객체의 목록</param>
    /// <returns>소리 매칭 결과(SoundMatchResult)</returns>
    public SoundMatchResult GetMatchedObjects(List<SentisInferenceUiManager.BoundingBox> allDetectedObjects)
    {
        // 1. QuestWsClient에서 최신 소리 데이터(SoundResultMessage)를 가져옵니다.
        var soundResult = QuestWsClient.Instance?.GetAndClearLatestSoundResult();
        if (soundResult == null || soundResult.tags == null || soundResult.tags.Length == 0)
        {
            // 상태 0: 새로운 소리 없음
            return new SoundMatchResult(SoundMatchResultType.NoNewSound);
        }

        // 2. 받은 데이터에서 최고 점수의 소리 레이블(bestLabel)을 찾습니다.
        float bestScore = -1f;
        string bestLabel = "";
        int doa = soundResult.doa;
        foreach (var tag in soundResult.tags)
        {
            if (tag.score > bestScore)
            {
                bestScore = tag.score;
                bestLabel = tag.label;
            }
        }
        Debug.Log($"[SoundObjectMatcher] Best sound detected: {bestLabel} ({bestScore:F3}) at DoA: {doa}");

        // 3. 찾은 bestLabel과 매핑되는 객체 클래스 이름을 딕셔너리에서 찾습니다.
        if (string.IsNullOrEmpty(bestLabel) || !_soundObjectMap.TryGetValue(bestLabel, out string targetClassName))
        {
            // 상태 1: 매칭 규칙 없음
            return new SoundMatchResult(SoundMatchResultType.NoMatchingRule, bestLabel, doa);
        }

        // 4. DoA(소리 방향)가 시야 밖인 경우 먼저 처리합니다.
        if (doa > sight && doa < 360 - sight)
        {
            Debug.Log($"[SoundObjectMatcher] Sound is out of view (DoA: {doa}).");
            return new SoundMatchResult(SoundMatchResultType.OutOfView, bestLabel, doa);
        }

        // 5. 시야 내에 타겟 클래스를 가진 객체가 있는지 확인합니다.
        var objectsInView = allDetectedObjects.Where(obj => obj.ClassName == targetClassName).ToList();
        if (objectsInView.Count == 0)
        {
            // 상태 3: 시야 내에 매칭되는 객체 없음
            Debug.Log($"[SoundObjectMatcher] Sound is in view, but no object of class '{targetClassName}' found.");
            return new SoundMatchResult(SoundMatchResultType.NoObjectInView, bestLabel, doa);
        }

        // 6. 매칭 성공
        Debug.Log($"[SoundObjectMatcher] Result: Match Found! Sound '{bestLabel}' matched with {objectsInView.Count} object(s) of class '{targetClassName}'.");
        return new SoundMatchResult(SoundMatchResultType.MatchFound, bestLabel, doa, objectsInView);
    }
}