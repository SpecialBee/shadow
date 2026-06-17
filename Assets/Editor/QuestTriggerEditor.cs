using UnityEngine;
using UnityEditor;
using ShadowSeller.Core;

[CustomEditor(typeof(QuestTrigger))]
public class QuestTriggerEditor : Editor
{
    SerializedProperty _questData;
    SerializedProperty _role;
    SerializedProperty _mode;
    SerializedProperty _oneShot;
    SerializedProperty _requiredActiveQuest;
    SerializedProperty _playCutsceneOnComplete;
    SerializedProperty _completionSteps;
    SerializedProperty _overrideSpawnOnComplete;
    SerializedProperty _completionSpawnPoint;
    SerializedProperty _completionMeetFlag;

    private bool[] _stepFoldouts = new bool[0];

    private static readonly string[] StepTypeNames =
        { "대화", "플레이어 이동", "오브젝트 이동", "카메라 이동", "대기", "페이드" };

    private void OnEnable()
    {
        _questData              = serializedObject.FindProperty("questData");
        _role                   = serializedObject.FindProperty("role");
        _mode                   = serializedObject.FindProperty("mode");
        _oneShot                = serializedObject.FindProperty("oneShot");
        _requiredActiveQuest    = serializedObject.FindProperty("requiredActiveQuest");
        _playCutsceneOnComplete = serializedObject.FindProperty("playCutsceneOnComplete");
        _completionSteps        = serializedObject.FindProperty("completionSteps");
        _overrideSpawnOnComplete= serializedObject.FindProperty("overrideSpawnOnComplete");
        _completionSpawnPoint   = serializedObject.FindProperty("completionSpawnPoint");
        _completionMeetFlag     = serializedObject.FindProperty("completionMeetFlag");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── 퀘스트 연결 ──────────────────────────────────────────────
        Label("퀘스트 연결");
        EditorGUILayout.PropertyField(_questData,  C("퀘스트 데이터"));
        EditorGUILayout.PropertyField(_role,       C("역할", "퀘스트 시작 or 진행도 추가"));
        EditorGUILayout.PropertyField(_mode,       C("발동 방식", "Zone: 콜라이더 진입 / OnExamine: 확인하기 버튼"));
        EditorGUILayout.PropertyField(_oneShot,    C("한 번만 발동"));
        Space();

        // ── 발동 조건 ────────────────────────────────────────────────
        Label("발동 조건 (선택)");
        EditorGUILayout.PropertyField(_requiredActiveQuest, C("활성 필요 퀘스트", "이 퀘스트가 활성 상태일 때만 발동. 비워두면 조건 없음."));
        Space();

        // ── 완료 시 처리 (AddProgress 역할일 때만) ───────────────────
        var roleVal = (QuestTrigger.TriggerRole)_role.enumValueIndex;
        if (roleVal == QuestTrigger.TriggerRole.AddProgress)
        {
            Label("완료 시 처리 (선택)");
            EditorGUILayout.PropertyField(_completionMeetFlag,     C("등록할 플래그", "완료 시 CheckpointManager에 저장할 플래그."));
            EditorGUILayout.PropertyField(_playCutsceneOnComplete, C("완료 시 컷씬 실행"));

            if (_playCutsceneOnComplete.boolValue)
            {
                EditorGUI.indentLevel++;
                DrawCompletionSteps();
                EditorGUILayout.PropertyField(_overrideSpawnOnComplete, C("플레이어 위치 변경"));
                if (_overrideSpawnOnComplete.boolValue)
                    EditorGUILayout.PropertyField(_completionSpawnPoint, C("위치 오브젝트"));
                EditorGUI.indentLevel--;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCompletionSteps()
    {
        EditorGUILayout.LabelField("컷씬 스텝 목록", EditorStyles.boldLabel);

        if (_stepFoldouts.Length != _completionSteps.arraySize)
        {
            var tmp = new bool[_completionSteps.arraySize];
            for (int i = 0; i < Mathf.Min(_stepFoldouts.Length, tmp.Length); i++)
                tmp[i] = _stepFoldouts[i];
            _stepFoldouts = tmp;
        }

        for (int i = 0; i < _completionSteps.arraySize; i++)
        {
            var step    = _completionSteps.GetArrayElementAtIndex(i);
            var typeProp = step.FindPropertyRelative("type");
            int typeIdx  = typeProp.enumValueIndex;
            string label = (typeIdx >= 0 && typeIdx < StepTypeNames.Length) ? StepTypeNames[typeIdx] : "?";

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();
            _stepFoldouts[i] = EditorGUILayout.Foldout(_stepFoldouts[i], $"[{i}] {label}", true, EditorStyles.foldoutHeader);
            if (GUILayout.Button("▲", GUILayout.Width(24)) && i > 0)   _completionSteps.MoveArrayElement(i, i - 1);
            if (GUILayout.Button("▼", GUILayout.Width(24)) && i < _completionSteps.arraySize - 1) _completionSteps.MoveArrayElement(i, i + 1);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
            {
                _completionSteps.DeleteArrayElementAtIndex(i);
                serializedObject.ApplyModifiedProperties();
                return;
            }
            EditorGUILayout.EndHorizontal();

            if (_stepFoldouts[i])
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
            _completionSteps.arraySize++;
            var tmp = new bool[_completionSteps.arraySize];
            for (int i = 0; i < _stepFoldouts.Length; i++) tmp[i] = _stepFoldouts[i];
            tmp[_completionSteps.arraySize - 1] = true;
            _stepFoldouts = tmp;
        }
    }

    private void DrawStep(SerializedProperty step)
    {
        var typeProp     = step.FindPropertyRelative("type");
        var waitComplete = step.FindPropertyRelative("waitForComplete");

        typeProp.enumValueIndex = EditorGUILayout.Popup(C("스텝 종류"), typeProp.enumValueIndex, StepTypeNames);
        EditorGUILayout.PropertyField(waitComplete, C("이 스텝 완료 후 다음 스텝"));
        EditorGUILayout.Space(2);

        switch ((CutsceneStepType)typeProp.enumValueIndex)
        {
            case CutsceneStepType.Dialogue:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("dialogue"), C("대화 데이터"));
                break;
            case CutsceneStepType.MovePlayer:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("moveTo"),       C("목적지"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("moveDuration"), C("이동 시간(초)"));
                break;
            case CutsceneStepType.MoveObject:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("objectToMove"), C("이동할 오브젝트"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("moveTo"),       C("목적지"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("moveDuration"), C("이동 시간(초)"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("smoothMove"),   C("부드러운 이동"));
                break;
            case CutsceneStepType.MoveCamera:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("cameraTarget"),     C("카메라 목표"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("cameraDuration"),   C("이동 시간(초)"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("cameraFollowAfter"),C("이후 플레이어 팔로우"));
                break;
            case CutsceneStepType.Wait:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("waitSeconds"), C("대기 시간(초)"));
                break;
            case CutsceneStepType.Fade:
                EditorGUILayout.PropertyField(step.FindPropertyRelative("fadeOut"),     C("페이드 아웃"));
                EditorGUILayout.PropertyField(step.FindPropertyRelative("fadeDuration"),C("페이드 시간(초)"));
                break;
        }
    }

    static GUIContent C(string label, string tooltip = "") => new GUIContent(label, tooltip);
    static void Label(string text) => EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    static void Space() => EditorGUILayout.Space(6);
}
