using UnityEngine;
using UnityEditor;
using ShadowSeller.Core;

[CustomEditor(typeof(InteractableObject))]
public class InteractableObjectEditor : Editor
{
    // 상호작용 종류
    SerializedProperty _canCarry, _canPush, _canPull, _isDoor;
    SerializedProperty _canToggleLight, _canInventory, _canTalk, _isTarget, _canExamine;
    // 들기
    SerializedProperty _holdOffset;
    // 밀기
    SerializedProperty _pushDistance, _pushSpeed;
    // 당기기
    SerializedProperty _pullDistance, _pullSpeed;
    // 문
    SerializedProperty _doorCollider, _doorRenderer, _openSprite, _closedSprite, _startOpen;
    // 조명
    SerializedProperty _controlledSources;
    // 인벤토리
    SerializedProperty _itemName;
    // NPC 대화
    SerializedProperty _npcDialogue, _speechBubble;
    // 아이템 지급
    SerializedProperty _giveItemAfterTalk, _rewardItemName, _rewardItemSprite, _giveItemOnce, _rewardedDialogue;
    // 목표 대화
    SerializedProperty _dialogue;
    // 확인하기
    SerializedProperty _examineSprite;
    // 접근
    SerializedProperty _highlightColor, _highlightAlpha, _approachRadius;
    // 벽 감지
    SerializedProperty _wallLayer;
    // 방향 표시
    SerializedProperty _dirArrow;

    void OnEnable()
    {
        _canCarry        = serializedObject.FindProperty("canCarry");
        _canPush         = serializedObject.FindProperty("canPush");
        _canPull         = serializedObject.FindProperty("canPull");
        _isDoor          = serializedObject.FindProperty("isDoor");
        _canToggleLight  = serializedObject.FindProperty("canToggleLight");
        _canInventory    = serializedObject.FindProperty("canInventory");
        _canTalk         = serializedObject.FindProperty("canTalk");
        _isTarget        = serializedObject.FindProperty("isTarget");
        _canExamine      = serializedObject.FindProperty("canExamine");

        _holdOffset      = serializedObject.FindProperty("holdOffset");
        _pushDistance    = serializedObject.FindProperty("pushDistance");
        _pushSpeed       = serializedObject.FindProperty("pushSpeed");
        _pullDistance    = serializedObject.FindProperty("pullDistance");
        _pullSpeed       = serializedObject.FindProperty("pullSpeed");

        _doorCollider    = serializedObject.FindProperty("doorCollider");
        _doorRenderer    = serializedObject.FindProperty("doorRenderer");
        _openSprite      = serializedObject.FindProperty("openSprite");
        _closedSprite    = serializedObject.FindProperty("closedSprite");
        _startOpen       = serializedObject.FindProperty("startOpen");

        _controlledSources = serializedObject.FindProperty("controlledSources");
        _itemName          = serializedObject.FindProperty("itemName");
        _npcDialogue          = serializedObject.FindProperty("npcDialogue");
        _speechBubble         = serializedObject.FindProperty("speechBubble");
        _giveItemAfterTalk    = serializedObject.FindProperty("giveItemAfterTalk");
        _rewardItemName       = serializedObject.FindProperty("rewardItemName");
        _rewardItemSprite     = serializedObject.FindProperty("rewardItemSprite");
        _giveItemOnce         = serializedObject.FindProperty("giveItemOnce");
        _rewardedDialogue     = serializedObject.FindProperty("rewardedDialogue");
        _dialogue             = serializedObject.FindProperty("dialogue");
        _examineSprite     = serializedObject.FindProperty("examineSprite");

        _highlightColor  = serializedObject.FindProperty("highlightColor");
        _highlightAlpha  = serializedObject.FindProperty("highlightAlpha");
        _approachRadius  = serializedObject.FindProperty("approachRadius");
        _wallLayer       = serializedObject.FindProperty("wallLayer");
        _dirArrow        = serializedObject.FindProperty("dirArrow");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 상호작용 종류 ────────────────────────────────────────────
        Label("상호작용 종류");
        EditorGUILayout.PropertyField(_canCarry,       C("들기 가능"));
        EditorGUILayout.PropertyField(_canPush,        C("밀기 가능"));
        EditorGUILayout.PropertyField(_canPull,        C("당기기 가능"));
        EditorGUILayout.PropertyField(_isDoor,         C("문"));
        EditorGUILayout.PropertyField(_canToggleLight, C("조명 켜기/끄기"));
        EditorGUILayout.PropertyField(_canInventory,   C("줍기 가능"));
        EditorGUILayout.PropertyField(_canExamine,     C("확인하기 가능"));
        EditorGUILayout.PropertyField(_canTalk,        C("대화 가능"));
        EditorGUILayout.PropertyField(_isTarget,       C("목표 NPC"));
        Space();

        // ── 들기 ─────────────────────────────────────────────────────
        if (_canCarry.boolValue)
        {
            Label("들기 설정");
            EditorGUILayout.PropertyField(_holdOffset, C("들기 거리"));
            Space();
        }

        // ── 밀기 ─────────────────────────────────────────────────────
        if (_canPush.boolValue)
        {
            Label("밀기 설정");
            EditorGUILayout.PropertyField(_pushDistance, C("밀기 거리"));
            EditorGUILayout.PropertyField(_pushSpeed,    C("밀기 속도"));
            Space();
        }

        // ── 당기기 ───────────────────────────────────────────────────
        if (_canPull.boolValue)
        {
            Label("당기기 설정");
            EditorGUILayout.PropertyField(_pullDistance, C("당기기 거리"));
            EditorGUILayout.PropertyField(_pullSpeed,    C("당기기 속도"));
            Space();
        }

        // ── 문 ───────────────────────────────────────────────────────
        if (_isDoor.boolValue)
        {
            Label("문 설정");
            EditorGUILayout.PropertyField(_doorCollider, C("문 콜라이더"));
            EditorGUILayout.PropertyField(_doorRenderer, C("문 렌더러"));
            EditorGUILayout.PropertyField(_openSprite,   C("열린 스프라이트"));
            EditorGUILayout.PropertyField(_closedSprite, C("닫힌 스프라이트"));
            EditorGUILayout.PropertyField(_startOpen,    C("시작 시 열려있음"));
            Space();
        }

        // ── 조명 ─────────────────────────────────────────────────────
        if (_canToggleLight.boolValue)
        {
            Label("조명 설정");
            EditorGUILayout.PropertyField(_controlledSources, C("제어할 조명"), true);
            Space();
        }

        // ── 줍기 ─────────────────────────────────────────────────────
        if (_canInventory.boolValue)
        {
            Label("줍기 설정");
            EditorGUILayout.PropertyField(_itemName, C("아이템 이름"));
            Space();
        }

        // ── 확인하기 ─────────────────────────────────────────────────
        if (_canExamine.boolValue)
        {
            Label("확인하기 설정");
            EditorGUILayout.PropertyField(_examineSprite, C("확인 이미지 (Sprite)"));
            Space();
        }

        // ── NPC 대화 ─────────────────────────────────────────────────
        if (_canTalk.boolValue)
        {
            Label("NPC 대화 설정");
            EditorGUILayout.PropertyField(_npcDialogue,  C("대화 데이터"));
            EditorGUILayout.PropertyField(_speechBubble, C("말풍선"));
            Space();

            Label("아이템 지급 설정");
            EditorGUILayout.PropertyField(_giveItemAfterTalk, C("대화 후 아이템 지급"));
            if (_giveItemAfterTalk.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_rewardItemName,   C("지급할 아이템 이름"));
                EditorGUILayout.PropertyField(_rewardItemSprite, C("지급할 아이템 아이콘"));
                EditorGUILayout.PropertyField(_giveItemOnce,     C("한 번만 지급"));
                EditorGUILayout.PropertyField(_rewardedDialogue, C("지급 후 대화", "재대화 시 보여줄 대사. 비워두면 원래 대화 반복."));
                EditorGUI.indentLevel--;
            }
            Space();
        }

