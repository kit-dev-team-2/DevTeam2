// Copyright (c) Meta Platforms, Inc. and affiliates.
// Original Source code from Oculus Starter Samples (https://github.com/oculus-samples/Unity-StarterSamples)

using System;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using Meta.XR.Samples;
using UnityEngine.UI;
using UnityEngine;


namespace PassthroughCameraSamples.StartScene
{
    // Create menu of all scenes included in the build.
    [MetaCodeSample("PassthroughCameraApiSamples-StartScene")]
    public class StartMenu : MonoBehaviour
    {
        private OVRScreenFade _screenFade;

        [SerializeField] private QuestWsClient m_wsClient;

        private InputField m_ipAddressInput;
        private Button m_ipAddressButton;
        private Text m_statusText;
        private RectTransform m_connectButtonRect;
        private bool m_isConnected = false;

        private void Start()
        {
            if (m_wsClient != null)
            {
                m_wsClient.gameObject.SetActive(false);
            }
            SeverMenu();
        }

        private void SeverMenu()
        {
            var uiBuilder = DebugUIBuilder.Instance;

            _ = uiBuilder.AddLabel("개발 2팀", DebugUIBuilder.DEBUG_PANE_CENTER);
            _ = uiBuilder.AddDivider(DebugUIBuilder.DEBUG_PANE_CENTER);
            var statusRect = uiBuilder.AddLabel("버튼을 눌러 호스트 IP를 입력하고 연결을 누르세요", DebugUIBuilder.DEBUG_PANE_CENTER);
            m_statusText = statusRect.GetComponent<Text>();

            var ipButtonRect = uiBuilder.AddButton(PlayerPrefs.GetString("LastIPAddress", "192.168.1.10"), () => StartCoroutine(ShowSystemKeyboard()), -1, DebugUIBuilder.DEBUG_PANE_CENTER);
            m_ipAddressButton = ipButtonRect.GetComponent<Button>();

            m_connectButtonRect = uiBuilder.AddButton("서버 연결", OnConnectButtonPressed, -1, DebugUIBuilder.DEBUG_PANE_CENTER);

            uiBuilder.Show();
        }

        private IEnumerator ShowSystemKeyboard()
        {
            Text buttonText = m_ipAddressButton.GetComponentInChildren<Text>();
            TouchScreenKeyboard keyboard = TouchScreenKeyboard.Open(buttonText.text, TouchScreenKeyboardType.URL, false, false, false, false, "IP 주소를 입력하세요");

            while (keyboard != null && keyboard.status != TouchScreenKeyboard.Status.Done && keyboard.status != TouchScreenKeyboard.Status.Canceled)
            {
                if (keyboard.text != buttonText.text)
                {
                    buttonText.text = keyboard.text;
                }
                yield return null;
            }

            if (keyboard != null && keyboard.status == TouchScreenKeyboard.Status.Done)
            {
                buttonText.text = keyboard.text;
                PlayerPrefs.SetString("LastIPAddress", keyboard.text);
            }
        }

        private void OnConnectButtonPressed()
        {
            if (m_isConnected) return;

            StartCoroutine(ConnectToServer());
        }

        private IEnumerator ConnectToServer()
        {
            string ip = m_ipAddressButton.GetComponentInChildren<Text>().text;
            m_statusText.text = $"{ip}에 연결 중...";

            if (!m_wsClient.gameObject.activeInHierarchy)
            {
                m_wsClient.gameObject.SetActive(true);
            }

            // IP 주소를 명시적으로 전달해야 함 -> 안 하니까 오류 발생  
            m_wsClient.ConnectWithIP(ip);

            float timeout = 5.0f;
            while (!m_wsClient.IsConnected() && timeout > 0)
            {
                yield return new WaitForSeconds(0.1f);
                timeout -= 0.1f;
            }

            if (m_wsClient.IsConnected())
            {
                m_isConnected = true;
                m_statusText.text = "연결 성공!";

                m_ipAddressButton.gameObject.SetActive(false);

                // "서버 연결" 버튼을 "실행" 버튼으로 변경
                Button connectButton = m_connectButtonRect.GetComponent<Button>();
                Text connectButtonText = m_connectButtonRect.GetComponentInChildren<Text>();

                if (connectButton != null && connectButtonText != null)
                {
                    connectButtonText.text = "소리의 시각화";
                    connectButton.onClick.RemoveAllListeners();
                    connectButton.onClick.AddListener(() => LoadScene(1));
                }
            }
            else
            {
                m_statusText.text = "연결 실패. IP 주소를 확인하세요.";
                m_wsClient.gameObject.SetActive(false);
            }
        }

        // private void SceneMenu()
        // {
        //     var uiBuilder = DebugUIBuilder.Instance;

        //     var generalScenes = new List<Tuple<int, string>>();
        //     var n = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        //     for (var sceneIndex = 1; sceneIndex < n; ++sceneIndex)
        //     {
        //         var path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(sceneIndex);
        //         if (path.Contains("MultiObjectDetection"))
        //         {
        //             generalScenes.Add(new Tuple<int, string>(sceneIndex, path));    // MutiObjectDetection 씬도 여기에 포함됨
        //         }
        //     }

        //     if (generalScenes.Count > 0)
        //     {
        //         // _ = uiBuilder.AddDivider(DebugUIBuilder.DEBUG_PANE_CENTER);
        //         // _ = uiBuilder.AddLabel("개발 2팀", DebugUIBuilder.DEBUG_PANE_CENTER);
        //         foreach (var scene in generalScenes)
        //         {
        //             _ = uiBuilder.AddButton("실행", () => LoadScene(scene.Item1), -1, DebugUIBuilder.DEBUG_PANE_CENTER);
        //         }
        //     }

        //     uiBuilder.Show();
        // }

        private void LoadScene(int idx)
        {
            DebugUIBuilder.Instance.Hide();
            Debug.Log("Load scene: " + idx);
            StartCoroutine(FadeAndLoadScene(idx));
        }

        private IEnumerator FadeAndLoadScene(int idx)
        {
            if (_screenFade != null)
            {
                _screenFade.FadeOut();
                yield return new WaitForSeconds(_screenFade.fadeTime);
            }
            UnityEngine.SceneManagement.SceneManager.LoadScene(idx);
        }
    }
}
