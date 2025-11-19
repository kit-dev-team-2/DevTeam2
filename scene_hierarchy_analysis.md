
# 유니티 프로젝트 씬/오브젝트 분석 정리

올려준 프로젝트 기준으로 **두 개 씬(StartScene, MultiObjectDetection)** 을 열어서,  
각 씬의 **하이어라키에 있는 오브젝트들이 어떤 역할을 하고, 어떤 스크립트를 가지고 있는지**를 정리한 내용이다.

---

## 1. `StartScene` 씬

### 1-1. 최상위 레벨 하이어라키 구조

StartScene 씬 파일 기준으로 루트에 있는 오브젝트들은 대략 이렇게 구성돼 있다:

- `Directional Light`
- `'[BuildingBlock] Camera Rig'`
  - `TrackingSpace`
    - `CenterEyeAnchor`
    - `TrackerAnchor`
    - `RightHandAnchorDetached`
    - `LeftHandAnchor`
      - `LeftControllerInHandAnchor`
        - `LeftHandOnControllerAnchor`
      - `LeftControllerAnchor`
    - `RightEyeAnchor`
    - `LeftEyeAnchor`
    - `RightHandAnchor`
      - `RightControllerInHandAnchor`
        - `RightHandOnControllerAnchor`
      - `RightControllerAnchor`
    - `LeftHandAnchorDetached`
- `Server`
- `StartMenu`
- `'[BuildingBlock] Passthrough'`
- `Metadata`
- `CompositorLayerLoadingScreen`
  - `loadingText`
  - `overlay`
- (프리팹 인스턴스) `CanvasWithDebug`
- (프리팹 인스턴스) `ReturnToStartScene`

아래는 **역할 + 붙어 있는 스크립트**를 오브젝트별로 정리한 것이다.

---

### 1-2. 오브젝트별 역할 / 스크립트

#### ① `Directional Light`

- **역할**
  - 씬 전체 조명 담당하는 기본 Directional Light.
- **스크립트**
  - 커스텀 C# 스크립트 없음.
  - Unity 기본 `Light` 컴포넌트만 사용.

---

#### ② `'[BuildingBlock] Camera Rig'` + 자식들

- **역할 (전체)**
  - Meta/Oculus에서 제공하는 **XR 카메라 리그 프리팹**.
  - HMD 위치/회전 + 손/컨트롤러 위치를 모두 여기서 관리.

- **최상위 오브젝트**
  - `'[BuildingBlock] Camera Rig'`
    - XR 관련 OVR/Meta 컴포넌트들이 달려 있음 (OVRCameraRig 등).
    - **커스텀 스크립트 없음** (전부 SDK 제공 스크립트들).

- **자식 오브젝트들 역할**
  - `TrackingSpace`  
    - 리그의 기준좌표. 그 아래에 HMD/손/컨트롤러 앵커들이 전부 들어감.
  - `CenterEyeAnchor`  
    - HMD(헤드셋)의 중심 카메라.  
    - 다른 씬에서는 여기 밑에 UI 프리팹이 붙어서, **머리에 고정된 UI**를 보여줌.
  - `LeftEyeAnchor` / `RightEyeAnchor`  
    - 스테레오 렌더링을 위한 왼/오른쪽 눈 카메라.
  - `LeftHandAnchor` / `RightHandAnchor` (+ Detached 변형들)  
    - 컨트롤러/손 위치 기준점.
    - `XXControllerAnchor` : 실제 컨트롤러 모델이 붙는 위치  
    - `XXControllerInHandAnchor` : 손에 컨트롤러 쥔 상태 표현용  
    - `XXHandOnControllerAnchor` : 손 모델/레이저 포인터 등이 붙을 수 있는 위치

- **스크립트**
  - 이쪽은 전부 Meta/Oculus XR 관련 스크립트(OVR 계열)이고,
  - **프로젝트에서 직접 만든 C# 스크립트는 안 붙어 있음.**

