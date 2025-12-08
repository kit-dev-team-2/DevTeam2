using UnityEngine;
using UnityEngine.SceneManagement;

public class MultiObjectOpenSettingsButton : MonoBehaviour
{
    // 이 함수는 버튼 OnClick에 직접 연결한다.
    public void OnClickOpenSettings()
    {
        SceneManager.LoadScene("SettingScene");
    }
}
