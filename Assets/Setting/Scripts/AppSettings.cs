using System;

[Serializable]
public class AppSettings
{
    // ==== Emoji 관련 ====
    public float emojiScale = 1.0f;   // 이모지 전체 스케일 (기본 1.0)
    public float emojiAlpha = 1.0f;   // 이모지 알파 (0~1)

    // ==== 모델 설정 (서버로 전송할 값) ====
    public float CONF_THRESH = 0.7f;
    public float DETECT_DURATION = 0.8f;
    public float PRE_BUFFER_DURATION = 0.3f;
}
