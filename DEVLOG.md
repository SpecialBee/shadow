# 개발 로그 — 그림자를 판 사나이 (ShadowSell)

---

## 2026-06-08

### [완료] 프로젝트 초기 세팅 확인
- Unity 6 (6000.4.1f1) + URP 2D + `activeInputHandler: 1` (New Input System 전용) 확인
- `Assets/ShadowSeller/` 폴더 + `ShadowSeller.asmdef` 생성
  - 참조: `Unity.InputSystem`, `Unity.TextMeshPro`
- 설계서(엑셀) 분석 → 게임 콘셉트·시스템 메모리에 저장

---

### [완료] 플레이어 이동
- `InputReader.cs` — `Keyboard.current` 기반 WASD / E(상호작용) / F(픽업) / R(리셋) 입력
  - Legacy `UnityEngine.Input` 사용 불가 프로젝트이므로 New Input System API 전용 사용
- `PlayerController.cs` — `Rigidbody2D.linearVelocity` 기반 top-down 이동 (gravityScale=0)
  - 스프라이트 미할당 시 하늘색 플레이스홀더 자동 생성
  - `FootPoint` 프로퍼티 예약 (추후 그림자·위험구역 판정용)

---

### [완료] 카메라 팔로우
- `CameraFollow.cs` — `Vector3.SmoothDamp` 기반 LateUpdate 팔로우
  - offset `(0, 0, -10)`, smoothTime `0.15f`
  - Main Camera에 부착, target = Player Transform

---

### [완료] TestObject (상호작용 테스트용 오브젝트)
- 주황색 사각형 SpriteRenderer
- "오브젝트" TextMeshPro 텍스트 (DungGeunMo SDF, 사용자 직접 조정)
- 목적: 오브젝트 상호작용 시스템 프로토타이핑

---

### [완료] 상호작용 시스템 — F키 픽업
- `IInteractable.cs` — `OnPickup(Transform)` / `OnDrop()` 인터페이스
- `CarryableObject.cs` — TestObject에 부착
  - CircleCollider2D trigger (radius=1.5u) 로 근접 감지
  - 픽업 시 플레이어 자식으로 부착 (localPos `(0.7, 0, 0)`)
  - 내려놓을 때 un-parent + collider 재활성화
- `PlayerInteraction.cs` — 플레이어에 부착
  - 근접 시 플레이어 머리 위 **"F 이동"** 힌트 TMP 자동 생성
  - 폰트: **DungGeunMo SDF** (에디터에서 자동 탐색 후 할당)
  - F 누르면 픽업 / 다시 F 누르면 내려놓기

---

## 앞으로 할 일

### 다음 우선순위

#### 1. GameLoopController (DAY 1-2 나머지)
- 매 틱 처리 순서를 관리하는 중앙 루프 골격
- 순서: 입력 → 플레이어 이동 → 광원 갱신 → NPC AI → 의심도 갱신 → 렌더링
- 현재 각 시스템이 독립 Update로 동작 → 틱 순서 불확정 문제 해소 필요

#### 2. 위험 구역 & 의심도 시스템 (DAY 3-4)
- `DangerZone.cs` — 광원 범위를 기반으로 위험 구역 정의 (Collider2D trigger)
- `SuspicionManager.cs` — 의심도(0~100) 전역 관리, 100 도달 시 패배 처리
- 플레이어 노출 상태 판정: EXPOSED > LIT > SHADOW > DARK
- 의심도 게이지 UI (임시 슬라이더)

#### 3. NPC 시야 & AI (DAY 5-6)
- `NPCController.cs` — 6 상태 머신 (Idle / Patrol / Alert / Chase / Search / Distracted)
- 부채꼴 시야 판정 (ViewAngle, ViewRange, 장애물 레이캐스트)
- NPC 시야 내 플레이어 노출 → 의심도 상승 연결

#### 4. 그림자 시스템 1차 (DAY 7 ★)
- `ShadowSystem.cs` — 오브젝트 그림자 점유 판정
- 그림자 품질 A~D (의심도 변화율 적용)
- `ExposureResolver.cs` — 플레이어 노출 상태 최종 판정

#### 5. 오브젝트 상호작용 확장 (DAY 8-9)
- TestObject 프레임워크 → 실제 오브젝트 종류 구현
  (Curtain / Chair / Chandelier / Pot / Crowd)
