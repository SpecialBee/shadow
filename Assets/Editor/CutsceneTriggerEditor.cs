using UnityEngine;
using UnityEditor;
using ShadowSeller.Core;

[CustomEditor(typeof(CutsceneTrigger))]
public class CutsceneTriggerEditor : Editor
{
    private SerializedProperty _mode;
    private SerializedProperty _oneShot;
    private SerializedProperty _approachRadius;
    private SerializedProperty _steps;
    private SerializedProperty _overrideSpawn;
    private SerializedProperty _spawnPoint;
    private SerializedProperty _requiredMeetFlag;
    private SerializedProperty _deactivateOnEnd;
    private SerializedProperty _meetFlag;

    private bool[] _foldouts = new bool[0];

    private static readonly string[] StepTypeNames =
        { "대화", "플레이어 이동", "오브젝트 이동", "카메라 이동", "대기", "페이드" };

    private void OnEnable()
    {
        _mode             = serializedObject.FindProperty("mode");
        _oneShot          = serializedObject.FindProperty("oneShot");
        _approachRadius   = serializedObject.FindProperty("approachRadius");
        _steps            = serializedObject.FindProperty("steps");
        _overrideSpawn    = serializedObject.FindProperty("overridePlayerSpawn");
        _spawnPoint       = serializedObject.FindProperty("spawnPoint");
        _requiredMeetFlag = serializedObject.FindProperty("requiredMeetFlag");
        _deactivateOnEnd  = serializedObject.FindProperty("deactivateOnEnd");
        _meetFlag         = serializedObject.FindProperty("meetFlag");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 트리거 설정 ──────────────────────────────────────────────
        EditorGUILayout.LabelField("트리거 설정", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_mode,    new GUIContent("발동 방식", "Zone: 충돌로 자동 발동 / Interaction: 근처에서 E키"));
        EditorGUILayout.PropertyField(_oneShot, new GUIContent("한 번만 발동"));

        var modeVal = (CutsceneTrigger.TriggerMode)_mode.enumValueIndex;
        if (modeVal == CutsceneTrigger.TriggerMode.Interaction)
            EditorGUILayout.PropertyField(_approachRadius, new GUIContent("감지 반경"));

        EditorGUILayout.Space(8);

        // ── 컷씬 스텝 ───────────────────────────────────────────────
        EditorGUILayout.LabelField("컷씬 스텝 목록", EditorStyles.boldLabel);

        if (_foldouts.Length != _steps.arraySize)
        {
            var tmp = new bool[_steps.arraySize];
            for (int i = 0; i < Mathf.Min(_foldouts.Length, tmp.Length); i++)
                tmp[i] = _foldouts[i];
            _foldouts = tmp;
        }

        for (int i = 0; i < _steps.arraySize; i++)
        {
            var step     = _steps.GetArrayElementAtIndex(i);
            var typeProp = step.FindPropertyRelative("type");
            var typeIdx  = typeProp.enumValueIndex;
            var label    = (typeIdx >= 0 && typeIdx < StepTypeNames.Length) ? StepTypeNames[typeIdx] : "?";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _foldouts[i] = EditorGUILayout.Foldout(_foldouts[i], $"[{i}] {label}", true, EditorStyles.foldoutHeader);
            if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0)
                _steps.MoveArrayElement(i, i - 1);
            if (GUILayout.Button("▼", GUILayout.Width(24)) && i < _steps.arraySize - 1)
                _steps.MoveArrayElement(i, i + 1);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                _steps.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (_foldouts[i])
            {
                EditorGUI.indentLevel++;
                DrawStep(step);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        if (GUILayout.Button("+ 스텝 추가"))
        {
            _steps.arraySize++;
            var newFolds = new bool[_steps.arraySize];
            for (int i = 0; i < _foldouts.Length; i++) newFolds[i] = _foldouts[i];
            newFolds[_steps.arraySize - 1] = true;
            _foldouts = newFolds;
        }

        EditorGUILayout.Space(8);

        // ── 발동 조건 ───────────────────────────────────────────────
        EditorGUILayout.LabelField("발동 조건", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_requiredMeetFlag, new GUIContent("필요 플래그", "이 플래그가 등록돼 있어야 트리거 발동. 비워두면 조건 없음."));

        EditorGUILayout.Space(8);

        // ── 컷씬 후 플레이어 위치 ───────────────────────────────────
        EditorGUILayout.LabelField("컷씬 종료 후 플레이어 위치", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_overrideSpawn, new GUIContent("위치 변경 사용"));
        if (_overrideSpawn.boolValue)
            EditorGUILayout.PropertyField(_spawnPoint, new GUIContent("시작 위치 (씬의 빈 오브젝트)"));

        EditorGUILayout.Space(8);

        // ── 컷씬 종료 후 처리 ───────────────────────────────────────
        EditorGUILayout.LabelField("컷씬 종료 후 처리", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(_deactivateOnEnd, new GUIContent("비활성화할 오브젝트들", "컷씬이 끝나면 꺼질 오브젝트 (NPC 등)."), true);
        EditorGUILayout.PropertyField(_meetFlag, new GUIContent("등록할 플래그", "CheckpointManager에 저장할 플래그 이름. 비워두면 생략."));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawStep(SerializedProperty step)
    {
        var typeProp     = step.FindPropertyRelative("type");
        var waitComplete = step.FindPropertyRelative("waitForComplete");

        typeProp.enumValueIndex = EditorGUILayout.Popup(
            new GUIContent("스텝 종류"), typeProp.enumValueIndex, StepTypeNames);

        EditorGUILayout.PropertyField(waitComplete,
            new GUIContent("이 스텝 완료 후 다음 스텝", "체크 해제 시 다음 스텝과 동시에 실행"));

        EditorGUILayout.Space(3);

        var type = (CutsceneStepType)typeProp.enumValueIndex;
        switch (type)
        {
            case CutsceneStepType.Dialogue:
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("dialogue"),
                    new GUIContent("대화 데이터 (DialogueData)"));
                break;

            case CutsceneStepType.MovePlayer:
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("moveTo"),
                    new GUIContent("목적지 오브젝트"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("moveDuration"),
                    new GUIContent("이동 시간 (초)"));
                break;

            case CutsceneStepType.MoveObject:
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("objectToMove"),
                    new GUIContent("이동할 오브젝트"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("moveTo"),
                    new GUIContent("목적지 오브젝트"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("moveDuration"),
                    new GUIContent("이동 시간 (초)"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("smoothMove"),
                    new GUIContent("부드러운 이동 (Ease)"));
                break;

            case CutsceneStepType.MoveCamera:
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("cameraTarget"),
                    new GUIContent("카메라 목표 위치 오브젝트"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("cameraDuration"),
                    new GUIContent("이동 시간 (초)"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("cameraFollowAfter"),
                    new GUIContent("이후 카메라 플레이어 팔로우"));
                break;

            case CutsceneStepType.Wait:
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("waitSeconds"),
                    new GUIContent("대기 시간 (초)"));
                break;

            case CutsceneStepType.Fade:
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("fadeOut"),
                    new GUIContent("페이드 아웃 (화면이 검정으로)", "체크 해제 = 페이드 인 (밝아짐)"));
                EditorGUILayout.PropertyField(
                    step.FindPropertyRelative("fadeDuration"),
                    new GUIContent("페이드 시간 (초)"));
                break;
        }
    }
}