        // ── 목표 대화 ────────────────────────────────────────────────
        if (_isTarget.boolValue)
        {
            Label("목표 NPC 설정");
            EditorGUILayout.PropertyField(_dialogue, C("대화 데이터"));
            Space();
        }

        // ── 접근 감지 & 하이라이트 ───────────────────────────────────
        Label("접근 감지 & 하이라이트");
        EditorGUILayout.PropertyField(_approachRadius,  C("감지 반경"));
        EditorGUILayout.PropertyField(_highlightColor,  C("하이라이트 색상"));
        EditorGUILayout.Slider(_highlightAlpha,  0f, 1f, C("하이라이트 투명도"));
        Space();

        // ── 벽 감지 ──────────────────────────────────────────────────
        Label("벽 감지");
        EditorGUILayout.PropertyField(_wallLayer, C("벽 레이어", "설정 안 하면 'wall' 태그로 자동 감지"));
        Space();

        // ── 방향 표시 ────────────────────────────────────────────────
        if (_canPush.boolValue || _canPull.boolValue)
        {
            Label("방향 표시");
            EditorGUILayout.PropertyField(_dirArrow, C("방향 화살표 (비워두면 자동 생성)"));
        }

        serializedObject.ApplyModifiedProperties();
    }

    static GUIContent C(string label, string tooltip = "") => new GUIContent(label, tooltip);
    static void Label(string text) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    static void Space() => EditorGUILayout.Space(6);
}