- 샹들리에 당기기, 커튼 뒤 숨기 등 개별 상호작용 로직

#### 6. 레벨 제작 (DAY 10-11)
- 튜토리얼 룸 + 무도회장 스테이지 레이아웃
- StageResetController

#### 7. 목표 & 승패 (DAY 12)
- `ObjectiveManager.cs` — 목표 오브젝트 상호작용 완료 판정
- `ExitTrigger.cs` — 탈출 지점 도달 시 승리 처리

#### 8. 밸런싱 & 폴리싱 (DAY 13-14)
- BalanceConfig ScriptableObject 인게임 대시보드
- UI Canvas, FX, 씬 전환 페이드, 사운드

---

*마지막 업데이트: 2026-06-09*

---

## 2026-06-08 (2차 세션)

### [완료] GameLoopController — 중앙 틱 루프
- **시간**: 오후 세션
- **방식**: 신규 스크립트 작성 + 기존 시스템 리팩토링
- `ITickable.cs` — `TickPhase` enum (Input=0, PlayerMove=10, Interaction=20, LightUpdate=30, NpcAI=40, SuspicionUpdate=50) + `ITickable` 인터페이스
- `GameLoopController.cs` — `[DefaultExecutionOrder(-100)]` 싱글턴. `SortedDictionary`로 위상 순서 보장. 각 시스템이 `Register(this)` 한 줄로 편입
- `InputReader`, `PlayerController`, `PlayerInteraction` — 기존 `Update()` 제거 후 `ITickable` 구현으로 전환
- 씬에 `GameLoopController` 오브젝트 배치

---

### [완료] 의심도 시스템 (SuspicionManager + DangerZone)
- **시간**: 오후 세션
- **방식**: 설계서 §Sheet2 기반 구현
- `GameTypes.cs` — `ExposureState` enum (Dark/ShadowA~D/Lit/ExposedSight/ExposedClose)
- `SuspicionManager.cs` — 전역 의심도 0~100 관리. 상태별 변화율 테이블(`Rates[]`). EXPOSED 진입 시 +15 즉시 스파이크(spikeArmed 1회). 100 도달 시 `OnGameOver` 이벤트 발행
- `PlayerExposureTracker.cs` — 플레이어에 부착. 위험구역 카운트 + NPC 위협 등록 → 우선순위(EXPOSED > LIT > DARK) 판정 후 SuspicionManager 갱신
- `DangerZone.cs` — BoxCollider2D trigger. 플레이어 진입/이탈 감지
- `SuspicionUI.cs` — Screen Space Canvas. fillAmount 게이지 + 구간별 색상(회색→주황→빨강)
- `ShadowSeller.asmdef`에 `Unity.ugui` 참조 추가
- 씬에 테스트용 반투명 노란 DangerZone 배치

---

### [완료] NPC AI (NPCController)
- **시간**: 오후 세션
- **방식**: 설계서 §Sheet5 기반 6상태 머신 구현
- `NpcKindData.cs` — ScriptableObject. viewAngle/viewRange/suspicionGainRate 등 모든 수치 Inspector 조정 가능
- `NPCController.cs` — 6상태 머신 (Idle→Patrol→Suspicious→Alert→Chase→Search)
  - 시야 판정: OverlapAngle + 레이캐스트 차단
  - 개별 의심 게이지 → 상태 전환 구동
  - 즉시 Chase 점프: closeRange(2u) 이내 노출 시 중간 상태 건너뜀
  - Chase 중 시야 유지 `arrestTime`(3s) → `SuspicionManager.TriggerArrest()` 호출
  - Alert/Chase 상태 진입 시 `PlayerExposureTracker.RegisterNpcThreat()` → 전역 ExposureState = ExposedSight
- `GameTypes.cs`에 `NpcKind`, `NpcState` enum 추가
- 씬에 `NPC_Guard` 배치 (WP1 ↔ WP2 순찰, NpcKindData_Guard.asset)

---

### [완료] VisionCone — 시야 시각화
- **시간**: 오후 세션
- **방식**: 런타임 메시 동적 생성
- `VisionCone.cs` — NPC 자식 오브젝트로 자동 생성. `LateUpdate`마다 24-segment 부채꼴 메시 재빌드
- 상태별 색상 자동 전환: Idle/Patrol(연노랑) → Suspicious(주황노랑) → Alert(진한 주황) → Chase(빨강)
- `NPCController`에 `FacingDir`, `KindData` public 프로퍼티 추가

