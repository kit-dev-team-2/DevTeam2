using UnityEngine;
using UnityEngine.SceneManagement;
using PassthroughCameraSamples.StartScene;
using PassthroughCameraSamples.MultiObjectDetection;

public class OpenSettings : MonoBehaviour
{
    private bool _isPanelActive = false;
    private DebugUIBuilder _debugUI;

    private void Start()
    {
        // 씬이 시작될 때, 컨트롤러 포인터가 비활성화된 상태로 시작하도록 보장합니다.
        var pointer = FindObjectOfType<ControllerPointer>(true);
        if (pointer != null)
        {
            pointer.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.Start))
        {
            if (SceneManager.GetActiveScene().name == "MultiObjectDetection")
            {
                // SettingsPanelController를 찾습니다 
                var settingsPanelController = FindObjectOfType<SettingsPanelController>(true);
                if (settingsPanelController != null)
                {
                    // 패널의 현재 활성화 상태를 토글
                    _isPanelActive = !settingsPanelController.gameObject.activeSelf; // 상태 업데이트
                    settingsPanelController.gameObject.SetActive(_isPanelActive); // 패널 활성화/비활성화

                    // 메타 퀘스트 기본 포인터를 제어합니다.
                    var pointer = FindObjectOfType<ControllerPointer>(true);
                    if (pointer != null)
                    {
                        pointer.gameObject.SetActive(_isPanelActive);
                    }

                    var detectionManager = FindObjectOfType<DetectionManager>();
                    if (detectionManager != null)
                    {
                        // 패널이 활성화되면 Detection을 멈추고, 비활성화되면 다시 시작합니다.
                        detectionManager.OnPause(_isPanelActive);

                        // 패널이 활성화될 때, 기존에 생성된 모든 마커를 지웁니다.
                        if (_isPanelActive)
                        {
                            detectionManager.ClearAllDetectionVisuals();
                        }
                    }
                }
            }
        }
    }
}
