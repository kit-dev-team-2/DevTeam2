using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

public class QuestWsClient : MonoBehaviour
{
    private static QuestWsClient instance;

    [Header("ws://127.0.0.1:8080  (adb reverse 쓰면 127.0.0.1)")]
    [SerializeField] string serverUrl = "ws://192.168.0.121:8080";
    [SerializeField] int ack_duration = 5;  //seconds

    ClientWebSocket ws;
    CancellationTokenSource cts;

    // ====== 메시지 타입들 ======

    [Serializable]
    public class HelloMsg
    {
        public string type = "hello";
        public string device;
        public long t;
    }

    // ★ 주기적인 ack용
    [Serializable]
    public class AckMsg
    {
        public string type = "ack";
        public long t;
    }

    // ★ 타입 구분용(서버 JSON에 type 필드가 있다고 가정)
    [Serializable]
    public class MsgTypeWrapper
    {
        public string type;
    }

    // ★ 음성 분류 결과 JSON 구조 (서버에서 이렇게 보내게 맞추면 됨)
    [Serializable]
    public class TagItem
    {
        public string label;
        public float score;
    }

    [Serializable]
    public class SoundResultMessage
    {
        public string type;      // "inference" 같은 값으로 맞춰두면 좋음
        public string timestamp; // "17:16:31" 같은 문자열
        public float doa;        // 방향 없으면 0 쓰거나 필드 빼도 됨
        public TagItem[] tags;
    }

    // ====== 설정 ======

    async void Start()
    {
        Application.runInBackground = true;
        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();

        try
        {
            Debug.Log($"WS connecting: {serverUrl}");
            await ws.ConnectAsync(new Uri(serverUrl), cts.Token);
            Debug.Log("WS connected ✅");

            // ✅ 연결 성공 직후 hello 전송
            await SendJson(new HelloMsg
            {
                device = SystemInfo.deviceModel,
                t = NowMs()
            });

            // ✅ 주기적인 ack 시작
            _ = HeartbeatLoop();

            // ✅ 수신 루프 시작
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"WS connect fail ❌ : {e.Message}");
        }
    }

    // ====== 주기적인 ack 루프 ======
    async Task HeartbeatLoop()
    {
        try
        {
            while (ws != null && ws.State == WebSocketState.Open && !cts.IsCancellationRequested)
            {
                var ack = new AckMsg
                {
                    t = NowMs()
                };

                await SendJson(ack);
                // Debug.Log("[WS] sent ack");

                await Task.Delay(ack_duration * 1000, cts.Token);
            }
        }
        catch (TaskCanceledException)
        {
            // 종료 시 자연스럽게 끝나는 경우
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WS] Heartbeat error: {e.Message}");
        }
    }

    async Task ReceiveLoop()
    {
        var buf = new byte[64 * 1024];
        try
        {
            while (ws != null && ws.State == WebSocketState.Open)
            {
                var res = await ws.ReceiveAsync(new ArraySegment<byte>(buf), cts.Token);
                if (res.MessageType == WebSocketMessageType.Close) break;

                var msg = Encoding.UTF8.GetString(buf, 0, res.Count);
                // Debug.Log($"WS RX raw: {msg}");

                HandleServerMessage(msg);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WS RX error: {e.Message}");
        }
    }

    // ====== 여기서 서버 JSON 분기 처리 ======
    void HandleServerMessage(string json)
    {
        try
        {
            // 1) 우선 type만 꺼내보기
            var typeWrap = JsonUtility.FromJson<MsgTypeWrapper>(json);

            if (typeWrap != null && !string.IsNullOrEmpty(typeWrap.type))
            {
                switch (typeWrap.type)
                {
                    case "inference":   // 서버에서 분류 결과 보낼 때 type="inference"로 맞춘다고 가정
                        var res = JsonUtility.FromJson<SoundResultMessage>(json);
                        OnSoundResult(res);
                        break;

                    case "ack":
                        // 서버가 보내는 ack가 있다면 여기서 처리
                        Debug.Log("[WS] server ack: " + json);
                        break;

                    case "hello":
                        Debug.Log("[WS] server hello: " + json);
                        break;

                    default:
                        Debug.Log("[WS] unknown type msg: " + json);
                        break;
                }
            }
            else
            {
                // type 없이 그냥 날아오는 JSON이면 여기서 처리
                Debug.Log("[WS] msg without type: " + json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[WS] JSON parse failed: {json}\n{e}");
        }
    }

    // 실제 게임/앱 로직으로 넘기는 함수
    void OnSoundResult(SoundResultMessage msg)
    {
        if (msg == null || msg.tags == null || msg.tags.Length == 0)
        {
            Debug.Log("[SoundResult] empty msg or no tags");
            return;
        }

        Debug.Log($"[SoundResult] timestamp={msg.timestamp}, doa={msg.doa}");
        foreach (var t in msg.tags)
        {
            Debug.Log($"  {t.label}: {t.score}");
        }

        // 🔻 여기서 점수 보고 어떤 소리인지 판단해서
        // UI 띄우거나, 이펙트 재생하거나, 다른 스크립트에 이벤트 넘기면 됨
        // ex) 최고 점수 태그 찾기:
        float bestScore = -1f;
        string bestLabel = "";
        foreach (var t in msg.tags)
        {
            if (t.score > bestScore)
            {
                bestScore = t.score;
                bestLabel = t.label;
            }
        }
        Debug.Log($"[SoundResult] TOP = {bestLabel} ({bestScore:F3})");
    }

    async Task SendJson<T>(T obj)
    {
        if (ws == null || ws.State != WebSocketState.Open) return;

        var json = JsonUtility.ToJson(obj);
        var seg = new ArraySegment<byte>(Encoding.UTF8.GetBytes(json));
        try
        {
            await ws.SendAsync(seg, WebSocketMessageType.Text, true, cts.Token);
            Debug.Log($"WS TX: {json}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"WS TX error: {e.Message}");
        }
    }

    long NowMs() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    async void OnDestroy()
    {
        try
        {
            cts?.Cancel();
            if (ws != null)
            {
                if (ws.State == WebSocketState.Open)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                ws.Dispose();
            }
        }
        catch { }
    }

    void Awake()
    {
        // 싱글톤 + 씬 넘어가도 안 죽게
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
