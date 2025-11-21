using System.Collections.Generic;
using UnityEngine;
using PassthroughCameraSamples.MultiObjectDetection; // BoundingBox를 위해 필요

public class SoundObjectMatcher : MonoBehaviour
{
    // 소리 레이블(Key)과 객체 클래스 이름(Value)을 매핑하는 딕셔너리
    private readonly Dictionary<string, string> _soundObjectMap = new()
    {
        { "Dog", "dog" },
        { "Laptop", "laptop" },
        { "Speech", "person" },
        // 필요한 만큼 추가
    };

    /// <summary>
    /// QuestWsClient에서 새로운 소리 데이터를 가져와, 그 소리와 매칭되는 객체들을 찾아 반환합니다.
    /// </summary>
    /// <param name="allDetectedObjects">현재 화면에서 감지된 모든 객체의 목록</param>
    /// <returns>소리와 매칭된 객체들의 목록</returns>
    public List<SentisInferenceUiManager.BoundingBox> GetMatchedObjects(List<SentisInferenceUiManager.BoundingBox> allDetectedObjects)
    {
        var matchedObjects = new List<SentisInferenceUiManager.BoundingBox>();

        // 1. QuestWsClient에서 최신 소리 데이터(SoundResultMessage)를 가져옵니다.
        var soundResult = QuestWsClient.Instance?.GetAndClearLatestSoundResult();
        if (soundResult == null || soundResult.tags == null || soundResult.tags.Length == 0)
        {
            // 새로운 소리 데이터가 없으면 빈 리스트를 반환합니다.
            return matchedObjects;
        }

        // 2. 받은 데이터에서 최고 점수의 소리 레이블(bestLabel)을 찾습니다.
        float bestScore = -1f;
        string bestLabel = "";
        foreach (var tag in soundResult.tags)
        {
            if (tag.score > bestScore)
            {
                bestScore = tag.score;
                bestLabel = tag.label;
            }
        }
        Debug.Log($"[SoundObjectMatcher] Best sound detected: {bestLabel} ({bestScore:F3})");

        // 3. 찾은 bestLabel과 매핑되는 객체 클래스 이름을 딕셔너리에서 찾습니다.
        if (string.IsNullOrEmpty(bestLabel) || !_soundObjectMap.TryGetValue(bestLabel, out string targetClassName))
        {
            // 매칭되는 규칙이 없으면 빈 리스트를 반환합니다.
            return matchedObjects;
        }

        // 4. 현재 감지된 모든 객체들 중에서 타겟 클래스 이름과 일치하는 객체를 찾습니다.
        foreach (var detectedObject in allDetectedObjects)
        {
            if (detectedObject.ClassName == targetClassName)
            {
                matchedObjects.Add(detectedObject);
            }
        }

        return matchedObjects;
    }
}