---

### [완료] SpawnPoint — 플레이어 시작 위치 설정
- **시간**: 오후 세션
- **방식**: 마커 오브젝트 + PlayerController Start() 연동
- `SpawnPoint.cs` — Scene 뷰에서 초록 원+십자 기즈모로 표시. 드래그만 하면 시작 위치 변경
- `PlayerController.cs` — `[SerializeField] Transform spawnPoint` 추가. `Start()`에서 해당 위치로 Rigidbody2D 텔레포트
- 씬에 `SpawnPoint` 오브젝트 배치 및 PlayerController에 연결

---

### [완료] 그림자 시스템 (ShadowZone + ShadowSystem)
- **시간**: 오후 세션
- **방식**: 렌더 그림자(URP Light2D/ShadowCaster2D)와 판정용 그림자(Collider2D) 완전 분리 설계
- **핵심 원칙**: 시각적 그림자는 광원에 따라 동적으로 생성되지만, 판정용 그림자는 씬에 수동 배치. 광원이 정적이므로 허용되는 트레이드오프
- `ShadowZone.cs` — `[RequireComponent(Collider2D)]`. `ExposureState` 등급(ShadowA~D) 설정 가능. Awake에서 isTrigger 자동 설정. 등급별 기즈모 색상 (파랑→회색→갈색)
- `ShadowSystem.cs` — `ITickable(ShadowUpdate=45)`. `Physics2D.OverlapPoint(FootPoint, shadowLayer)`로 플레이어 발 위치의 ShadowZone 감지
  - 위장 안정성 임계 거리: ShadowA=항상 안정, B=3u, C=5u, D=∞(시야만으로 불안정)
  - NPC가 플레이어를 보고 있고(IsSeeingPlayer) 임계 거리 이내면 → ExposedSight로 격상
  - 안정 조건 충족 시 → 해당 등급(ShadowA~D)을 PlayerExposureTracker에 전달
- `ITickable.cs` — `ShadowUpdate=45` 페이즈 추가 (NpcAI=40 이후, SuspicionUpdate=50 이전)
- `PlayerExposureTracker.cs` — `SetShadow()` 연결. 우선순위: EXPOSED > SHADOW(A~D) > LIT > DARK
  - 그림자 안에 있으면 DangerZone이 있어도 그림자 등급 적용 (DangerZone 무시)
- "Shadow" 레이어(slot 8) 추가
- 씬에 `ShadowSystem` 오브젝트 배치 (shadowLayer=Shadow)
- 씬에 `ShadowZone_Test` 테스트용 배치 (-2,0,0): BoxCollider2D(3x3, trigger), Shadow 레이어, 파란 SpriteRenderer, grade=ShadowB

---

## 2026-06-09

### [완료] 그림자 시스템 전면 리팩토링 — 등급 제거 & 이진 판정

- **방식**: ShadowA~D 등급 체계 폐기 → 그림자 안/밖 이진 판정으로 단순화
- `GameTypes.cs` — `ExposureState` 개편
  - 제거: `ShadowA`, `ShadowB`, `ShadowC`, `ShadowD`
  - 추가: `Shadow` (단일 그림자 상태)
  - 최종 순서: `Dark=0 / Shadow=1 / Lit=2 / ExposedSight=3 / ExposedClose=4`
- `SuspicionManager.cs` — `Rates[]` 8개 → 5개로 축소
  - `Dark=-6f / Shadow=0f / Lit=+8f / ExposedSight=+20f / ExposedClose=+20f`
  - 그림자 안에서는 의심도 변화 없음(0f)
- `ShadowZone.cs` — grade 필드·`SetGrade()`·`Grade` 프로퍼티 완전 제거
  - 단순 마커 컴포넌트로 축소
  - `PolygonCollider2D` 기즈모 지원 추가 (이전 미구현 상태였음)
- `ShadowSystem.cs` — 대폭 단순화
  - `IsUnstable()` / `StabilityThresholds` / `_npcs` 캐시 제거
  - `Physics2D.OverlapPoint` (단일 히트 버그) → `Physics2D.OverlapPointNonAlloc` 교체
  - 커버리지 ≥ 3/5 인 ShadowZone 존재 시 `inShadow=true` 전달
