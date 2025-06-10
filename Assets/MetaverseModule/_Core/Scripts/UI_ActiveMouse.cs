using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ActiveMouse : MonoBehaviour
{
    void OnEnable()
    {
        MetaverSystem.Instance.AddCursorObj(gameObject); 
    }

    void OnDisable()
    {
        MetaverSystem.Instance.RemoveCursorObj(gameObject);
    }
}