---

#### ③ `Server`

- **역할**
  - 이 씬에서 유일하게 **서버 통신 담당하는 오브젝트**.
  - 퀘스트 → PC/서버로 **WebSocket 연결 유지 & JSON 메시지 송수신**.

- **붙어 있는 스크립트**
  - `QuestWsClient` (`Assets/Server/Script/QuestWsClient.cs`)
    - 싱글톤 (`QuestWsClient.Instance`)으로 동작.
    - `serverUrl` 필드로 접속할 WebSocket 주소 설정  
      - 예: `ws://127.0.0.1:8080` (adb reverse 사용 시).
    - 연결되면:
      - `HelloMsg` 구조체로 **type=hello, device 모델명, 타임스탬프** 전송.
      - 이후 주기적으로 `AckMsg` (`type=ack`)를 보내서 서버와 연결 상태 확인.
    - `SendJson<T>(T obj)` 같은 식으로 **다른 코드에서 임의의 JSON 구조를 쉽게 전송할 수 있게** 만들어둔 유틸.
    - 연결 재시도/에러 처리도 여기서 담당.

> 요약: **네트워크 레이어** 담당 오브젝트.  
> “씬 어디선가 결과를 서버로 보내고 싶다” → 결국 이 오브젝트의 `QuestWsClient.Instance`를 사용하게 된다.

---

#### ④ `StartMenu`

- **역할**
  - 빌드에 포함된 씬들을 읽어서, VR 안에서 **씬 선택 메뉴**를 만들어주는 오브젝트.
  - 시작했을 때 보이는 “씬 리스트 UI”를 담당.

- **붙어 있는 스크립트**
  - `StartMenu` (`Assets/StartScene/Scripts/StartMenu.cs`)
    - 빌드 세팅에 들어 있는 씬 목록을 가져옴.
    - VR UI 버튼으로 씬 리스트를 동적으로 생성.
    - 특정 버튼 누르면 `SceneManager.LoadScene()`으로 해당 씬 로드.
    - 필드:
      - `Overlay` / `Text`: OVROverlay를 이용해서 **로딩 텍스트/배경**을 표시.
      - `VrRig`: 카메라 리그 참조.

> 요약: **씬 네비게이션 UI의 중심 오브젝트**.

---

#### ⑤ `'[BuildingBlock] Passthrough'`

- **역할**
  - Meta의 **Passthrough 샘플 프리팹**.
  - 실제 현실 카메라를 배경으로 깔아주는 기능과 연동되는 오브젝트.

- **스크립트**
  - Passthrough 관련 Meta/Oculus 스크립트들이 붙어 있지만,  
    커스텀 C# 스크립트는 안 붙어 있음.

---

#### ⑥ `Metadata`

- **역할**
  - Meta 샘플에서 사용하는 **씬 메타데이터용 오브젝트**.
  - 샘플 이름, 설명, 버전 같은 것들을 담는 용도.

- **스크립트**
  - Meta 샘플용 스크립트 (SDK 쪽)만 있을 가능성이 크고,  
  - 직접 만든 스크립트는 없음.

---

#### ⑦ `CompositorLayerLoadingScreen` + `loadingText` / `overlay`

- **역할**
  - 씬 전환 시 보여주는 **로딩 화면 오버레이**.
  - `loadingText` : 로딩 중 텍스트 표시.
  - `overlay` : OVROverlay 기반의 전체 화면 이미지/색깔.

- **스크립트**
  - 두 자식(`loadingText`, `overlay`)에는 샘플/SDK 쪽 MonoBehaviour가 붙어 있고,  
    프로젝트 내 C# 파일과 매칭되는 것은 없음.

---

#### ⑧ 프리팹: `CanvasWithDebug` (씬 안에 프리팹 인스턴스)

- **역할**
  - 개발용 **디버그 UI 캔버스**.
  - 버튼/텍스트 같은 UI를 통해 런타임에 값을 찍거나 테스트할 때 사용하는 구조.

