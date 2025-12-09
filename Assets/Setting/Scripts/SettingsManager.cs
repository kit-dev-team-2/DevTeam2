using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    private const string PlayerPrefsKey = "AppSettings";

    public static SettingsManager Instance { get; private set; }

    public AppSettings Current { get; private set; }

    private void Awake()
    {
        // 싱글톤 보장
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);   // 여러 씬 쓸 거면 유지

        Load();
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);

        if (string.IsNullOrEmpty(json))
        {
            Current = new AppSettings();  // 무조건 기본값 생성
        }
        else
        {
            Current = JsonUtility.FromJson<AppSettings>(json);
            if (Current == null)
            {
                Current = new AppSettings();  // 파싱 실패해도 null 방지
            }
        }
    }

    public void Save()
    {
        if (Current == null)
        {
            Current = new AppSettings();
        }

        string json = JsonUtility.ToJson(Current);
        PlayerPrefs.SetString(PlayerPrefsKey, json);
        PlayerPrefs.Save();
    }
}
