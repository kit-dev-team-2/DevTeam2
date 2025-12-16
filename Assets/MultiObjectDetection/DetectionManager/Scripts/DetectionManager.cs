// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Meta.XR;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionManager : MonoBehaviour
    {
        /// <summary>
        /// FindBestObjectForDoa 함수의 결과를 나타냅니다.
        /// </summary>
        private enum FindBestObjectResult
        {
            Success,      // 명확한 객체를 찾음
            NoMatch,      // 조건에 맞는 객체를 찾지 못함
            Ambiguous     // 여러 객체가 후보이고, 조건이 모호함
        }

        [SerializeField] private PassthroughCameraAccess m_cameraAccess;

        [Header("Controls configuration")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Matching configuration")]
        [Tooltip("소리의 방향과 객체의 방향 사이의 최대 허용 각도입니다.")]
        [SerializeField, Range(0, 30)] private float m_matchingAngleThreshold = 5.0f;
        public void SetMatchingAngleThreshold(float value)
        {
            m_matchingAngleThreshold = value;
        }

        [Tooltip("두 객체의 소리 방향 각도 차이가 이 값 미만일 경우, 모호한 상황으로 간주하여 거리 비교를 시작합니다.")]
        [SerializeField, Range(0, 10)] private float m_ambiguousAngleThreshold = 5.0f;

        [Header("Ui references")]
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;
        [Tooltip("시야 밖 소리 경고 UI: msg")]
        [SerializeField] private GameObject m_outOfViewMsg;
        [Tooltip("시야 밖 소리 경고 UI: 왼쪽")]
        [SerializeField] private GameObject m_outOfViewBorderLeft;
        [Tooltip("시야 밖 소리 경고 UI: 아래쪽")]
        [SerializeField] private GameObject m_outOfViewBorderBottom;
        [Tooltip("시야 밖 소리 경고 UI: 오른쪽")]
        [SerializeField] private GameObject m_outOfViewBorderRight;

        [Tooltip("경고 메시지 UI: 왼쪽")]
        [SerializeField] private GameObject m_NoObjectInViewMsgLeft;
        [Tooltip("경고 메시지 UI: 정면")]
        [SerializeField] private GameObject m_NoObjectInViewMsgFront;
        [Tooltip("경고 메시지 UI: 오른쪽")]
        [SerializeField] private GameObject m_NoObjectInViewMsgRight;

        [Header("Placement configureation")]
        [SerializeField] private MarkerPrefabManager m_markerPrefabManager; // 이모지 프리팹 매니저
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private float m_spawnDistance = 0.25f; // 최소 거리
        [SerializeField] private AudioSource m_placeSound;

        [Header("UI Lifetime")]
        [Tooltip("새로운 소리 이벤트가 없을 때 시각적 요소(마커, 경고)가 유지되는 시간(초)입니다.")]
        [SerializeField] private float m_visualsLifetime = 2.0f;
        private float m_visualsTimer;

        [Header("Sentis inference ref")]
        [SerializeField] private SentisInferenceRunManager m_runInference;
        [SerializeField] private SentisInferenceUiManager m_uiInference;
        [Space(10)]
        [SerializeField] private SoundObjectMatcher m_soundObjectMatcher;   // 사운드-객체 매칭 매니저
        [Space(10)]
        public UnityEvent<int> OnObjectsIdentified;

        private bool m_isPaused = true;
        private List<GameObject> m_spwanedEntities = new();
        private bool m_isStarted = false;
        private float m_delayPauseBackTime = 0;

        #region Unity Functions
        private void Awake() => OVRManager.display.RecenteredPose += CleanMarkersCallBack;

        private void OnDestroy() => OVRManager.display.RecenteredPose -= CleanMarkersCallBack;

        private IEnumerator Start()
        {
            // if (m_NoObjectInViewMsgBottom) m_NoObjectInViewMsgBottom.SetActive(true);
            // 시작 로직은 이제 DetectionUiMenuManager가 제어합니다.
            yield return null;
        }

        private void Update()
        {
            // Passthrough 카메라가 준비되지 않았다면 아무것도 하지 않고 화면을 계속 렌더링합니다.
            if (!m_cameraAccess.IsPlaying)
            {
                return;
            }

            // Passthrough 카메라 렌더링을 최우선으로 실행합니다.
            // 이렇게 하면 일시정지 상태에서도 배경 화면이 꺼지지 않습니다.
            if (!m_runInference.IsRunning())
            {
                m_runInference.RunInference(m_cameraAccess);
            }

            // 탐지가 시작되었고, 앱이 일시정지 상태가 아닐 때만 탐지 로직을 실행합니다.
            if (m_isStarted)
            {
                // 매 프레임 WebSocket 연결 상태를 확인합니다.
                if (QuestWsClient.Instance != null && !QuestWsClient.Instance.IsConnected())
                {
                    ClearAllDetectionVisuals();
                    return;
                }

                if (!m_isPaused)
                {
                    // 매 프레임, 새로운 소리와 매칭되는 객체가 있는지 확인하고 마커를 생성합니다.
                    SpawnMarkersForMatchedObjects();

                    // 타이머가 활성화되어 있으면 시간을 감소시킵니다.
                    if (m_visualsTimer > 0)
                    {
                        m_visualsTimer -= Time.deltaTime;
                        if (m_visualsTimer <= 0)
                        {
                            // 타이머가 만료되면 모든 시각적 요소를 지웁니다.
                            ClearAllDetectionVisuals();
                        }
                    }
                }
                else
                {
                    // Set the delay time for the A button to return from the pause menu
                    m_delayPauseBackTime = 0.1f;
                }
            }
        }
        #endregion

        #region Marker Functions
        /// <summary>
        /// Clean 3d markers when the tracking space is re-centered.
        /// 이제 이 메서드는 모든 시각적 요소를 지우는 역할을 합니다.
        /// </summary>
        public void ClearAllDetectionVisuals()
        {
            ClearAllMarkers();
            ClearAllWarnings();
        }

        /// <summary>
        /// 생성된 모든 마커(이모지)를 제거합니다.
        /// </summary>
        private void ClearAllMarkers()
        {
            foreach (var e in m_spwanedEntities)
            {
                Destroy(e);
            }
            m_spwanedEntities.Clear();
            OnObjectsIdentified?.Invoke(-1);
        }

        /// <summary>
        /// 화면에 표시된 모든 경고 UI (테두리, 메시지)를 비활성화합니다.
        /// </summary>
        private void ClearAllWarnings()
        {
            if (m_outOfViewMsg) m_outOfViewMsg.SetActive(false);
            if (m_outOfViewBorderLeft) m_outOfViewBorderLeft.SetActive(false);
            if (m_outOfViewBorderBottom) m_outOfViewBorderBottom.SetActive(false); // This seems to be for OutOfView, not NoObjectInView
            if (m_outOfViewBorderRight) m_outOfViewBorderRight.SetActive(false);
            if (m_NoObjectInViewMsgLeft) m_NoObjectInViewMsgLeft.SetActive(false);
            if (m_NoObjectInViewMsgFront) m_NoObjectInViewMsgFront.SetActive(false);
            if (m_NoObjectInViewMsgRight) m_NoObjectInViewMsgRight.SetActive(false);
        }

        /// <summary>
        /// OVRManager의 RecenteredPose 이벤트에 연결될 콜백 함수입니다.
        /// </summary>
        private void CleanMarkersCallBack()
        {
            ClearAllDetectionVisuals();
        }

        /// <summary>
        /// SoundObjectMatcher를 통해 소리와 매칭된 객체를 찾아 마커를 생성합니다.
        /// </summary>
        private void SpawnMarkersForMatchedObjects()
        {
            var count = 0;
            // SoundObjectMatcher를 사용해 현재 소리와 매칭되는 객체 목록을 가져옵니다.
            var allDetectedObjects = m_uiInference.BoxDrawn;
            var matchResult = m_soundObjectMatcher.GetMatchedObjects(allDetectedObjects);

            // DoA를 월드 좌표계의 수평 각도(Y축 기준)로 변환
            float soundAngle = GetWorldAngleFromDoa(matchResult.Doa);

            // 새로운 소리가 감지되었을 경우(성공/실패 무관) 기존 마커를 모두 지웁니다.
            if (matchResult.ResultType != SoundMatchResultType.NoNewSound)
            {
                ClearAllDetectionVisuals();
                // 새로운 소리 이벤트가 감지되었으므로, 타이머를 초기화합니다.
                m_visualsTimer = m_visualsLifetime;
            }

            if (matchResult.ResultType == SoundMatchResultType.MatchFound)
            {
                var findResult = FindBestObjectForDoa(soundAngle, matchResult, out var bestMatchedObject);

                switch (findResult)
                {
                    // 1. 명확한 객체를 찾은 경우: 마커를 표시합니다.
                    case FindBestObjectResult.Success:
                        if (PlaceMarkerUsingEnvironmentRaycast(bestMatchedObject.WorldPos, matchResult.SoundLabel))
                        {
                            count++;
                        }
                        break;

                    // 2. 모호한 상황인 경우: "주변" 메시지를 표시합니다.
                    case FindBestObjectResult.Ambiguous:
                        var ambiguousWarningUI = m_NoObjectInViewMsgFront.GetComponent<InViewWarningUI>();
                        ambiguousWarningUI?.SetWarningText("주변", matchResult.SoundLabel);
                        m_NoObjectInViewMsgFront.SetActive(true);
                        break;

                    // 3. 조건에 맞는 객체를 못 찾은 경우: 기존처럼 방향별 메시지를 표시합니다.
                    case FindBestObjectResult.NoMatch:
                        // 시야에 객체는 있지만 각도가 맞지 않는 경우, NoObjectInView와 동일하게 처리합니다.
                        ShowDirectionalWarning(matchResult.Doa, matchResult.SoundLabel);
                        break;
                }
            }
            // NoObjectInView인 경우, DoA 방향에 경고 UI를 표시
            else if (matchResult.ResultType == SoundMatchResultType.NoObjectInView)
            {
                ShowDirectionalWarning(matchResult.Doa, matchResult.SoundLabel);
            }
            // OutOfView 인 경우 화면 경고 표시
            else if (matchResult.ResultType == SoundMatchResultType.OutOfView)
            {

                int doa = matchResult.Doa;
                string soundLabel = matchResult.SoundLabel;
                // DoA 값에 따라 방향을 결정하고 해당 테두리를 활성화합니다.
                // 남동 (sight < doa <= 150) -> 오른쪽 테두리
                if (doa > m_soundObjectMatcher.sight && doa <= 150)
                {
                    m_outOfViewBorderLeft.SetActive(true);

                    var warningUI = m_outOfViewMsg.GetComponent<OutOfViewMsgUI>();
                    warningUI?.SetWarningText("오른쪽 뒤", soundLabel);
                    m_outOfViewMsg.SetActive(true);
                }
                // 남서 (210 <= doa < 360 - sight) -> 왼쪽 테두리
                else if (doa >= 210 && doa < 360 - m_soundObjectMatcher.sight)
                {
                    m_outOfViewBorderRight.SetActive(true);

                    var warningUI = m_outOfViewMsg.GetComponent<OutOfViewMsgUI>();
                    warningUI?.SetWarningText("왼쪽 뒤", soundLabel);
                    m_outOfViewMsg.SetActive(true);

                }
                // 남 (150 < doa < 210) -> 아래쪽 테두리
                else
                {
                    m_outOfViewBorderBottom.SetActive(true);

                    var warningUI = m_outOfViewMsg.GetComponent<OutOfViewMsgUI>();
                    warningUI?.SetWarningText("뒤", soundLabel);
                    m_outOfViewMsg.SetActive(true);
                }
            }

            if (count > 0)
            {
                // Play sound if a new marker is placed.
                m_placeSound.Play();
            }
            OnObjectsIdentified?.Invoke(count);
        }

        /// <summary>
        /// DOA 값에 따라 방향별 경고 UI를 표시합니다.
        /// </summary>
        private void ShowDirectionalWarning(int doa, string soundLabel)
        {
            // 오른쪽
            if (doa > 20 && doa <= m_soundObjectMatcher.sight)
            {
                var warningUI = m_NoObjectInViewMsgLeft.GetComponent<InViewWarningUI>();
                warningUI?.SetWarningText("오른쪽", soundLabel);
                m_NoObjectInViewMsgLeft.SetActive(true);
            }
            // 왼쪽
            else if (doa >= 360 - m_soundObjectMatcher.sight && doa < 340)
            {
                var warningUI = m_NoObjectInViewMsgRight.GetComponent<InViewWarningUI>();
                warningUI?.SetWarningText("왼쪽", soundLabel);
                m_NoObjectInViewMsgRight.SetActive(true);
            }
            // 정면
            else
            {
                var warningUI = m_NoObjectInViewMsgFront.GetComponent<InViewWarningUI>();
                warningUI?.SetWarningText("정면", soundLabel);
                m_NoObjectInViewMsgFront.SetActive(true);
            }
        }

        /// <summary>
        /// DOA 값과 가장 가까운 화면상 위치의 객체를 찾습니다.
        /// </summary>
        private FindBestObjectResult FindBestObjectForDoa(float soundAngle, SoundMatchResult matchResult, out SentisInferenceUiManager.BoundingBox bestObject)
        {
            bestObject = default; // bestObject를 기본값으로 초기화
            if (matchResult.MatchedObjects.Count == 0) return FindBestObjectResult.NoMatch;

            var camera = FindFirstObjectByType<OVRCameraRig>().centerEyeAnchor;
            List<(SentisInferenceUiManager.BoundingBox obj, float angleDiff, float distance)> candidates = new();

            // 모든 매칭된 객체 중에서, 소리 방향과 가장 가까운 방향에 있는 객체를 찾습니다.
            foreach (var obj in matchResult.MatchedObjects)
            {
                if (!obj.WorldPos.HasValue) continue; // 객체의 3D 위치가 없으면 건너뜁니다.

                Vector3 toObject = obj.WorldPos.Value - camera.position;
                float objectAngle = Vector3.SignedAngle(camera.forward, toObject, Vector3.up);
                float difference = Mathf.Abs(Mathf.DeltaAngle(soundAngle, objectAngle));

                // 1. 각도 차이가 허용 임계값(m_matchingAngleThreshold) 이내인 객체만 후보로 추가합니다.
                if (difference <= m_matchingAngleThreshold)
                {
                    candidates.Add((obj, difference, toObject.magnitude));
                }
            }

            if (candidates.Count == 0)
            {
                Debug.Log($"[DetectionManager] No objects within angle threshold ({m_matchingAngleThreshold}°).");
                return FindBestObjectResult.NoMatch;
            }

            // 후보들을 각도 차이가 적은 순으로 정렬합니다.
            candidates.Sort((a, b) => a.angleDiff.CompareTo(b.angleDiff));

            bestObject = candidates[0].obj;
            float minAngleDiff = candidates[0].angleDiff;

            // 2. 후보가 2개 이상이고, 1순위와 2순위의 각도 차이가 매우 작은지(모호한지) 확인합니다.
            if (candidates.Count > 1 && (candidates[1].angleDiff - minAngleDiff) < m_ambiguousAngleThreshold)
            {
                // 2-2. 모호한 상황: 1순위와 2순위의 '거리'가 매우 가까운지 확인합니다. (예: 0.5미터 이내)
                if (Mathf.Abs(candidates[1].distance - candidates[0].distance) < 0.5f)
                {
                    Debug.Log($"[DetectionManager] Ambiguous situation: Multiple objects at similar angles and distances. Showing message box.");
                    return FindBestObjectResult.Ambiguous; // 모호한 상태로 반환
                }
            }

            // 2-1. 모호하지 않거나, 모호하더라도 거리 차이가 충분히 나서 가장 가까운 객체를 선택한 경우
            Debug.Log($"[DetectionManager] Best match found: {bestObject.ClassName}, Angle Diff: {minAngleDiff:F2}°, Dist: {candidates[0].distance:F2}m. Yes match.");
            return FindBestObjectResult.Success;
        }

        /// <summary>
        /// DOA 각도를 월드 좌표계의 수평 각도(Y축 기준)로 변환합니다.
        /// </summary>
        private float GetWorldAngleFromDoa(int doa)
        {
            // 1. DoA 각도를 -180 ~ 180 범위로 정규화합니다. (예: 350도 -> -10도)
            float normalizedDoa = (doa > 180) ? doa - 360 : doa;
            // DoA는 이미 카메라 기준의 각도이므로, 정규화된 값을 그대로 반환합니다.
            return normalizedDoa;
        }

        /// <summary>
        /// Place a marker using the environment raycast
        /// 지정된 위치에 마커를 생성 -> 물체 위에 생성
        /// </summary>
        private bool PlaceMarkerUsingEnvironmentRaycast(Vector3? position, string className)
        {
            // Check if the position is valid
            if (!position.HasValue)
            {
                return false;
            }

            // Check if you spanwed the same object before
            var existMarker = false;
            foreach (var e in m_spwanedEntities)
            {
                // 두 종류의 마커를 모두 확인합니다.
                var defaultMarker = e.GetComponent<DetectionSpawnMarkerAnim>();
                var emojiMarker = e.GetComponent<EmojiMarker>();

                string markerLabel = "";
                if (defaultMarker != null) markerLabel = defaultMarker.GetYoloClassName();
                else if (emojiMarker != null) markerLabel = emojiMarker.GetSoundLabel();

                if (!string.IsNullOrEmpty(markerLabel))
                {
                    var dist = Vector3.Distance(e.transform.position, position.Value);
                    if (dist < m_spawnDistance && markerLabel == className)
                    {
                        existMarker = true;
                        break;
                    }
                }
            }

            if (!existMarker)
            {
                // spawn a visual marker
                GameObject prefabToSpawn = m_markerPrefabManager.GetPrefabForSoundLabel(className);
                var eMarker = Instantiate(prefabToSpawn);
                m_spwanedEntities.Add(eMarker);

                // Update marker transform with the real world transform
                eMarker.transform.SetPositionAndRotation(position.Value, Quaternion.identity);

                // 두 종류의 마커에 모두 이름을 설정합니다.
                var defaultMarkerToSet = eMarker.GetComponent<DetectionSpawnMarkerAnim>();
                var emojiMarkerToSet = eMarker.GetComponent<EmojiMarker>();
                if (defaultMarkerToSet != null) defaultMarkerToSet.SetYoloClassName(className);
                else if (emojiMarkerToSet != null) emojiMarkerToSet.SetSoundLabel(className);
            }

            return !existMarker;
        }

        #endregion

        #region Public Functions
        /// <summary>
        /// Pause the detection logic when the pause menu is active
        /// </summary>
        public void OnPause(bool pause)
        {
            m_isPaused = pause;
        }

        /// <summary>
        /// DetectionUiMenuManager에 의해 호출되어 객체 탐지를 시작합니다.
        /// </summary>
        public void StartDetection()
        {
            m_isStarted = true;
        }
        #endregion
    }
}