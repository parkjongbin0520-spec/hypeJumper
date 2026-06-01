# hypeJumper — Unity/C# 포팅 셋업 가이드 (유니티 처음 기준)

Python/Pygame 프로토타입을 Unity로 재구성하기 위한 **첫 실행 환경 + 코어 이동 스크립트**.
이 폴더(`unity_port/Assets/Scripts/`)의 `.cs` 5개가 Phase 1 이동 코어다.

---

## 0. 핵심 원칙 (꼭 기억)
- **Rigidbody2D 쓰지 않는다.** 셀레스트식 정밀 점프 손맛은 유니티 물리(중력/마찰)와 안 맞음. 우리는 `transform` + 수동 AABB 충돌로 직접 제어한다. (`PlayerController.cs`가 이미 그렇게 짜여 있음)
- **y축 위=+y.** Pygame은 아래가 +y였지만 유니티는 위가 +y. 점프 속도는 양수, 중력은 빼기. (스크립트에 반영됨)
- **FixedUpdate 60Hz.** 튜닝값이 "프레임당@60fps" 기준이라 고정 스텝에서 매 스텝 그대로 적용해야 손맛이 같다.

---

## 1. 유니티 설치
1. **Unity Hub** 다운로드: https://unity.com/download → 설치
2. Hub 열고 `Installs → Install Editor` → **최신 LTS 버전**(예: 2022.3.x LTS 또는 6000.x LTS) 선택
   - 모듈: Windows Build Support 기본 포함이면 됨
3. 설치 완료까지 대기(몇 분~십몇 분)

## 2. 새 2D 프로젝트
1. Hub → `Projects → New project`
2. 템플릿 **2D (Core)** 선택, 프로젝트명 `hypeJumper`, 위치 지정 → Create
3. 에디터 열리면 좌하단 `Project` 창에 `Assets` 폴더 보임

## 3. 스크립트 넣기
1. `Project` 창에서 `Assets` 우클릭 → `Create → Folder` → 이름 `Scripts`
2. 이 레포의 `unity_port/Assets/Scripts/` 안 `.cs` 5개를 **Windows 탐색기에서 복사** →
   유니티 `Assets/Scripts` 폴더에 **붙여넣기** (또는 드래그 앤 드롭)
   - `GameSettings.cs`, `PlayerState.cs`, `InputBuffer.cs`, `PlayerInputState.cs`, `PlayerController.cs`
3. 유니티가 자동 컴파일. 우하단에 에러 없으면 성공 (콘솔창: `Window → General → Console`)

## 4. 물리 고정 스텝 설정 (중요)
1. 상단 메뉴 `Edit → Project Settings → Time`
2. **Fixed Timestep** 을 `0.01666667` (=1/60) 로 설정
   - (기본 0.02면 50Hz라 손맛이 미세하게 달라짐)

## 5. "Solid" 레이어 만들기
1. 상단 우측 `Layers` 드롭다운(또는 `Edit → Project Settings → Tags and Layers`)
2. 빈 User Layer 한 칸에 `Solid` 입력

## 6. 씬 구성 (바닥 + 플레이어)
### 바닥
1. `Hierarchy` 우클릭 → `2D Object → Sprites → Square` (이름 `Ground`)
2. Inspector에서 `Transform` Scale을 가로로 길게 (예: X=20, Y=1)
3. `Add Component → Box Collider 2D`
4. Inspector 상단 `Layer`를 **Solid** 로 변경
5. 위치 Y를 화면 아래쪽으로 (예: Y = -3)

### 플레이어
1. `Hierarchy` 우클릭 → `2D Object → Sprites → Square` (이름 `Player`)
2. Scale을 작게: X = `8/16 = 0.5`, Y = `16/16 = 1` (PPU 16 기준 히트박스)
3. `Add Component → Player Controller` (우리 스크립트)
4. PlayerController의 **Solid Mask** 필드 클릭 → **Solid** 체크
5. 위치 Y를 바닥 위쪽 (예: Y = 0)
6. (BoxCollider2D는 플레이어엔 **안 붙여도 됨** — 우리 스크립트가 크기를 자체 계산. 시각 확인용으로만 붙이려면 Size를 0.5×1로)

