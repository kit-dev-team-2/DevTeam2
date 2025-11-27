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
        [SerializeField] private PassthroughCameraAccess m_cameraAccess;

        [Header("Controls configuration")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Matching configuration")]
        [Tooltip("소리의 방향과 객체의 방향 사이의 최대 허용 각도입니다. 이 각도보다 차이가 크면 매칭되지 않습니다.")]
        [SerializeField, Range(0, 90)] private float m_matchingAngleThreshold = 30.0f;

        [Header("Ui references")]
        [SerializeField] private DetectionUiMenuManager m_uiMenuManager;

        [Header("Placement configureation")]
        [SerializeField] private MarkerPrefabManager m_markerPrefabManager; // 이모지 프리팹 매니저
        [SerializeField] private EnvironmentRayCastSampleManager m_environmentRaycast;
        [SerializeField] private float m_spawnDistance = 0.25f; // 최소 거리
        [SerializeField] private AudioSource m_placeSound;

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
        private bool m_isSentisReady = false;
        private float m_delayPauseBackTime = 0;

        #region Unity Functions
        private void Awake() => OVRManager.display.RecenteredPose += CleanMarkersCallBack;

        private void OnDestroy() => OVRManager.display.RecenteredPose -= CleanMarkersCallBack;

        private IEnumerator Start()
        {
            // Wait until Sentis model is loaded
            var sentisInference = FindAnyObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_isSentisReady = true;
        }

        private void Update()
        {
            if (!m_isStarted)
            {
                // Manage the Initial Ui Menu
                if (m_cameraAccess.IsPlaying && m_isSentisReady)
                {
                    m_isStarted = true;
                }
            }
            else
            {
                // 매 프레임, 새로운 소리와 매칭되는 객체가 있는지 확인하고 마커를 생성합니다.
                // (SoundObjectMatcher가 내부적으로 QuestWsClient를 확인합니다)
                SpawnMarkersForMatchedObjects();

                // Cooldown for the A button after return from the pause menu
                m_delayPauseBackTime -= Time.deltaTime;
                if (m_delayPauseBackTime <= 0)
                {
                    m_delayPauseBackTime = 0;
                }
            }

            // Don't start Sentis inference if the app is paused or we don't have a camera image yet
            if (m_isPaused || !m_cameraAccess.IsPlaying)
            {
                if (m_isPaused)
                {
                    // Set the delay time for the A button to return from the pause menu
                    m_delayPauseBackTime = 0.1f;
                }
                return;
            }

            // Run a new inference when the current inference finishes
            if (!m_runInference.IsRunning())
            {
                m_runInference.RunInference(m_cameraAccess);
            }
        }
        #endregion

        #region Marker Functions
        /// <summary>
        /// Clean 3d markers when the tracking space is re-centered.
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
        /// OVRManager의 RecenteredPose 이벤트에 연결될 콜백 함수입니다.
        /// </summary>
        private void CleanMarkersCallBack()
        {
            ClearAllMarkers();
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

            // DoA를 3D 공간의 방향 벡터로 변환
            Vector3 soundDirection = GetDirectionFromDoa(matchResult.Doa);

            // 새로운 소리가 감지되었을 경우(성공/실패 무관) 기존 마커를 모두 지웁니다.
            if (matchResult.ResultType != SoundMatchResultType.NoNewSound)
            {
                ClearAllMarkers();
            }

            if (matchResult.ResultType == SoundMatchResultType.MatchFound)
            {
                // DoA 각도와 가장 일치하는 위치의 객체를 찾습니다.
                // out 키워드를 사용하여 bestMatchedObject를 전달하고, 성공 여부를 bool로 받습니다.
                if (FindBestObjectForDoa(soundDirection, matchResult, out var bestMatchedObject))
                {
                    // 최종 선택된 하나의 객체에만 마커를 생성합니다.
                    if (PlaceMarkerUsingEnvironmentRaycast(bestMatchedObject.WorldPos, matchResult.SoundLabel))
                    {
                        count++;
                    }
                }
            }
            // NoObjectInView인 경우, DoA 방향에 마커를 생성
            else if (matchResult.ResultType == SoundMatchResultType.NoObjectInView)
            {
                // 마커 위치 설정
                var camera = FindFirstObjectByType<OVRCameraRig>().centerEyeAnchor;
                Vector3 markerPosition = camera.position + soundDirection * 0.5f; // 0.5미터 앞에 생성

                // Raycast 없이 마커 생성
                if (PlaceMarkerAtPosition(markerPosition, matchResult.SoundLabel))
                {
                    count++;
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
        /// DOA 값과 가장 가까운 화면상 위치의 객체를 찾습니다.
        /// </summary>
        private bool FindBestObjectForDoa(Vector3 soundDirection, SoundMatchResult matchResult, out SentisInferenceUiManager.BoundingBox bestObject)
        {
            float minDifference = float.MaxValue;
            bestObject = default; // bestObject를 기본값으로 초기화

            var camera = FindFirstObjectByType<OVRCameraRig>().centerEyeAnchor;

            // 모든 매칭된 객체 중에서, 소리 방향과 가장 가까운 방향에 있는 객체를 찾습니다.
            foreach (var obj in matchResult.MatchedObjects)
            {
                if (!obj.WorldPos.HasValue) continue; // 객체의 3D 위치가 없으면 건너뜁니다.

                // 카메라 위치에서 객체를 바라보는 방향 벡터를 계산합니다.
                Vector3 objectDirection = (obj.WorldPos.Value - camera.position).normalized;

                // 소리 방향 벡터와 객체 방향 벡터 사이의 각도 차이를 계산합니다.
                float difference = Vector3.Angle(soundDirection, objectDirection);

                if (difference < minDifference)
                {
                    minDifference = difference;
                    bestObject = obj;
                }
            }

            // 찾은 최소 각도 차이가 설정한 임계값보다 작거나 같을 때만 성공
            bool success = minDifference <= m_matchingAngleThreshold;
            if (!success) { Debug.Log($"[DetectionManager] Best match found, but angle difference ({minDifference}°) exceeds threshold ({m_matchingAngleThreshold}°). No match."); }
            return success;
        }

        /// <summary>
        /// DOA 각도를 3D 공간의 방향 벡터로 변환
        /// </summary>
        private Vector3 GetDirectionFromDoa(int doa)
        {
            var centerEye = FindFirstObjectByType<OVRCameraRig>().centerEyeAnchor;
            int sight = m_soundObjectMatcher.sight;

            // 1. DoA 각도를 -sight ~ +sight 범위로 정규화합니다. (예: 310도 -> -50도)
            float normalizedDoa = (doa > 180) ? doa - 360 : doa;

            // 2. 정규화된 DoA를 실제 시야각에 비례하는 각도로 변환합니다.
            //    (예: -50도 -> -25도, +50도 -> +25도, 만약 실제 시야각이 50도라면)
            float targetAngle = (normalizedDoa / sight) * (centerEye.GetComponent<Camera>().fieldOfView / 2.0f);

            // 3. 카메라의 정면 방향을 기준으로 계산된 targetAngle만큼 Y축 회전시켜 최종 방향을 계산합니다.
            Vector3 direction = Quaternion.AngleAxis(targetAngle, Vector3.up) * centerEye.forward;
            return direction;
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

        /// <summary>
        /// 지정된 위치에 마커를 생성합니다. (Raycast 없이)
        /// </summary>
        private bool PlaceMarkerAtPosition(Vector3 position, string label)
        {
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
                    var dist = Vector3.Distance(e.transform.position, position);
                    if (dist < m_spawnDistance && markerLabel == label)
                    {
                        existMarker = true;
                        break;
                    }
                }
            }

            if (!existMarker)
            {
                // spawn a visual marker
                GameObject prefabToSpawn = m_markerPrefabManager.GetPrefabForSoundLabel(label);
                var eMarker = Instantiate(prefabToSpawn);
                m_spwanedEntities.Add(eMarker);

                // Update marker transform with the calculated position
                eMarker.transform.SetPositionAndRotation(position, Quaternion.identity);

                // 두 종류의 마커에 모두 이름을 설정합니다.
                var defaultMarkerToSet = eMarker.GetComponent<DetectionSpawnMarkerAnim>();
                var emojiMarkerToSet = eMarker.GetComponent<EmojiMarker>();
                if (defaultMarkerToSet != null) defaultMarkerToSet.SetYoloClassName(label);
                else if (emojiMarkerToSet != null) emojiMarkerToSet.SetSoundLabel(label);
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
        #endregion
    }
}