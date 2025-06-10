using UnityEditor;

[CustomEditor(typeof(ButtonAction_ChangeScene))]
public class LoadSceneBtnInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        base.OnInspectorGUI();
        
        var origin = (ButtonAction_ChangeScene)target;
        
        switch (origin.condition)
        {
            case ButtonCondition.CheckItem:
            case ButtonCondition.UseItem:
                origin.itemName = EditorGUILayout.TextField("아이템", origin.itemName);
                origin.itemAmount = EditorGUILayout.IntField("필요 수량", origin.itemAmount);
                break;
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(origin);
    }
}