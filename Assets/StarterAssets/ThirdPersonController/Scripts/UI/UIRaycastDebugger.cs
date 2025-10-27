using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRaycastDebugger : MonoBehaviour
{
    PointerEventData ped;
    List<RaycastResult> results = new List<RaycastResult>();

    void Awake()
    {
        if (EventSystem.current == null)
        {
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            // 새 Input System이면 위 라인 대신 InputSystemUIInputModule 붙이세요.
        }
        ped = new PointerEventData(EventSystem.current);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            results.Clear();
            ped.position = Input.mousePosition;
            EventSystem.current.RaycastAll(ped, results);

            Debug.Log($"[UIRaycast] hits={results.Count}");
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                Debug.Log($"{i}. obj={r.gameObject.name} canvas={r.module?.ToString()} depth={r.depth}");
            }
        }
    }
}
