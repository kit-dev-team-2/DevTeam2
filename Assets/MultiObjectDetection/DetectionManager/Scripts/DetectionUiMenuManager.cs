// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [MetaCodeSample("PassthroughCameraApiSamples-MultiObjectDetection")]
    public class DetectionUiMenuManager : MonoBehaviour
    {
        [Header("Ui buttons")]
        [SerializeField] private OVRInput.RawButton m_actionButton = OVRInput.RawButton.A;

        [Header("Ui elements ref.")]
        [SerializeField] private GameObject m_loadingPanel;
        [SerializeField] private GameObject m_initialPanel;
        [SerializeField] private GameObject m_noPermissionPanel;
        [SerializeField] private Text m_labelInformation;
        [SerializeField] private AudioSource m_buttonSound;
        [SerializeField] private DetectionManager m_detectionManager;

        [Tooltip("자동 시작까지 대기할 시간")]
        [SerializeField] private float m_autoStartTime = 3.0f;

        public bool IsInputActive { get; set; } = false;

        public UnityEvent<bool> OnPause;

        private bool m_initialMenu;
        private OVRScreenFade _screenFade;

        // start menu
        private int m_objectsDetected = 0;
        private int m_objectsIdentified = 0;

        // pause menu
        public bool IsPaused { get; private set; } = true;

        #region Unity Functions
        private IEnumerator Start()
        {
            _screenFade = FindFirstObjectByType<OVRScreenFade>();
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);

            // Wait until Sentis model is loaded
            m_loadingPanel.SetActive(true);
            var sentisInference = FindFirstObjectByType<SentisInferenceRunManager>();
            while (!sentisInference.IsModelLoaded)
            {
                yield return null;
            }
            m_loadingPanel.SetActive(false);

            // Wait for permissions
            OnNoPermissionMenu();
            while (!OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.Scene) || !OVRPermissionsRequester.IsPermissionGranted(OVRPermissionsRequester.Permission.PassthroughCameraAccess))
            {
                yield return null;
            }

            if (_screenFade != null && _screenFade.currentAlpha > 0)
            {
                _screenFade.FadeIn();
            }

            OnInitialMenu();

            // 3초 동안 실제 시간 기준으로 대기합니다.
            yield return new WaitForSecondsRealtime(m_autoStartTime);

            // 3초 후, 시작 메뉴를 숨기고 탐지를 시작합니다.
            m_detectionManager.StartDetection();
            OnPauseMenu(false);
        }

        private void Update()
        {
        }
        #endregion

        #region Ui state: No permissions Menu
        private void OnNoPermissionMenu()
        {
            m_initialMenu = false;
            IsPaused = true;
            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(true);
        }
        #endregion

        #region Ui state: Initial Menu

        private void OnInitialMenu()
        {
            m_initialMenu = true;
            IsPaused = true;
            m_initialPanel.SetActive(true); // 초기 화면에 표시되는 패널 활성화
            m_noPermissionPanel.SetActive(false);
        }

        private void InitialMenuUpdate()
        {
            // 이 메서드는 더 이상 Update에서 호출되지 않습니다.
        }

        private void OnPauseMenu(bool visible)
        {
            m_initialMenu = false;
            IsPaused = visible;

            m_initialPanel.SetActive(false);
            m_noPermissionPanel.SetActive(false);

            OnPause?.Invoke(visible);
        }
        #endregion

        #region Ui state: detection information
        private void UpdateLabelInformation()   // 화면 하단에 뜨는 패널
        {
            m_labelInformation.text = $"Unity Sentis version: 2.1.3\nAI model: Yolo\nDetecting objects: {m_objectsDetected}\nObjects identified: {m_objectsIdentified}";
        }

        public void OnObjectsDetected(int objects)
        {
            m_objectsDetected = objects;
            UpdateLabelInformation();
        }

        public void OnObjectsIndentified(int objects)
        {
            if (objects < 0)
            {
                // reset the counter
                m_objectsIdentified = 0;
            }
            else
            {
                m_objectsIdentified += objects;
            }
            UpdateLabelInformation();
        }
        #endregion
    }
}
