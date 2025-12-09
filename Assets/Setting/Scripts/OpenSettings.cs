using UnityEngine;
using UnityEngine.SceneManagement;
using PassthroughCameraSamples.StartScene;
using PassthroughCameraSamples.MultiObjectDetection;

public class OpenSettings : MonoBehaviour
{
    private bool _isPanelActive = false;

    // 컴포넌트들을 캐싱하기 위한 변수
    private ControllerPointer _pointer;
    private DetectionManager _detectionManager;
    private SettingsPanelController _settingsPanelController;

    public void OnPanelToggled(bool isActive)
    {
        _isPanelActive = isActive;

        // 포인터 on/off
        if (_pointer != null)
        {
            _pointer.gameObject.SetActive(_isPanelActive);
        }

        // Detection on/off
        if (_detectionManager != null)
        {
            _detectionManager.OnPause(_isPanelActive);

            // 패널이 켜질 때만 기존 마커 제거
            if (_isPanelActive)
            {
                _detectionManager.ClearAllDetectionVisuals();
            }
        }
    }


    private void Start()
    {
        // 씬 시작 시 필요한 컴포넌트들을 미리 찾아둡니다.
        _pointer = FindObjectOfType<ControllerPointer>(true);
        _detectionManager = FindObjectOfType<DetectionManager>(true);
        _settingsPanelController = FindObjectOfType<SettingsPanelController>(true);

        // 씬이 시작될 때, 컨트롤러 포인터가 비활성화된 상태로 시작하도록 보장합니다.
        if (_pointer != null)
        {
            _pointer.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (OVRInput.GetUp(OVRInput.Button.Start))
        {
            if (SceneManager.GetActiveScene().name == "MultiObjectDetection")
            {
                if (_settingsPanelController != null)
                {
                    bool newActive = !_settingsPanelController.gameObject.activeSelf;

                    // 패널 on/off
                    _settingsPanelController.gameObject.SetActive(newActive);

                    // 상태 + 포인터 + Detection 전부 동기화
                    OnPanelToggled(newActive);
                }
            }
        }
    }

}