- **붙어 있는 스크립트**
  - `CanvasWithDebug` 오브젝트:
    - `DebugUIBuilder` (`Assets/StartScene/Scripts/DebugUIBuilder.cs`)
      - Meta 샘플 기반 디버그 UI 시스템.
      - 런타임에 버튼, 토글, 슬라이더 같은 것들을 코드로 쉽게 생성.

---

#### ⑨ 프리팹: `ReturnToStartScene` (StartScene에도 미리 존재)

- **역할**
  - **어디서든 StartScene으로 돌아가는 기능**을 담당하는 프리팹.

- **붙어 있는 스크립트**
  - 루트 오브젝트 `ReturnToStartScene`:
    - `ReturnToStartScene` (`Assets/StartScene/Scripts/ReturnToStartScene.cs`)
      - 특정 입력(버튼 등)을 받았을 때 `StartScene` 씬을 로드하는 역할.
      - MultiObjectDetection 씬에도 같은 프리팹이 들어 있어, 거기서도 StartScene으로 돌아갈 수 있게 사용됨.

---

## 2. `MultiObjectDetection` 씬

### 2-1. 기본 루트 하이어라키

씬 파일 기준 **루트에 직접 올라와 있는 오브젝트들**:

- `'[BuildingBlock] Passthrough'`
- `'[BuildingBlock] Camera Rig'`
  - `TrackingSpace`
    - `RightHandAnchor`
      - `RightControllerAnchor`
      - `RightControllerInHandAnchor`
        - `RightHandOnControllerAnchor`
    - `TrackerAnchor`
    - `RightHandAnchorDetached`
    - `LeftEyeAnchor`
    - `LeftHandAnchorDetached`
    - `LeftHandAnchor`
      - `LeftControllerAnchor`
      - `LeftControllerInHandAnchor`
        - `LeftHandOnControllerAnchor`
    - `CenterEyeAnchor`
    - `RightEyeAnchor`
- `Metadata`

여기까지는 StartScene과 거의 동일한 XR/패스스루 기본 구조다.

여기에 **추가로 프리팹 인스턴스가 여섯 개** 더 있다:

- `ReturnToStartScene` (프리팹)
- `PassthroughCameraAccessPrefab` (프리팹)
- `EnvironmentRaycastPrefab` (프리팹)
- `DetectionManagerPrefab` (프리팹)
- `SentisInferenceManagerPrefab` (프리팹)
- `DetectionUiMenuPrefab` (프리팹, `CenterEyeAnchor` 밑에 붙음)

---

### 2-2. 기본 오브젝트들

#### ① `'[BuildingBlock] Passthrough'`

- **역할**
  - StartScene과 마찬가지로 **현실 배경 패스스루를 띄워주는 기본 빌딩 블록**.

- **스크립트**
  - Meta/Oculus 제공 패스스루용 스크립트들.
  - 커스텀 C# 스크립트는 없음.

---

#### ② `'[BuildingBlock] Camera Rig'` + 자식

- **역할**
  - StartScene에서 설명한 XR 카메라 리그와 동일.
  - MultiObjectDetection 씬에서는 여기에 **Detection UI 프리팹**(`DetectionUiMenuPrefab`)이 추가로 붙는다.

- **스크립트**
  - XR 추적 관련 OVR 컴포넌트만.
  - 프로젝트에서 직접 만든 스크립트는 없음.

---

#### ③ `Metadata`

- StartScene과 동일한 샘플 메타데이터용 오브젝트.
- 커스텀 스크립트 없음.

---

### 2-3. 프리팹 인스턴스들 (핵심 파트)

#### (1) `ReturnToStartScene` 프리팹 인스턴스

- **위치**
  - MultiObjectDetection 씬 루트.

- **역할**
  - 이 씬에서 **StartScene으로 돌아갈 수 있는 기능** 제공.