- `PlayerExposureTracker.cs` — `SetShadow(ExposureState)` → `SetShadow(bool)` 단순화
  - `IsInShadow` public 프로퍼티 추가 (NPCController 참조용)
- `ChandelierObject.cs` — `SetGrade(ShadowD/B)` 전환 → `SetActive(false/true)` 전환
  - 흔들리는 동안: ShadowZone 비활성 (그림자 판정 없음)
  - 멈춘 동안: ShadowZone 활성 (그림자 판정 적용) → `stillDuration` 후 자동 복귀

---

### [완료] ShadowProjector — 동적 ShadowZone 자동 생성

- **방식**: ShadowProjector가 생성하는 `_Shadow` GameObject에 판정 콜라이더 자동 부착
- `ShadowProjector.cs` 확장
  - `_Shadow` GO에 `Rigidbody2D(Kinematic)` + `BoxCollider2D(trigger)` + `ShadowZone` 자동 추가
  - 레이어를 "Shadow"로 자동 설정 (레이어 미존재 시 조용히 스킵)
  - 콜라이더 크기: 원본 오브젝트의 `BoxCollider2D` 복사. 없으면 스프라이트 bounds 기준
  - `LateUpdate`에서 `_shadowRb.position = newPos` 로 물리 콜라이더 동기화
- **효과**: 오브젝트를 밀거나 당기면 시각적 그림자와 판정 영역이 함께 이동

---

### [완료] NPC 그림자 인식 — 그림자 안 플레이어 시야 차단

- **문제**: NPC가 그림자 안 플레이어를 발견해도 의심도가 상승하고 추격을 지속
- **원인 1**: `NPCController.CanSeePlayer()`가 그림자 여부를 확인하지 않음
- **원인 2**: `PlayerExposureTracker.Evaluate()` 우선순위가 `EXPOSED > SHADOW`라서
  NPC가 Alert/Chase 상태면 그림자 안에 있어도 ExposedSight로 판정
- `NPCController.cs` — `CanSeePlayer()` 최상단에 그림자 차단 추가
  - `_tracker.IsInShadow == true` 이면 즉시 `false` 반환
- `PlayerExposureTracker.cs` — 우선순위 변경: `SHADOW > EXPOSED > LIT > DARK`
  - 그림자 안에 있으면 NPC 위협 여부와 무관하게 Shadow 상태 적용
- **플레이 흐름**:
  1. NPC가 플레이어 발견 → Chase
  2. 플레이어가 그림자 진입 → 의심도 즉시 정지 / NPC `IsSeeingPlayer = false`
  3. NPC가 마지막 목격 위치로 `sightLoseDelay(2s)` 동안 계속 이동
  4. 2초 후 Search 상태 전환 → `searchDuration(5s)` 후 순찰 복귀

---

## 2026-06-10

### [완료] InteractableObject 버그 수정 & 성능 개선

- **pull overshoot 수정**: `pullDistance`가 플레이어-오브젝트 거리를 초과하지 않도록 clamp
  - `clampedDist = Mathf.Min(pullDistance, Mathf.Max(0f, distToPlayer - holdOffset))`
- **null carrier 방지**: `DoCarry()`에서 `_carrier = _nearbyPlayer ?? _player` 로 안전 할당
- **슬라이드 중 클릭 차단**: `Update()` 상단에 `if (_isSliding) return;` 추가
- **성능**: `Vector2.Distance` → `sqrMagnitude` 비교로 sqrt 연산 제거
- **EventSystem UI 가드**: UI 위 클릭 시 월드 오브젝트 오동작 방지
  - `EventSystem.current.IsPointerOverGameObject()` 체크 추가

---

### [완료] 상호작용 버튼 UI — 월드 스페이스 → Canvas UI 전환

- **문제**: 월드 좌표 기반 버튼(SpriteRenderer+TMP)이 해상도 및 Z-order 오류 유발
- **InteractionPanel.cs** 신규 작성 (`ShadowSeller.UI` 네임스페이스)
  - 싱글턴 패턴, UICanvas에 컴포넌트로 부착
  - `Show(List<(string, Action)>)` / `Hide()` API
  - 버튼 수에 따라 패널 높이를 자동 계산 (`BtnH=38, Spacing=4, PadY=10`)
  - 버튼 위치: Canvas 상 고정 위치 (런타임에 world→canvas 변환 없음)
  - `[SerializeField] TMP_FontAsset font` — Inspector에서 폰트 지정 가능
  - Overlay(반투명 배경) 클릭 시 패널 자동 닫힘
