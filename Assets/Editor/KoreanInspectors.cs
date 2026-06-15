using UnityEngine;
using UnityEditor;
using ShadowSeller.Core;
using ShadowSeller.UI;

// ── CinematicBars ────────────────────────────────────────────────────────────
[CustomEditor(typeof(CinematicBars))]
public class CinematicBarsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("레터박스 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("barHeightRatio"),
            new GUIContent("바 높이 비율", "화면 높이 대비 비율. 0.12 = 12%"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("animDuration"),
            new GUIContent("슬라이드 시간 (초)"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("숨길 HUD 오브젝트", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("컷씬 중 숨길 오브젝트를 여기에 넣으세요.\n대화창(DialoguePanel)은 넣지 마세요.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("hudObjects"),
            new GUIContent("HUD 목록"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

// ── ExaminePopup ────────────────────────────────────────────────────────────
[CustomEditor(typeof(ExaminePopup))]
public class ExaminePopupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.LabelField("UI 연결", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("overlay"),      new GUIContent("팝업 루트 오브젝트"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("examineImage"), new GUIContent("이미지 컴포넌트"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("closeBtn"),     new GUIContent("닫기 버튼"));
        serializedObject.ApplyModifiedProperties();
    }
}

// ── InteractionPanel ────────────────────────────────────────────────────────
[CustomEditor(typeof(InteractionPanel))]
public class InteractionPanelEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("버튼 슬롯", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("carryBtn"),   new GUIContent("들기 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pushBtn"),    new GUIContent("밀기 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pullBtn"),    new GUIContent("당기기 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("doorBtn"),    new GUIContent("문 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("lightBtn"),   new GUIContent("조명 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("pickupBtn"),  new GUIContent("줍기 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("talkBtn"),    new GUIContent("대화 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("examineBtn"), new GUIContent("확인하기 버튼"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("투명도", EditorStyles.boldLabel);
        EditorGUILayout.Slider(serializedObject.FindProperty("activeAlpha"),   0f, 1f, new GUIContent("활성 버튼 투명도"));
        EditorGUILayout.Slider(serializedObject.FindProperty("inactiveAlpha"), 0f, 1f, new GUIContent("비활성 버튼 투명도"));

        serializedObject.ApplyModifiedProperties();
    }
}

// ── GameOverUI ───────────────────────────────────────────────────────────────
[CustomEditor(typeof(GameOverUI))]
public class GameOverUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("패배 UI", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defeatPanel"),      new GUIContent("패배 패널"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defeatReasonText"), new GUIContent("패배 이유 텍스트"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("defeatRestartBtn"), new GUIContent("다시하기 버튼"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("continueBtn"),      new GUIContent("이어하기 버튼 (체크포인트)"));

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("승리 UI", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("victoryPanel"),      new GUIContent("승리 패널"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("victoryRestartBtn"), new GUIContent("다시하기 버튼"));

        serializedObject.ApplyModifiedProperties();
    }
}

// ── SceneFader ───────────────────────────────────────────────────────────────
[CustomEditor(typeof(SceneFader))]
public class SceneFaderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeDuration"), new GUIContent("페이드 시간 (초)"));
        serializedObject.ApplyModifiedProperties();
    }
}

// ── SpeechBubble ─────────────────────────────────────────────────────────────
[CustomEditor(typeof(SpeechBubble))]
public class SpeechBubbleEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("스프라이트", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bubbleSprite"), new GUIContent("말풍선 스프라이트", "없으면 흰 사각형 자동 생성"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("위치 & 크기", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("xOffset"),  new GUIContent("X 위치 오프셋"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("yOffset"),  new GUIContent("Y 위치 오프셋"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingX"), new GUIContent("좌우 여백"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("paddingY"), new GUIContent("상하 여백"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("maxWidth"), new GUIContent("최대 가로 길이"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("표시 시간", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("duration"), new GUIContent("표시 시간 (초)"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeTime"), new GUIContent("페이드아웃 시간 (초)"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("폰트", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("font"), new GUIContent("TMP 폰트"));

        serializedObject.ApplyModifiedProperties();
    }
}

// ── SpeechBubbleArea ─────────────────────────────────────────────────────────
[CustomEditor(typeof(SpeechBubbleArea))]
public class SpeechBubbleAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("감지 범위", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerRadius"), new GUIContent("감지 반경 (월드 단위)"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("말풍선 내용", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("bubbleText"), new GUIContent("표시할 텍스트"));

        serializedObject.ApplyModifiedProperties();
    }
}

// ── Checkpoint ───────────────────────────────────────────────────────────────
[CustomEditor(typeof(Checkpoint))]
public class CheckpointEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("감지", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"), new GUIContent("감지 반경 (월드 단위)"));

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("색상", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("inactiveColor"), new GUIContent("비활성 색상"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("activeColor"),   new GUIContent("활성 색상"));

        serializedObject.ApplyModifiedProperties();
    }
}
