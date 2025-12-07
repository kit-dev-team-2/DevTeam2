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

    /// ["소리 레이블", "객체 레이블"]
    private readonly Dictionary<string, List<string>> _soundObjectMap = new()
    {
        { "Speech", new List<string> { "person" } },
        { "Bark", new List<string> { "dog" } },
        { "Dog", new List<string> { "dog" } },
        { "Siren", new List<string> { "car" } },
        { "Vehicle horn", new List<string> { "car", "bus", "truck", "motorbike" } },
        { "Vehicle", new List<string> { "car", "bus", "truck", "motorbike" } },
    };

    // OutOfView 경고 매칭용 셋 
    private readonly HashSet<string> _warningSoundObjectMap = new()
    {
        "Bark", "Dog", "Vehicle horn", "Vehicle", "Siren", "Explosion"
    };

    private readonly List<SentisInferenceUiManager.BoundingBox> _reusableMatchedObjects = new();

    /// <summary>
    /// QuestWsClient에서 새로운 소리 데이터를 가져와, 그 소리와 매칭되는 객체를 찾아 결과를 반환합니다.
    /// </summary>
    /// <param name="allDetectedObjects">현재 화면에서 감지된 모든 객체의 목록</param>
    /// <returns>소리 매칭 결과(SoundMatchResult)</returns>
    public SoundMatchResult GetMatchedObjects(List<SentisInferenceUiManager.BoundingBox> allDetectedObjects)
    {
        // QuestWsClient에서 최신 소리 데이터(SoundResultMessage)를 가져옵니다.
        var soundResult = QuestWsClient.Instance?.GetAndClearLatestSoundResult();
        if (soundResult == null || soundResult.tags == null || soundResult.tags.Length == 0)
        {
            // 상태: 새로운 소리 없음
            return new SoundMatchResult(SoundMatchResultType.NoNewSound);
        }

        // 받은 데이터에서 최고 점수의 소리 레이블(bestLabel)을 찾습니다.
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

        #region 시야 밖 처리
        // DoA(소리 방향)가 시야 밖인지 확인
        if (doa > sight && doa < 360 - sight)
        {
            // warningSoundObjectMap에 정의된 소리인지 확인
            if (_warningSoundObjectMap.Contains(bestLabel))
            {
                // 상태: 시야 밖
                return new SoundMatchResult(SoundMatchResultType.OutOfView, bestLabel, doa);
            }
            else
            {
                // 상태: 매칭 규칙 없음 (무시)
                return new SoundMatchResult(SoundMatchResultType.NoMatchingRule, bestLabel, doa);
            }
        }
        #endregion

        #region 시야 내 처리
        if (string.IsNullOrEmpty(bestLabel) || !_soundObjectMap.TryGetValue(bestLabel, out var targetClassNames))
        {
            // 상태: 매칭 규칙 없음 (무시)
            return new SoundMatchResult(SoundMatchResultType.NoMatchingRule, bestLabel, doa);
        }

        // 시야 내에 타겟 클래스를 가진 객체가 있는지 확인
        _reusableMatchedObjects.Clear();
        foreach (var detectedObject in allDetectedObjects)
        {
            if (targetClassNames.Contains(detectedObject.ClassName))
            {
                _reusableMatchedObjects.Add(detectedObject);
            }
        }

        if (_reusableMatchedObjects.Count == 0)
        {
            // 상태: 시야 내에 매칭되는 객체 없음
            return new SoundMatchResult(SoundMatchResultType.NoObjectInView, bestLabel, doa);
        }

        // 상태: 매칭 성공
        return new SoundMatchResult(SoundMatchResultType.MatchFound, bestLabel, doa, _reusableMatchedObjects);
        #endregion
    }
}