- **InteractableObject.cs** — 월드 스페이스 버튼 코드 전면 제거
  - `s_panelOwner` static 필드로 동시에 열린 패널 단 1개 보장
  - `ShowPanel()` → `InteractionPanel.Instance?.Show(CollectActions())`
  - `HidePanel()` → 본인이 오너일 때만 닫음

---

### [완료] UICanvas 단일 통합 & HUDPanel 구조 개편

- **기존 문제**: 씬에 UICanvas / SuspicionCanvas / InventoryCanvas / InteractionCanvas 분산 → 관리 복잡
- **통합 결과**: 단일 `UICanvas` (ScreenSpaceOverlay, sortOrder=100, ScaleWithScreenSize 1920×1080)
- **최종 계층 구조**:
  ```
  UICanvas
  ├── HUDPanel [Image, InventoryUI, SuspicionUI, HUDToggle]
  │     ├── Bar_BG [SuspicionUI] — 우상단 앵커
  │     ├── InventoryPanel [GridLayoutGroup] — 좌하단 앵커
  │     │     └── Slot_0..9 [Image] → Icon [Image]
  │     ├── MiniMapPanel
  │     ├── DialoguePanel
  │     └── Button  ← HUD 토글 버튼
  ├── Overlay [Image, Button]  ← 상호작용 패널 배경
  ├── InteractionButtonPanel [Image]
  │     └── ButtonContainer [VerticalLayoutGroup]
  ├── DefeatPanel
  └── VictoryPanel
  ```
- **EventSystem** — `InputSystemUIInputModule` 포함 (New Input System 전용) 씬에 배치

---

### [완료] 인벤토리 5행×2열 그리드 리디자인

- **InventoryPanel** — `GridLayoutGroup` 추가
  - cellSize `(60, 60)`, spacing `(6, 6)`, 2열(constraintCount=2)
  - 채우기 순서: 위→아래, 왼→오른쪽
- **InventoryUI.cs** — `Image[] slotIcons` 배열로 Inspector 연결 방식으로 개편
- **Slot_0..9** — 각 슬롯에 `Image` (배경) + 자식 `Icon [Image]` 구조

---

### [완료] HUDToggle — HUD 전체 ON/OFF 버튼

- **HUDToggle.cs** 신규 작성
  - HUDPanel에 부착, `[SerializeField] Button toggleButton` Inspector 연결
  - 버튼 클릭 시 HUDPanel의 자식 중 버튼 자신을 제외한 모든 오브젝트 SetActive 토글
  - 버튼 본체는 항상 표시

---

### [완료] 인벤토리 슬롯 클릭 → 아이템 드롭

- **InventoryManager.cs** 확장
  - `public static event System.Action<int> OnItemRemoved` 추가
  - `public ItemData? RemoveItem(int index)` — 슬롯 비우기 + 이벤트 발행
- **InventoryUI.cs** 전면 재작성
  - `Start()`에서 각 Slot GO에 `Button` 컴포넌트 런타임 추가
  - `OnItemAdded` / `OnItemRemoved` 이벤트 구독으로 아이콘 갱신
  - 슬롯 클릭 → `RemoveItem()` → `SpawnDroppedItem()`
  - `SpawnDroppedItem()`: `SpriteRenderer + BoxCollider2D + InteractableObject` 조합의 GO를 플레이어 이동 방향 1u 앞에 생성
- **InteractableObject.cs** — `SetupAsDroppedPickup(string)` 추가
  - 드롭된 아이템 GO에 `canInventory=true, itemName=...` 자동 설정

---

### [완료] 버그 수정 — 슬롯 버튼 미반응

- **원인**: EventSystem이 씬 저장 누락으로 소실 → UI 클릭 전혀 처리 안 됨
- **수정 1**: EventSystem + `InputSystemUIInputModule` 재생성 및 씬 저장
- **수정 2**: `InventoryUI.Start()`에서 `slotIcons[i].raycastTarget = false` 추가
  - 아이콘 Image가 부모 Button의 레이캐스트를 가로막는 문제 해소
