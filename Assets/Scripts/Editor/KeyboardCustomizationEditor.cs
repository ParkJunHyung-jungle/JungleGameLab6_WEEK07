#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(KeyboardCustomization))]
public sealed class KeyboardCustomizationEditor : Editor
{
    /// <summary>
    /// Inspector 입력으로 직렬화된 설정을 표시하고 변경된 Case와 키캡 외관을 적용한다.
    /// 반환값 없이 선택한 Material을 렌더러에 반영하고 에디터 화면을 갱신한다.
    /// </summary>
    public override void OnInspectorGUI()
    {
        if (DrawDefaultInspector())
        {
            ((KeyboardCustomization)target).ApplyCaseEditorPreview();
        }
    }
}
#endif
