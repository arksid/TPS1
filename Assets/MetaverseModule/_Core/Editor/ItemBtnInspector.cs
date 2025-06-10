using UnityEditor;

[CustomEditor(typeof(ButtonAction_AddItem))]
public class ItemBtnInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        base.OnInspectorGUI();
        
        var origin = (ButtonAction_AddItem)target;
        
        switch (origin.condition)
        {
            case ButtonCondition.CheckItem:
            case ButtonCondition.UseItem:
                origin.needItemName = EditorGUILayout.TextField("아이템", origin.needItemName);
                origin.needItemAmount = EditorGUILayout.IntField("필요 수량", origin.needItemAmount);
                break;
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(origin);
    }
}