- **붙어 있는 스크립트**
  - 루트 `ReturnToStartScene`:
    - `ReturnToStartScene`
      - StartScene 씬으로 돌아가기 위한 입력 처리/씬 전환 담당.

---

#### (2) `PassthroughCameraAccessPrefab`

- **위치**
  - 씬 루트.

- **역할**
  - 패스스루 카메라 접근/권한 관련 유틸 프리팹.
  - 실제 카메라 텍스처를 가져오고, 권한 요청 같은 걸 처리하는 용도.

- **붙어 있는 스크립트**
  - 프로젝트 내 C# 스크립트 매핑에는 안 보이는 걸로 봐서,  
    Meta 샘플/패키지 쪽 스크립트일 가능성이 크다.
  - 별도의 커스텀 스크립트는 없는 것으로 보면 된다.

---

#### (3) `EnvironmentRaycastPrefab`

- **위치**
  - 씬 루트.

- **역할**
  - 환경(raycast) 관련 권한/기능 담당.
  - 예를 들어, Scene 이해(벽/바닥 등)에 관련된 **spatial permission(`USE_SCENE`)**을 체크하고,  
    Raycast를 통해 환경과 상호작용하는 샘플 기능.

- **붙어 있는 스크립트**
  - 루트 `EnvironmentRaycastPrefab`:
    - `EnvironmentRayCastSampleManager`  
      (`Assets/MultiObjectDetection/EnvironmentRaycast/Scripts/EnvironmentRayCastSampleManager.cs`)
      - `com.oculus.permission.USE_SCENE` 권한 요청.
      - `EnvironmentRaycastManager`를 통해 실제 레이캐스트 처리.

---

#### (4) `DetectionManagerPrefab`

- **위치**
  - 씬 루트.

- **역할**
  - **인식 결과를 실제 월드에 그려주는 매니저**.
  - 카메라에서 넘어온 detection 결과(박스, 클래스 등)를 받아서:
    - 3D/월드 좌표에 **마커 프리팹 생성/업데이트/삭제**
    - UI 상태(일시정지, 텍스트 표시 등)와 연동.

- **붙어 있는 스크립트**
  - 루트 `DetectionManagerPrefab`:
    - `DetectionManager`  
      (`Assets/MultiObjectDetection/DetectionManager/Scripts/DetectionManager.cs`)
      - 패스스루 카메라 접근 (`PassthroughCameraAccess`와 연동).
      - 인식 결과(바운딩 박스 리스트)를 받아서,
        - 해당 위치에 마커 오브젝트 생성
        - 이미 있던 마커 업데이트
        - 사라진 대상의 마커 제거
      - 전체 인식 on/off, 일시정지 같은 기능 관리.

  - **프리팹 내부 자식들**
    - 마커 프리팹, 애니메이션, 텍스트 등에 예시 스크립트가 붙어 있음:
      - `DetectionSpawnMarkerAnim`  
        → 새로 잡힌 오브젝트 마커가 “툭” 튀어나오는 애니메이션 등.
      - `DetectionUiBlinkText`  
        → 깜빡이는 안내 텍스트.
      - `DetectionUiTextWritter`  
        → 타자치는 듯한 텍스트 출력 효과.

---

#### (5) `SentisInferenceManagerPrefab`

- **위치**
  - 씬 루트.

- **역할**
  - **Unity Sentis를 이용한 모델 추론 + 관련 UI 전체를 관리하는 핵심 오브젝트**.

- **붙어 있는 스크립트**  
  루트 `SentisInferenceManagerPrefab`에 세 개가 같이 붙어 있음:

