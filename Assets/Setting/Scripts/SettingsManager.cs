using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    private const string PlayerPrefsKey = "AppSettings";

    public static SettingsManager Instance { get; private set; }

    public AppSettings Current { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public void Load()
    {
        string json = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);

        if (string.IsNullOrEmpty(json))
        {
            Current = new AppSettings(); // 기본값
        }
        else
        {
            Current = JsonUtility.FromJson<AppSettings>(json);
            if (Current == null)
            {
                Current = new AppSettings();
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
