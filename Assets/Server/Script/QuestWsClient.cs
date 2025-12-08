using UnityEngine;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

public class QuestWsClient : MonoBehaviour
{
    // 🔻 다른 클래스에서 쉽게 접근할 수 있도록 public static 인스턴스를 만듭니다.
    public static QuestWsClient Instance { get; private set; }
    private static QuestWsClient instance; // 이전 버전 호환성을 위해 유지

    [Header("WebSocket 서버 설정")]
    public string hostIP = "192.168.0.121";   // 호스트(PC) 서버 IP 주소
    [SerializeField] string portNum = "8080";   // 서버 포트 번호
    string serverAddress => $"ws://{hostIP}:{portNum}"; // WebSocket 서버 주소

    [Header("ack 주기 설정 (ms)")]
    [SerializeField] int ack_duration = 5000;

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
        public int doa;        // 방향 없으면 0 쓰거나 필드 빼도 됨
        public TagItem[] tags;
    }

    // 🔻 가장 최근에 받은 SoundResultMessage 전체를 저장할 변수
    private SoundResultMessage _latestSoundResult = null;

    // ====== 설정 ======

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

                await Task.Delay(ack_duration, cts.Token);
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
                    case "detection":   // 서버에서 분류 결과 보낼 때 type="detection"으로 변경
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

        // 🔻 받은 메시지(msg)를 그대로 _latestSoundResult 변수에 저장하기만 합니다.
        _latestSoundResult = msg;
        Debug.Log($"[SoundResult] Received new sound data. Storing message.");
    }

    /// <summary>
    /// 가장 최근에 받은 SoundResultMessage 전체를 반환하고, 변수를 비워 중복 처리를 방지합니다.
    /// </summary>
    public SoundResultMessage GetAndClearLatestSoundResult()
    {
        if (_latestSoundResult == null)
        {
            return null;
        }

        SoundResultMessage resultToReturn = _latestSoundResult;
        _latestSoundResult = null; // 값을 가져갔으므로 비워줍니다.
        return resultToReturn;
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

    void Awake()
    {
        // 싱글톤 + 씬 넘어가도 안 죽게
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        instance = this; // 이전 버전 호환성을 위해 유지
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Application.runInBackground = true;
    }

    async void OnDestroy()
    {
        await CloseConnection();
    }

    /// <summary>
    /// 지정된 IP 주소로 WebSocket 연결을 시작합니다.
    /// </summary>
    public async void ConnectWithIP(string ip)
    {
        // 이미 연결되어 있거나 연결 중이라면 먼저 기존 연결을 닫습니다.
        await CloseConnection();

        hostIP = ip; // 새로운 IP로 업데이트

        ws = new ClientWebSocket();
        cts = new CancellationTokenSource();

        try
        {
            Debug.Log($"[QuestWsClient] Connecting to: {serverAddress}");
            await ws.ConnectAsync(new Uri(serverAddress), cts.Token);
            Debug.Log("[QuestWsClient] Connected ✅");

            // 연결 성공 직후 hello 메시지 전송
            await SendJson(new HelloMsg
            {
                device = SystemInfo.deviceModel,
                t = NowMs()
            });

            // 주기적인 ack 및 수신 루프 시작
            _ = HeartbeatLoop();
            _ = ReceiveLoop();
        }
        catch (Exception e)
        {
            Debug.LogError($"[QuestWsClient] Connection failed ❌: {e.Message}");
        }
    }

    /// <summary>
    /// 현재 WebSocket 연결을 안전하게 닫습니다.
    /// </summary>
    public async Task CloseConnection()
    {
        cts?.Cancel();
        if (ws != null && (ws.State == WebSocketState.Open || ws.State == WebSocketState.Connecting))
        {
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing connection", CancellationToken.None);
        }
        ws?.Dispose();
        cts?.Dispose();
    }

    /// <summary>
    /// WebSocket이 현재 연결되어 있는지 확인합니다.
    /// </summary>
    /// <returns>연결되어 있으면 true, 그렇지 않으면 false</returns>
    public bool IsConnected()
    {
        return ws != null && ws.State == WebSocketState.Open;
    }
}
