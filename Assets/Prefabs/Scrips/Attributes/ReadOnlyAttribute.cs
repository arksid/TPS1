// Assets/Scripts/Attributes/ReadOnlyAttribute.cs
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 인스펙터에서 값을 보기만 하고 수정 못 하게 만드는 커스텀 속성입니다.
/// </summary>
public class ReadOnlyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
public class ReadOnlyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        bool prev = GUI.enabled;
        GUI.enabled = false;                                 // 잠금
        EditorGUI.PropertyField(position, property, label, true);
        GUI.enabled = prev;                                  // 복원
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        => EditorGUI.GetPropertyHeight(property, label, true);
}
#endif
