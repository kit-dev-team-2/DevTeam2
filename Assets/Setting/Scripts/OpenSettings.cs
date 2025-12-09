using UnityEngine;
using UnityEngine.SceneManagement;
using PassthroughCameraSamples.StartScene;
using PassthroughCameraSamples.MultiObjectDetection;

public class OpenSettings : MonoBehaviour
{
    private bool _isPanelActive = false;
    private DebugUIBuilder _debugUI;

    public void OnPanelToggled(bool isActive)
    {
        _isPanelActive = isActive;

        // 포인터 on/off
        var pointer = FindObjectOfType<ControllerPointer>(true);
        if (pointer != null)
        {
            pointer.gameObject.SetActive(_isPanelActive);
        }

        // Detection on/off
        var detectionManager = FindObjectOfType<DetectionManager>();
        if (detectionManager != null)
        {
            detectionManager.OnPause(_isPanelActive);

            // 패널이 켜질 때만 기존 마커 제거
            if (_isPanelActive)
            {
                detectionManager.ClearAllDetectionVisuals();
            }
        }
    }      


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
                var settingsPanelController = FindObjectOfType<SettingsPanelController>(true);
                if (settingsPanelController != null)
                {
                    bool newActive = !settingsPanelController.gameObject.activeSelf;

                    // 패널 on/off
                    settingsPanelController.gameObject.SetActive(newActive);

                    // 상태 + 포인터 + Detection 전부 동기화
                    OnPanelToggled(newActive);
                }
            }
        }
    }

}
