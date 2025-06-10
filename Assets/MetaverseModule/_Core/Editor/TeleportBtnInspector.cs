using UnityEditor;

[CustomEditor(typeof(ButtonAction_Teleport))]
public class TeleportBtnInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        base.OnInspectorGUI();
        
        var origin = (ButtonAction_Teleport)target;
        
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