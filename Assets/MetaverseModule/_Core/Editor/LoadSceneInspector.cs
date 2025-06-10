using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Trigger_ChangeScene))]
public class LoadSceneInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        base.OnInspectorGUI();
        
        var origin = (Trigger_ChangeScene)target;

        if (origin.condition != Condition.None)
        {
            if (string.IsNullOrEmpty(origin.msg))
                origin.msg = "Press 'F'";
            
            origin.msg = EditorGUILayout.TextField("상호작용 메시지", origin.msg);
        }
        
        switch (origin.condition)
        {
            case Condition.CheckItem:
            case Condition.UseItem:
                origin.itemName = EditorGUILayout.TextField("아이템", origin.itemName);
                origin.itemAmount = EditorGUILayout.IntField("필요 수량", origin.itemAmount);
                break;
        }
        
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(origin);
    }
}
