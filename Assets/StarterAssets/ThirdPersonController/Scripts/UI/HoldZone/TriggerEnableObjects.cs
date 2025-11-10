using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TriggerEnableObjects : MonoBehaviour
{
    public string playerTag = "Player";
    [Tooltip("트리거에 들어오면 켤 대상(스포너 GO 등). 시작할 때는 반드시 비활성 상태여야 합니다.")]
    public GameObject[] targetsToEnable;
    public bool onlyOnce = true;

    bool _done;

    void Reset()
    {
        // 콜라이더는 반드시 Trigger여야 합니다.
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (onlyOnce && _done) return;
        if (!other.CompareTag(playerTag)) return;

        _done = true;

        if (targetsToEnable == null) return;
        foreach (var go in targetsToEnable)
        {
            if (!go) continue;
            ActivateHierarchy(go);    // 부모가 꺼져 있어도 함께 켜짐
            Debug.Log($"[TriggerEnableObjects] 활성화: {go.name}");
        }
    }

    // 부모가 꺼져 있으면 자식만 켜도 효과가 없으니, 부모부터 순서대로 켜줍니다.
    static void ActivateHierarchy(GameObject leaf)
    {
        // 위로 타고 올라가며 스택에 쌓기
        var stack = new System.Collections.Generic.Stack<Transform>();
        var t = leaf.transform;
        while (t != null) { stack.Push(t); t = t.parent; }

        // 루트 → 리프 순으로 SetActive
        GameObject last = null;
        while (stack.Count > 0)
        {
            var cur = stack.Pop().gameObject;
            if (!cur.activeSelf) cur.SetActive(true);
            last = cur;
        }
    }
}
