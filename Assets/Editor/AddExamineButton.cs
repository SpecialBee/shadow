using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using ShadowSeller.UI;

public class AddExamineButton
{
    [MenuItem("Tools/ShadowSeller/Add Examine Button")]
    public static void Execute()
    {
        // 1. ButtonContainer 찾기
        var container = GameObject.Find("ButtonContainer");
        if (container == null)
        {
            Debug.LogError("[AddExamineButton] 'ButtonContainer' GameObject를 찾지 못했습니다.");
            return;
        }

        // 2. 기존 버튼 하나를 복제해서 구조 맞추기
        Button templateBtn = null;
        foreach (Transform child in container.transform)
        {
            templateBtn = child.GetComponent<Button>();
            if (templateBtn != null) break;
        }

        if (templateBtn == null)
        {
            Debug.LogError("[AddExamineButton] ButtonContainer 안에 버튼이 없습니다.");
            return;
        }

        // 3. 복제
        var newBtnGO = Object.Instantiate(templateBtn.gameObject, container.transform);
        newBtnGO.name = "ExamineBtn";

        // 4. 레이블 변경
        var tmp = newBtnGO.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) tmp.text = "확인하기";

        // 5. onClick 초기화
        var btn = newBtnGO.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();

        // 6. InteractionPanel에 슬롯 연결
        var panel = Object.FindAnyObjectByType<InteractionPanel>();
        if (panel == null)
        {
            Debug.LogError("[AddExamineButton] InteractionPanel 컴포넌트를 찾지 못했습니다.");
            return;
        }

        var so   = new SerializedObject(panel);
        var prop = so.FindProperty("examineBtn");
        if (prop == null)
        {
            Debug.LogError("[AddExamineButton] InteractionPanel에 'examineBtn' 필드가 없습니다. 컴파일을 확인하세요.");
            return;
        }
        prop.objectReferenceValue = btn;
        so.ApplyModifiedProperties();

        // 7. 씬 더티 표시
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[AddExamineButton] 완료! ButtonContainer 마지막에 'ExamineBtn'이 추가되었고, InteractionPanel.examineBtn에 연결됐습니다.");
        Selection.activeGameObject = newBtnGO;
    }
}
