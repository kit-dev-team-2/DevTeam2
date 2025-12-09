using System;

[Serializable]
public class AppSettings
{
    // Emoji 관련
    public float emojiScale = 1.0f;
    public float emojiAlpha = 1.0f;

    // 모델 설정
    public float CONF_THRESH = 0.5f;
    public float DETECT_DURATION = 0.5f;
    public float PRE_BUFFER_DURATION = 0.2f;
    public float MIC_SAD_DB = 23.5f;
}