### 카메라
- 기본 `Main Camera` 그대로. 안 보이면 `Projection = Orthographic`, `Size`를 5~8 정도로.

## 7. 실행
- 상단 중앙 **▶ Play** 버튼 클릭
- 조작: **A/D 또는 ←/→** 이동, **C** 점프 (꾹 누르면 더 높이 = 가변 점프)
- 바닥에 서고, 좌우 이동/점프/낙하가 되면 성공
- 다시 ▶ 누르면 정지

> 안 움직이면 체크: ① Player의 Solid Mask에 Solid 들어갔나 ② Ground Layer가 Solid인가 ③ 콘솔 에러 없나 ④ Fixed Timestep 1/60.

## 8. 전체화면
- 개발 중: `Game` 탭 좌상단 해상도 드롭다운 옆 **Maximize On Play** 또는 빌드 후 전체화면.
- 코드로: 아무 스크립트에서 `Screen.fullScreenMode = FullScreenMode.FullScreenWindow;` 또는 F키 토글:
  ```csharp
  if (Input.GetKeyDown(KeyCode.F)) Screen.fullScreen = !Screen.fullScreen;
  ```
- 최종 배포: `File → Build Settings → Windows → Build` 후 exe 실행 시 해상도/전체화면 선택 가능.

---

## 9. 프로젝트 구조 설계 (판매가능 게임 골격)
지금은 Player 하나만. 앞으로 이렇게 키운다:
```
Assets/
├── Scripts/
│   ├── Core/          ← 엔진무관 로직 (InputBuffer, GameSettings, PlayerState)
│   ├── Player/        ← PlayerController + 대시/잡기 (Phase별 추가)
│   ├── Entities/      ← Enemy, NTT, RopeNTT, Spring, JumpPad ...
│   ├── Level/         ← 타일맵 로더, 체크포인트, 카메라
│   └── Game/          ← GameApp 상태머신, 씬 전환, UI
├── Scenes/
│   ├── Title.unity    ← 메뉴
│   ├── Tutorial.unity ← 이동/점프/대시/잡기 교육
│   └── Level_1_1.unity
├── Sprites/           ← 캐릭터 12프레임 모션셋, 타일셋, 배경, 잔디
├── Audio/
└── Prefabs/           ← Player, Enemy 등 재사용 오브젝트
```
- **씬 전환**: `SceneManager.LoadScene("Level_1_1")` 로 Title→Tutorial→Level 이동. (Unity 내장이라 Python의 GameApp 상태머신을 직접 안 짜도 됨)
- **타일맵**: 유니티 `Tilemap` 컴포넌트로 1-1 레벨을 에디터에서 그림. 충돌은 `Tilemap` 오브젝트에 우리 Solid 레이어만 맞춰주면 PlayerController가 그대로 막힘.
- **카메라 추적**: 맵을 화면보다 크게 만든 뒤 Cinemachine(무료 패키지)으로 플레이어 추적 → 코드 거의 0.
- **스프라이트/애니메이션**: `Sprites/`에 PNG 넣고 SpriteRenderer + 간단 프레임스왑 스크립트(상태별 12프레임). 잔디 흔들림/패럴랙스는 별도 데코 스크립트.

---

## 10. 다음 이식 순서 (Python → C#)
Phase 1 코어는 됐다. 같은 패턴으로 이어서:
1. **대시(Phase 2)** — `_start_dash/_update_dash`를 PlayerController에 Dash 상태로 추가 (중력 무시 등속).
2. **고급 무브(2.5)** — 슈퍼/하이퍼/월바운스 (`dash_jump_buffer` 창).
3. **잡기(3)** — Z 누름 윈도우+슬로우(`Time.timeScale`), 순간이동, 릴리즈 테크, 줄 NTT 진자.
   - ⚠️ 잡기는 **PLANNING.md 말고 WORK_LOG 2026-06-01 (3)~(8)** 의 재설계가 최신 사양.
4. 적/스프링/점프패드/투사체.

각 단계 Python 원본(`src/player.py` 등)을 옆에 두고 부호(y축)만 뒤집으며 옮기면 된다.
검증은 유니티 **EditMode 테스트**(NUnit)로 로직 클래스(InputBuffer 등)를 런타임 없이 단위 테스트 가능.
