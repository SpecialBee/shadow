# ShadowSeller 레벨 디자인 가이드

> 이 문서는 기획자가 Unity Inspector만으로 게임의 모든 기능을 설정할 수 있도록 작성된 가이드입니다.  
> 스크립트를 직접 수정할 필요 없이, 각 컴포넌트의 Inspector 항목을 채우는 것만으로 레벨을 구성할 수 있습니다.

---

## 목차

1. [플레이어](#1-플레이어)
2. [카메라](#2-카메라)
3. [NPC](#3-npc)
4. [빛과 그림자 시스템](#4-빛과-그림자-시스템)
5. [상호작용 오브젝트 (InteractableObject)](#5-상호작용-오브젝트-interactableobject)
6. [대화 시스템](#6-대화-시스템)
7. [체크포인트 & 저장](#7-체크포인트--저장)
8. [컷씬 시스템](#8-컷씬-시스템)
9. [프롤로그 시스템](#9-프롤로그-시스템)
10. [승리 / 패배 조건](#10-승리--패배-조건)
11. [UI 시스템](#11-ui-시스템)
12. [사운드 시스템](#12-사운드-시스템)
13. [씬 관리](#13-씬-관리)
14. [ScriptableObject 에셋 목록](#14-scriptableobject-에셋-목록)

---

## 1. 플레이어

### PlayerController
플레이어 이동을 담당합니다. 플레이어 GameObject에 붙어 있습니다.

| Inspector 항목 | 타입 | 설명 |
|---|---|---|
| Move Speed | float | 이동 속도. 기본값 4. 높을수록 빠름 |
| Body Renderer | SpriteRenderer | 플레이어 스프라이트 렌더러. 비워두면 자동 탐색 |
| Spawn Point | Transform | 씬 시작 시 플레이어가 배치될 위치. 비워두면 현재 위치에서 시작 |
| Footstep Interval | float | 걷기 소리가 울리는 간격(초). 기본값 0.38 |

**키 조작 (고정, 변경 불가)**
- `WASD` : 이동
- `E` : 상호작용 / 대화 다음 줄
- `F` : 들기 전용 픽업
- `R` : 리셋(개발용)

---

### InputReader
키보드 입력을 읽어 PlayerController 등에 전달하는 내부 컴포넌트입니다.  
**Inspector에서 설정할 항목 없음.** 플레이어 GameObject에 함께 붙어 있으면 됩니다.

---

### PlayerExposureTracker
플레이어가 그림자 속에 있는지, 빛에 노출됐는지, NPC에게 발각됐는지를 판정합니다.  
**Inspector에서 설정할 항목 없음.** 플레이어 GameObject에 붙어 있으면 자동으로 동작합니다.

**노출 상태 우선순위 (높은 순)**
1. **Shadow** — 그림자 안에 있음 → 의심도 감소
2. **ExposedSight** — NPC가 직접 시야로 보고 있음 → 의심도 급상승
3. **ExposedClose** — NPC가 Suspicious 상태로 근처에 있음 → 의심도 상승
4. **Lit** — 조명 범위 안에 있음 → 의심도 천천히 상승
5. **Dark** — 어두운 곳 → 의심도 변화 없음

---

## 2. 카메라

### CameraFollow
카메라가 플레이어를 부드럽게 따라가도록 합니다. 메인 카메라 GameObject에 붙입니다.

| Inspector 항목 | 타입 | 설명 |
|---|---|---|
| Target | Transform | 따라갈 대상. 플레이어 Transform을 연결 |
| Smooth Time | float | 카메라가 따라가는 부드러움. 낮을수록 빠르게 따라감. 기본값 0.15 |
| Offset | Vector3 | 카메라 위치 오프셋. Z는 반드시 -10 유지 (2D 카메라 필수) |

---

## 3. NPC

### NpcKindData (ScriptableObject)
NPC의 행동 패턴 수치를 저장하는 데이터 에셋입니다.  
**생성 방법:** Project 창 우클릭 → `Create > ShadowSell > NPC Kind Data`

| 항목 | 설명 |
|---|---|
| **View Angle** | 시야각 (도). 90 = 앞을 기준으로 좌우 각 45도 |
| **View Range** | 시야 거리 (월드 단위) |
| **Suspicion Gain Rate** | NPC 개인 의심도 상승 속도 (/초). 클수록 빠르게 Alert 진입 |
| **Suspicion Decay Rate** | 개인 의심도 감소 속도 (/초). 클수록 빠르게 안정 |
| **Alert Threshold** | 이 수치 이상이면 Alert 상태 진입 (0~100) |
| **Chase Threshold** | 이 수치 이상이면 Chase 상태 진입 (0~100) |
| **Patrol Speed** | 순찰 이동 속도 |
| **Chase Speed** | 추격 이동 속도 |
| **Arrest Time** | 체포까지 걸리는 시간 (초). 현재 충돌 즉시 체포 |
| **Sight Lose Delay** | 시야에서 사라진 후 몇 초 뒤에 Chase → Search 전환 |
| **Search Duration** | Search 상태 지속 시간 (초) |
| **Close Range** | 이 거리 이내에 플레이어가 있으면 ExposedClose 판정 |

---

### NPCController
NPC AI를 담당합니다. NPC GameObject에 붙입니다.

| Inspector 항목 | 타입 | 설명 |
|---|---|---|
| **NPC Type** | Guard / Civilian | Guard: 시야 기반 FSM AI / Civilian: 전역 경계 레벨 기반 반응 |
| **Data** | NpcKindData | 위에서 만든 ScriptableObject 에셋 연결 |
| **Patrol Points** | Transform[] | Guard가 순찰할 경유지 목록. 순서대로 이동 |
| **Speech Bubble** | SpeechBubble | NPC 말풍선 컴포넌트 연결 (같은 GameObject에 있으면 됨) |

**NPC 상태 설명**

| 상태 | 조건 | 행동 |
|---|---|---|
| Idle | 순찰 경로 없음 | 제자리 대기 |
| Patrol | 순찰 경로 있음 | 경유지 순서대로 이동 반복 |
| Suspicious | 개인 의심도 ≥ AlertThreshold | 이상한 낌새를 느끼며 플레이어 방향을 봄, 말풍선 "음...?" |
| Alert | 개인 의심도 ≥ ChaseThreshold | 플레이어 위치로 이동, 말풍선 "거기 누구야?!" |
| Chase | Alert 상태에서 플레이어 시야 내 포착 | 전력 추격, 전역 경계 레벨 포인트 +2 |
| Search | 시야를 잃은 후 | 마지막 위치 주변 수색, 말풍선 "어디 갔지..." |

**Civilian 전용**: 전역 경계 레벨(AlertManager.Level)에 따라 반응 강도 자동 조절됩니다.

---

### VisionCone
NPCController가 자동으로 자식 오브젝트에 붙입니다. 직접 배치할 필요 없습니다.  
시야각을 부채꼴 메시로 시각화합니다 (에디터에서만 보임).

**색상 의미**
- 노랑: Idle/Patrol
- 주황: Suspicious
- 짙은 주황: Alert
- 빨강: Chase

---

### SpeechBubble
NPC 위에 말풍선을 띄우는 컴포넌트입니다. NPC GameObject에 함께 붙입니다.

| Inspector 항목 | 설명 |
|---|---|
| Bubble Sprite | 말풍선 배경 이미지. 비워두면 흰 사각형 |
| X Offset / Y Offset | 말풍선 위치 조정. Y는 NPC 머리 위 높이 |
| Padding X / Y | 텍스트 주변 여백 |
| Max Width | 말풍선 최대 가로 폭 |
| Duration | 말풍선 표시 시간 (초) |
| Fade Time | 사라질 때 페이드 시간 (초) |
| Font | TMP 폰트 에셋. 비워두면 기본 폰트 |

---

### SpeechBubbleArea
특정 범위에 플레이어가 들어오면 자동으로 말풍선을 표시합니다.  
SpeechBubble이 같은 GameObject에 필요합니다.

| Inspector 항목 | 설명 |
|---|---|
| Trigger Radius | 플레이어 감지 반경 (월드 단위) |
| Bubble Text | 표시할 텍스트. 여러 줄 입력 가능 |

> **Gizmo**: 씬 뷰에서 초록 반투명 원으로 감지 범위가 표시됩니다.

---

## 4. 빛과 그림자 시스템

### LightSource
조명 영역을 정의합니다. 빛을 내는 오브젝트에 붙입니다.  
`CircleCollider2D`가 자동으로 필요합니다.

| Inspector 항목 | 설명 |
|---|---|
| Range | 조명 반경 (월드 단위). 이 범위 안에 있으면 Lit 판정 |
| Wall Layer | 벽으로 인식할 레이어. 설정 시 벽에 막히면 조명이 닿지 않음 |

> **중요**: Unity Light 2D는 별도로 수동 조절해야 합니다. 이 스크립트는 게임 판정만 담당합니다.  
> **Gizmo**: 씬 뷰에서 노란 원으로 범위가 표시됩니다.

---

### ShadowProjector
오브젝트 아래에 타원형 그림자를 자동 생성합니다. 그림자를 드리울 오브젝트에 붙입니다.  
`SpriteRenderer`가 같은 GameObject에 있어야 합니다.

| Inspector 항목 | 설명 |
|---|---|
| Shadow Extend | 그림자가 스프라이트 발 밖으로 얼마나 뻗을지 (월드 단위). 클수록 넓은 그림자 |
| Light Offset Strength | 광원 반대 방향으로 그림자가 얼마나 이동할지. 0이면 항상 발 중앙 |
| Detection Scale | 은신 판정 범위 배율. 1.0 = 그림자 비주얼과 동일, 0.5 = 절반 크기 |
| Shadow Alpha | 그림자 투명도 (0~1). 클수록 진함 |
| Create Hiding Zone | 체크 시 그림자 안에 들어가면 Shadow 판정(은신) 적용 |

> **동작**: 가장 가까운 LightSource를 자동으로 찾아 그림자를 계산합니다. 벽에 막히거나 조명이 없으면 그림자가 생성되지 않습니다.

---

### EllipseShadow
타원형 그림자 판정 영역을 직접 배치할 때 사용합니다.  
ShadowProjector를 쓰지 않고 수동으로 그림자 은신 구역을 지정할 때 사용합니다.

| Inspector 항목 | 설명 |
|---|---|
| Radius X | 타원의 가로 반경 (월드 단위) |
| Radius Y | 타원의 세로 반경 (월드 단위) |
| Create Visual | 체크 시 타원 그라데이션 비주얼 자동 생성 |
| Shadow Color | 비주얼 색상 및 투명도 |
| Sorting Order | 비주얼의 렌더링 순서 |

> **Gizmo**: 씬 뷰에서 파란 타원 테두리로 판정 범위가 표시됩니다.

---

### ShadowZone
콜라이더로 그림자 판정 구역을 수동 지정할 때 사용합니다.  
`BoxCollider2D` 또는 `CircleCollider2D`와 함께 사용하며, 콜라이더 범위 안이 그림자 영역이 됩니다.  
**Inspector에서 설정할 항목 없음.** 콜라이더 크기만 조절하면 됩니다.

---

## 5. 상호작용 오브젝트 (InteractableObject)

게임의 핵심 상호작용 컴포넌트입니다. 문, 조명, 아이템, NPC 대화, 밀기/당기기 등 모든 상호작용을 하나의 컴포넌트로 처리합니다.

**사용법**: 원하는 오브젝트에 `InteractableObject` 컴포넌트를 붙이고, 아래의 상호작용 종류 체크박스를 선택합니다. 체크한 항목에 맞는 설정 항목이 자동으로 나타납니다.

---

### 공통 설정

| Inspector 항목 | 설명 |
|---|---|
| 접근 감지 반경 | 이 거리 안에 플레이어가 있어야 상호작용 패널이 표시됨 |
| 하이라이트 색상 | 플레이어가 접근했을 때 오브젝트가 빛나는 색상 |
| 하이라이트 투명도 | 하이라이트 강도 (0~1) |
| 벽 레이어 | 시야 차단 판정에 사용할 레이어. 설정 안 하면 'wall' 태그로 자동 감지 |

---

### 들기 (`canCarry` 체크)
플레이어가 오브젝트를 집어서 들고 다닐 수 있습니다.

| Inspector 항목 | 설명 |
|---|---|
| 들기 거리 | 플레이어 앞 얼마 거리에 오브젝트를 들고 다닐지 |

> 들고 있을 때 그림자가 사라지고, 내려놓으면 다시 생깁니다.

---

### 밀기 (`canPush` 체크)
플레이어가 오브젝트를 밀어서 이동시킬 수 있습니다.

| Inspector 항목 | 설명 |
|---|---|
| 밀기 거리 | 한 번 밀 때 이동하는 거리 |
| 밀기 속도 | 밀리는 속도 |

---

### 당기기 (`canPull` 체크)
플레이어가 오브젝트를 당길 수 있습니다.

| Inspector 항목 | 설명 |
|---|---|
| 당기기 거리 | 한 번 당길 때 이동하는 거리 |
| 당기기 속도 | 당겨지는 속도 |

> 밀기와 당기기에는 씬 뷰에서 방향 화살표(↑↓←→)가 자동 표시됩니다.

---

### 문 (`isDoor` 체크)
열고 닫을 수 있는 문입니다.

| Inspector 항목 | 설명 |
|---|---|
| **콜라이더 / 렌더러 자동 설정** 버튼 | 클릭하면 같은 GameObject의 Collider2D, SpriteRenderer를 자동으로 채워줌 |
| 문 콜라이더 | 닫혔을 때 막히는 콜라이더. 비워두면 플레이 시 자동 감지 |
| 문 렌더러 | 문 스프라이트를 표시하는 SpriteRenderer. 비워두면 자동 감지 |
| 열린 스프라이트 | 문이 열렸을 때 표시할 스프라이트 |
| 열린 상태 Order in Layer | 문이 열렸을 때의 렌더링 순서 |
| 닫힌 스프라이트 | 문이 닫혔을 때 표시할 스프라이트 |
| 닫힌 상태 Order in Layer | 문이 닫혔을 때의 렌더링 순서 |
| 시작 시 열려있음 | 체크하면 씬 시작 시 열린 상태로 시작 |

> 스프라이트를 둘 다 설정하지 않으면, 문이 열릴 때 렌더러가 숨겨집니다.

---

### 조명 켜기/끄기 (`canToggleLight` 체크)
스위치처럼 조명을 제어합니다.

| Inspector 항목 | 설명 |
|---|---|
| 제어할 조명 | 켜고 끌 LightSource 컴포넌트 목록. 여러 개 연결 가능 |

---

### 줍기 (`canInventory` 체크)
플레이어가 아이템을 주워 인벤토리에 넣습니다. 한 번 주우면 오브젝트가 씬에서 사라집니다.

| Inspector 항목 | 설명 |
|---|---|
| 아이템 이름 | 인벤토리에 표시될 이름. 비워두면 오브젝트 이름 사용 |

> 스프라이트는 SpriteRenderer에서 자동으로 가져옵니다.

---

### 확인하기 (`canExamine` 체크)
클릭하면 이미지를 전체 화면으로 표시합니다. 메모, 편지, 그림 등에 활용합니다.

| Inspector 항목 | 설명 |
|---|---|
| 확인 이미지 (Sprite) | 확인하기 팝업에 표시할 스프라이트 |

---

### NPC 대화 (`canTalk` 체크)
플레이어가 NPC와 대화합니다.

| Inspector 항목 | 설명 |
|---|---|
| 대화 데이터 | DialogueData ScriptableObject 연결 (아래 [대화 시스템](#6-대화-시스템) 참조) |
| 말풍선 | NPC의 SpeechBubble 컴포넌트. 대화 시작 시 말풍선이 숨겨짐 |

**아이템 지급 설정** (대화 후 아이템을 줄 때)

| Inspector 항목 | 설명 |
|---|---|
| 대화 후 아이템 지급 | 체크하면 대화 완료 후 아이템 지급 |
| 지급할 아이템 이름 | 인벤토리에 들어갈 아이템 이름 |
| 지급할 아이템 아이콘 | 인벤토리에 표시될 스프라이트 |
| 한 번만 지급 | 체크하면 처음 대화 시에만 지급. 이후 재대화 시 다른 대화 재생 |
| 지급 후 대화 | 이미 아이템을 준 NPC와 다시 대화할 때 재생될 DialogueData. 비워두면 원래 대화 반복 |

---

### 목표 NPC (`isTarget` 체크)
퀘스트 목표 NPC입니다. 대화 완료 시 `ObjectiveManager`를 통해 목표 완료 처리됩니다.

| Inspector 항목 | 설명 |
|---|---|
| 대화 데이터 | 목표 달성 시 재생할 DialogueData |

---

## 6. 대화 시스템

### DialogueData (ScriptableObject)
대화 내용을 저장하는 에셋입니다.

**생성 방법:** Project 창 우클릭 → `Create > ShadowSeller > DialogueData`

| 항목 | 설명 |
|---|---|
| Lines | 대화 줄 목록. 순서대로 출력됨 |

각 Line은 다음 항목을 가집니다:
- **Speaker Name**: 화자 이름 (대화창 상단에 표시)
- **Text**: 대화 내용

**대화 조작 (플레이 중)**
- `E` 또는 `Space`: 다음 줄로 넘기기 (타이핑 중이면 즉시 완성)

---

### DialogueSystem
대화창 UI를 담당하는 싱글톤입니다. HUD Canvas 하위에 배치합니다.

| Inspector 항목 | 설명 |
|---|---|
| Name Text | 화자 이름을 표시할 TextMeshPro UI |
| Dialogue Text | 대화 내용을 표시할 TextMeshPro UI |
| Next Indicator | "다음" 표시 아이콘 오브젝트 (타이핑 완료 시 표시) |
| Type Speed | 한 글자씩 타이핑되는 속도 (초/글자). 낮을수록 빠름 |

---

## 7. 체크포인트 & 저장

### Checkpoint
플레이어가 밟으면 현재 상태를 저장하는 체크포인트입니다.  
`SpriteRenderer`가 같은 GameObject에 있어야 합니다.

| Inspector 항목 | 설명 |
|---|---|
| Radius | 플레이어 감지 반경 (월드 단위) |
| Inactive Color | 아직 밟지 않은 체크포인트 색상 |
| Active Color | 밟은 체크포인트 색상 |

> 한 번 밟은 체크포인트는 씬이 끝날 때까지 재저장하지 않습니다.  
> **Gizmo**: 활성화 상태에 따라 색상이 바뀌는 원으로 표시됩니다.

---

### CheckpointManager
저장/불러오기를 총괄하는 싱글톤입니다. DontDestroyOnLoad로 씬 전환 시에도 유지됩니다.  
씬 하나에 하나만 배치하면 됩니다 (보통 MainMenu나 Stage씬에 배치).  
**Inspector에서 설정할 항목 없음.**

**저장 내용**: 현재 씬 이름 + 플레이어 위치 (PlayerPrefs에 저장, 게임 재시작 후에도 유지)

---

## 8. 컷씬 시스템

### CutsceneDirector
컷씬 재생을 총괄하는 싱글톤입니다. 씬에 하나 배치합니다.  
**Inspector에서 설정할 항목 없음.** CutsceneTrigger가 이 컴포넌트를 자동으로 찾아 사용합니다.

---

### CutsceneTrigger
컷씬을 발동시키는 트리거입니다. 원하는 위치에 빈 오브젝트를 만들어 붙입니다.

| Inspector 항목 | 설명 |
|---|---|
| **Mode** | Zone: 플레이어가 영역에 진입 시 발동 / Interaction: E키 입력 시 발동 |
| One Shot | 체크 시 한 번만 발동 (기본 권장) |
| Approach Radius | Interaction 모드에서 E키가 유효한 감지 반경 |
| **Steps** | 컷씬 스텝 목록 (아래 참조) |
| Override Player Spawn | 컷씬 종료 후 플레이어 위치를 바꿀지 여부 |
| Spawn Point | 컷씬 종료 후 플레이어를 이동시킬 Transform |

> Zone 모드에서는 반드시 오브젝트에 `Collider2D`를 추가하고 `Is Trigger`를 체크해야 합니다.

---

### CutsceneStep (컷씬 스텝 설정)

Steps 배열에 스텝을 추가합니다. 각 스텝의 Type에 따라 다른 항목이 활성화됩니다.

| Type | 필요 항목 | 설명 |
|---|---|---|
| **Dialogue** | dialogue | DialogueData를 재생. E키로 진행 |
| **MovePlayer** | moveTo | 플레이어를 지정한 Transform 위치까지 걸어서 이동 |
| **MoveObject** | objectToMove, moveTo, moveDuration, smoothMove | 지정 오브젝트를 목표 위치로 이동 |
| **MoveCamera** | cameraTarget, cameraDuration, cameraFollowAfter | 카메라를 목표 위치로 이동. `cameraFollowAfter` 체크 시 완료 후 다시 플레이어 추적 |
| **Wait** | waitSeconds | 지정한 초만큼 대기 |
| **Fade** | fadeOut, fadeDuration | 화면을 페이드인/아웃. `fadeOut` 체크 = 검게 / 해제 = 밝게 |

**Wait For Complete**: 각 스텝에 있는 옵션입니다.
- **체크 (기본)**: 이 스텝이 완료될 때까지 다음 스텝 대기
- **미체크**: 이 스텝과 다음 스텝을 동시에 실행

> 컷씬 시작 시 자동으로 레터박스(위아래 검정 바)가 나타나고, 종료 시 사라집니다.

---

## 9. 프롤로그 시스템

### PrologueDirector
프롤로그 씬 전체 흐름을 제어합니다. 프롤로그 씬에 하나 배치합니다.

| Inspector 항목 | 설명 |
|---|---|
| **Chapters** | 챕터 목록. 인덱스 0부터 순서대로 실행 |
| Next Scene Name | 마지막 챕터 완료 후 로드할 씬 이름 |
| Fade Image | 전체 화면 검정 페이드용 Image UI |
| Sfx Source | PlaySound 스텝에서 사용할 AudioSource. 비워두면 카메라 위치에서 재생 |

**각 챕터 (ChapterData) 설정**

| 항목 | 설명 |
|---|---|
| Chapter Name | 챕터 이름 (개발 구분용) |
| Spawn Point | 이 챕터에서 플레이어가 시작할 위치 |
| Camera Target | 카메라가 이동할 위치 |
| Camera Follows Player | 체크 시 카메라가 플레이어를 따라감 / 미체크 시 Camera Target 고정 |
| Black Screen Dialogue | 페이드 아웃 후 검정 화면에서 재생할 대화 (나레이션 등) |
| Intro Steps | 페이드 인 후 순서대로 실행할 스텝 목록 |
| Auto Advance | 체크 시 스텝 완료 후 자동으로 다음 챕터 / 미체크 시 PrologueTrigger 대기 |
| Fade Duration | 페이드 인/아웃 소요 시간 (초) |

**스텝 타입 (PrologueStepType)**

| Type | 항목 | 설명 |
|---|---|---|
| Dialogue | dialogue | DialogueData 재생 |
| MovePlayer | moveTarget | 플레이어를 걸어서 해당 위치로 이동 |
| PlaySound | sound | AudioClip 1회 재생 |
| Wait | waitSeconds | 지정 초 대기 |

---

### PrologueTrigger
프롤로그 내에서 챕터 진행을 트리거합니다.  
`autoAdvance = false`인 챕터에서 사용합니다.

| Inspector 항목 | 설명 |
|---|---|
| **Mode** | Interaction: 근접 후 Space키 / Zone: 트리거 영역 진입 / Dialogue: 대화 완료 후 진행 |
| Approach Radius | Interaction/Dialogue 모드에서 반응 거리 |
| Dialogue Data | Zone/Interaction: 선택 대화 / Dialogue 모드: 필수 대화 |
| Hint Text | 근접 시 표시할 TextMeshPro UI (힌트 문구 표시) |
| Hint Message | 힌트 텍스트 내용. 기본값 "E — 상호작용" |

---

## 10. 승리 / 패배 조건

### ObjectiveManager
게임의 목표 달성 여부를 관리합니다. 씬에 하나 배치합니다.  
**Inspector에서 설정할 항목 없음.**

**목표 완료 방법 (둘 중 하나)**
1. `isTarget`이 체크된 InteractableObject와 대화 완료
2. ExitTrigger에 진입 (`ObjectiveManager.IsComplete`가 true일 때만 동작)

---

### ExitTrigger
목표 완료 후 플레이어가 진입하면 승리 처리합니다.  
`Collider2D`가 자동으로 트리거로 설정됩니다.  
**Inspector에서 설정할 항목 없음.** Collider2D 크기만 조절하면 됩니다.

> **Gizmo**: 목표 미완료 = 회색, 완료 = 녹색으로 표시됩니다.

---

### AlertManager
전역 경계 레벨(1~4)을 관리합니다. 씬에 하나 배치합니다.  
**Inspector에서 설정할 항목 없음.**

**경계 레벨 상승 조건**
- Guard가 Chase 상태 진입 시: +2 포인트
- Civilian이 고위협 반응 시: +1 포인트
- 전역 의심도 70 이상 10초 지속 시: +1 포인트

**경계 레벨별 효과**
- 레벨 1 (0~1포인트): 기본 상태
- 레벨 2 (2~3포인트): Guard 이동속도 1.3배, BGM 긴장감 증가
- 레벨 3 (4~6포인트): Guard 이동속도 1.3배, Civilian 시야 1.2배
- 레벨 4 (7포인트+): Guard 이동속도 1.6배, 의심도 자연 감소 80% 차단

---

### SuspicionManager
전역 의심도(0~100)를 관리합니다. 씬에 하나 배치합니다.

| Inspector 항목 | 설명 |
|---|---|
| Rate Dark | 어두운 곳에서 의심도 변화 속도 (/초). 기본 0 |
| Rate Shadow | 그림자 안에서 의심도 변화 속도. 기본 -6 (감소) |
| Rate Lit | 조명 안에서 의심도 변화 속도. 기본 +8 |
| Rate Exposed Sight | NPC 시야에 포착됐을 때. 기본 +20 |
| Rate Exposed Close | NPC가 Suspicious 상태로 근접. 기본 +5 |

**게임오버 조건**
- 의심도 100 도달 → "의심도가 최대에 달했습니다"
- NPC와 충돌(Chase 상태) → "NPC에게 발각되었습니다"

---

### GameOverUI
패배/승리 패널을 표시합니다. HUD Canvas 하위에 배치합니다.

| Inspector 항목 | 설명 |
|---|---|
| Defeat Panel | 패배 시 표시할 GameObject |
| Victory Panel | 승리 시 표시할 GameObject |
| Defeat Restart Btn | 패배 화면의 재시작 버튼 |
| Continue Btn | 패배 화면의 이어하기 버튼 (체크포인트가 있을 때만 표시) |
| Victory Restart Btn | 승리 화면의 재시작 버튼 |
| Defeat Reason Text | 패배 이유를 표시할 TextMeshPro |

---

## 11. UI 시스템

### InteractionPanel
플레이어가 오브젝트에 접근했을 때 나타나는 상호작용 버튼 패널입니다.

| Inspector 항목 | 설명 |
|---|---|
| Carry Btn | "들기" 버튼 |
| Push Btn | "밀기" 버튼 |
| Pull Btn | "당기기" 버튼 |
| Door Btn | "열기/닫기" 버튼 |
| Light Btn | "켜기/끄기" 버튼 |
| Pickup Btn | "줍기" 버튼 |
| Talk Btn | "대화" 버튼 |
| Examine Btn | "확인하기" 버튼 |
| Active Alpha | 활성화된 버튼 투명도 (0~1) |
| Inactive Alpha | 비활성화된 버튼 투명도 (0~1) |

---

### InventoryUI
인벤토리 UI를 담당합니다. 슬롯 클릭 시 아이템을 바닥에 드롭합니다.

| Inspector 항목 | 설명 |
|---|---|
| Slot Icons | 각 슬롯의 Image 컴포넌트 배열 (최대 10개) |

---

### SuspicionUI
의심도 게이지를 표시합니다.

| Inspector 항목 | 설명 |
|---|---|
| Fill Bar | 채워지는 게이지 Image (Fill Amount 방식) |
| Value Label | 의심도 숫자를 표시할 TextMeshPro |

**게이지 색상**
- 회색 (0~39): 안전
- 주황 (40~69): 주의
- 빨강 (70~100): 위험

---

### AlertLevelUI
전역 경계 레벨 UI를 표시합니다.

| Inspector 항목 | 설명 |
|---|---|
| Level Icon | 레벨 색상을 나타낼 Image |
| Level Number | 레벨 숫자를 표시할 TextMeshPro |
| Notification Text | 레벨 상승 시 화면 중앙에 나타나는 알림 TextMeshPro |
| Notification Duration | 알림 표시 시간 (초) |
| Flash Overlay | 레벨 상승 시 붉게 번쩍이는 전체 화면 Image |
| Flash Duration | 플래시 지속 시간 (초) |

---

### ExaminePopup
"확인하기" 상호작용 시 이미지를 전체 화면으로 표시하는 팝업입니다.

| Inspector 항목 | 설명 |
|---|---|
| Overlay | 팝업 전체 루트 GameObject |
| Examine Image | 스프라이트를 표시할 Image |
| Close Btn | X 닫기 버튼 |

> `ESC` 키로도 닫을 수 있습니다.

---

### VignetteController
플레이어가 그림자 속에 있거나 경계 레벨이 높을 때 화면 가장자리를 어둡게 하는 효과입니다.  
**Inspector에서 설정할 항목 없음.** 자동으로 동작합니다.

---

### CinematicBars
컷씬 재생 중 위아래 검정 레터박스를 표시합니다. CutsceneDirector가 자동으로 제어합니다.

| Inspector 항목 | 설명 |
|---|---|
| Bar Height Ratio | 바 높이 (화면 높이 대비 비율). 0.12 = 12% |
| Anim Duration | 바가 나타나고 사라지는 애니메이션 시간 (초) |
| Hud Objects | 컷씬 중 숨길 HUD 오브젝트 목록 (대화창 제외) |

---

### HUDToggle
HUD 패널을 슬라이드하여 접고 펼치는 버튼입니다.

| Inspector 항목 | 설명 |
|---|---|
| Toggle Button | 토글 버튼 컴포넌트 |
| Anim Duration | 슬라이드 애니메이션 시간 (초) |

---

### MinimapController
우측 상단 미니맵을 담당합니다. 클릭하면 전체 맵 팝업이 열립니다.

| Inspector 항목 | 설명 |
|---|---|
| Minimap Panel | 미니맵을 표시할 RectTransform. 비워두면 'MiniMapPanel' 이름으로 자동 탐색 |
| Rt Width | 렌더 텍스처 해상도 (가로). 클수록 선명하지만 성능 소모 |
| Border Padding | 전체 맵 여백 (월드 단위) |
| Minimap Zoom | 미니맵 확대 배율. 1 = 메인 카메라와 동일 범위, 2 = 2배 넓게 표시 |
| Player Icon Size | 플레이어 아이콘 크기 (픽셀) |
| Npc Icon Size | NPC 아이콘 크기 (픽셀) |
| Player Color | 플레이어 아이콘 색상 |
| Guard Color | Guard NPC 아이콘 색상 (일반) |
| Guard Hot Color | Guard가 Alert/Chase 상태일 때 색상 |
| Civilian Color | Civilian NPC 아이콘 색상 |

> Tilemap이 씬에 있으면 전체 맵 범위를 자동으로 계산합니다.  
> 전체 맵은 `ESC`로도 닫을 수 있습니다.

---

## 12. 사운드 시스템

### AudioManager
BGM과 SFX를 모두 관리하는 싱글톤입니다. DontDestroyOnLoad로 씬 전환 시에도 유지됩니다.  
MainMenu 씬과 각 플레이 씬 모두에 배치합니다.

**BGM (배경음악)** — 루프 재생, 트랙 변경 시 자동 페이드 전환

| Inspector 항목 | 재생 시점 |
|---|---|
| Bgm Main Menu | 메인 메뉴 |
| Bgm Prologue | 프롤로그 씬 |
| Bgm Stage Ambient | 스테이지 기본 상태 (경계 레벨 1) |
| Bgm Stage Alert L2 | 경계 레벨 2 자동 전환 |
| Bgm Stage Alert L3 | 경계 레벨 3 자동 전환 |
| Bgm Stage Alert L4 | 경계 레벨 4 자동 전환 |

**SFX (효과음)** — 이벤트 발생 시 자동 재생

| Inspector 항목 | 재생 시점 |
|---|---|
| Sfx Footstep | 플레이어 이동 중 (0.38초마다) |
| Sfx Carry Pickup | 오브젝트 들기 |
| Sfx Carry Drop | 오브젝트 내려놓기 |
| Sfx Item Pickup | 아이템 줍기 |
| Sfx Item Receive | NPC 대화 후 아이템 지급 |
| Sfx Door Open | 문 열기 |
| Sfx Door Close | 문 닫기 |
| Sfx Light On | 조명 켜기 |
| Sfx Light Off | 조명 끄기 |
| Sfx Object Slide | 오브젝트 밀기/당기기 |
| Sfx Npc Suspicious | NPC Suspicious 상태 진입 |
| Sfx Npc Alert | NPC Alert/Chase 상태 진입 |
| Sfx Npc Search | NPC Search 상태 진입 |
| Sfx Npc Arrest | NPC가 플레이어를 체포할 때 |
| Sfx Checkpoint Save | 체크포인트 저장 |
| Sfx Alert Level Up | 전역 경계 레벨 상승 |
| Sfx Suspicion Spike | 플레이어가 그림자→노출 전환 시 스파이크 |
| Sfx Dialogue Next | 대화 줄 넘김 |
| Sfx UI Click | 메뉴 버튼 클릭 |
| Sfx Cutscene Letterbox | 컷씬 레터박스 등장 |

**추가 설정**

| Inspector 항목 | 설명 |
|---|---|
| Default BGM Volume | 기본 BGM 볼륨 (0~1) |
| Default SFX Volume | 기본 SFX 볼륨 (0~1) |
| Fade Duration | BGM 트랙 전환 시 페이드 시간 (초) |

---

### SceneBGM
씬 로드 시 특정 BGM 트랙을 자동 재생하는 컴포넌트입니다. 각 씬에 하나 배치합니다.

| Inspector 항목 | 설명 |
|---|---|
| Track | 이 씬에서 재생할 BGM 트랙 선택 (BGMTrack 열거형) |

---

### SettingsPopup
설정 팝업 (볼륨, 전체화면) UI입니다.

| Inspector 항목 | 설명 |
|---|---|
| Popup Root | 팝업 전체 루트 GameObject |
| Bgm Slider | BGM 볼륨 슬라이더 |
| Sfx Slider | SFX 볼륨 슬라이더 |
| Fullscreen Toggle | 전체화면 토글 |
| Close Button | X 닫기 버튼 |

---

## 13. 씬 관리

### MainMenuUI
메인 메뉴를 담당합니다.

| Inspector 항목 | 설명 |
|---|---|
| Prologue Scene | "새 게임" 버튼 클릭 시 로드할 씬 이름 |
| Btn New Game | "새 게임" 버튼 |
| Btn Continue | "이어하기" 버튼 (저장 데이터 없으면 자동 비활성) |
| Btn Settings | "설정" 버튼 |
| Btn Quit | "종료" 버튼 |
| Dim Color | "이어하기" 버튼 비활성 시 텍스트 색상 |
| Settings Popup | SettingsPopup 컴포넌트 연결 |
| Fade Delay | 버튼 클릭 후 씬 전환 전 대기 시간 (초) |

---

### SceneFader
씬 전환 시 화면 페이드인/아웃을 담당하는 싱글톤입니다. DontDestroyOnLoad로 유지됩니다.

| Inspector 항목 | 설명 |
|---|---|
| Fade Duration | 페이드 애니메이션 시간 (초) |

> 씬 전환(LoadScene) 후 자동으로 FadeIn이 실행됩니다.

---

### ExitTrigger (씬 전환용)
목표 완료 후 플레이어가 특정 구역에 진입하면 승리를 처리합니다.  
(자세한 내용은 [10. 승리 / 패배 조건](#10-승리--패배-조건) 참조)

---

## 14. ScriptableObject 에셋 목록

레벨 디자인에서 직접 만들어야 하는 에셋입니다.

| 에셋 타입 | 생성 메뉴 | 용도 |
|---|---|---|
| **DialogueData** | `Create > ShadowSeller > DialogueData` | 대화 내용 저장 |
| **NpcKindData** | `Create > ShadowSell > NPC Kind Data` | NPC 행동 수치 저장 |

---

## 씬 구성 체크리스트

새 스테이지 씬을 만들 때 필요한 컴포넌트 목록입니다.

### 필수 싱글톤 (씬당 하나)
- [ ] `AudioManager` + `SceneBGM`
- [ ] `CheckpointManager`
- [ ] `AlertManager`
- [ ] `SuspicionManager`
- [ ] `ObjectiveManager` (목표가 있는 씬)
- [ ] `CutsceneDirector` (컷씬이 있는 씬)
- [ ] `DialogueSystem` (대화가 있는 씬)
- [ ] `SceneFader`
- [ ] `CinematicBars` (컷씬이 있는 씬)

### 플레이어 GameObject 필요 컴포넌트
- [ ] `PlayerController`
- [ ] `InputReader`
- [ ] `PlayerExposureTracker`
- [ ] `Rigidbody2D` (Gravity Scale = 0, Constraints = Freeze Rotation Z)
- [ ] `Collider2D`
- [ ] `SpriteRenderer`

### 카메라 필요 컴포넌트
- [ ] `CameraFollow` (Target = 플레이어)

### NPC GameObject 필요 컴포넌트
- [ ] `NPCController`
- [ ] `SpeechBubble` (선택)
- [ ] `Rigidbody2D` (Gravity Scale = 0)
- [ ] `Collider2D`
- [ ] `SpriteRenderer`

### UI Canvas 필요 컴포넌트
- [ ] `InteractionPanel`
- [ ] `DialogueSystem`
- [ ] `InventoryUI`
- [ ] `SuspicionUI`
- [ ] `AlertLevelUI`
- [ ] `GameOverUI`
- [ ] `ExaminePopup` (확인하기 기능 사용 시)
- [ ] `HUDToggle` (선택)
- [ ] `MinimapController` (선택)