1. `SentisInferenceRunManager`  
   (`Assets/MultiObjectDetection/SentisInference/Scripts/SentisInferenceRunManager.cs`)
   - Sentis 모델 로드 & 초기화.
   - `m_inputSize` (예: 640x640), `BackendType` (CPU/GPU) 설정.
   - 매 프레임(혹은 일정 주기마다):
     1. 패스스루 카메라에서 프레임 가져오기
     2. Sentis 모델에 넣어서 추론 실행
     3. 결과를 구조화된 형태(클래스, 확률, 박스)로 가공
     4. `DetectionManager` / UI 쪽으로 전달.

2. `SentisInferenceUiManager`
   - 인퍼런스 관련 UI 상태 관리:
     - 로딩/준비 완료 표시
     - 추론 중 여부
     - 오류/권한 이슈 등.

3. `SentisObjectDetectedUiManager`
   - 실제 감지된 오브젝트 리스트/요약을 UI에 표시.
   - 최근 감지된 클래스 이름/개수 같은 텍스트 업데이트.

> 요약: 이 프리팹 하나가 **“모델을 돌리고, 그 결과를 다른 매니저/UI에 뿌리는 엔진 역할”**을 한다.

---

#### (6) `DetectionUiMenuPrefab` (CenterEyeAnchor 밑)

- **위치**
  - `CenterEyeAnchor`의 자식으로 인스턴스 됨.  
  - 즉, **머리에 붙어 있는 UI(HUD)** 같은 개념.

- **프리팹 내부 주요 오브젝트/스크립트**

  - 루트 `DetectionUiMenuPrefab`
    - `DetectionUiMenuManager`  
      (`Assets/MultiObjectDetection/DetectionManager/Scripts/DetectionUiMenuManager.cs`)
      - A 버튼 등 특정 입력을 받아서:
        - “Controls / Help / Information” 같은 메뉴 열고 닫기.
        - 로딩 패널 on/off.
        - 인식 일시정지/재개 토글.

  - 자식 오브젝트 `Controls`
    - `DetectionUiBlinkText`
      - 안내 텍스트를 깜빡이게 만들어서 사용자의 시선을 끌어줌.

  - 자식 오브젝트 `Information` (두 개 존재)
    - 각각에 `DetectionUiTextWritter`
      - 문자열을 한 글자씩 타이핑하듯이 출력하는 효과.
      - 감지된 오브젝트 정보, 도움말 텍스트 등을 “타자 효과”로 띄워줌.

> 요약: 이 프리팹은 **인식 기능을 켜고 끄고 / 설명을 보여주는 VR HUD 메뉴**라고 보면 된다.

---

## 3. 씬별 요약

- **StartScene**
  - XR 리그 + 패스스루 + 디버그 UI + 씬 선택 메뉴 + 서버 WebSocket 클라이언트.
  - 직접 만든/커스텀 역할이 큰 오브젝트:
    - `Server` → `QuestWsClient`
    - `StartMenu` → `StartMenu`
    - `CanvasWithDebug` → `DebugUIBuilder`
    - `ReturnToStartScene` → `ReturnToStartScene`

- **MultiObjectDetection**
  - 기본 XR/패스스루/메타데이터 구조는 같고,
  - 여기에 **“인식 시스템” 관련 프리팹들**이 추가로 올라감:
    - `DetectionManagerPrefab` → `DetectionManager` (+ 마커/텍스트 애니메이션들)
    - `SentisInferenceManagerPrefab` →  
      `SentisInferenceRunManager`, `SentisInferenceUiManager`, `SentisObjectDetectedUiManager`
    - `DetectionUiMenuPrefab` → `DetectionUiMenuManager`, `DetectionUiBlinkText`, `DetectionUiTextWritter`
    - `EnvironmentRaycastPrefab` → `EnvironmentRayCastSampleManager`
    - `ReturnToStartScene` → StartScene 복귀용
    - `PassthroughCameraAccessPrefab` → 패스스루 카메라 접근 관련

이 문서를 그대로 레포에 `Docs/SceneHierarchy.md`처럼 넣어두면,  
나중에 팀원이 프로젝트 구조 파악할 때도 참고하기 좋다.
