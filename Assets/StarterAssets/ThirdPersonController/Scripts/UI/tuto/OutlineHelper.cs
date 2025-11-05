using System.Linq;
using UnityEngine;

public static class OutlineHelper
{
    /// <summary>
    /// target과 자식들에서 이름이 "Outline" 또는 "QuickOutline"인 컴포넌트를 찾아 Enable/Disable.
    /// 없으면 경고만 띄우고 넘어갑니다.
    /// </summary>
    public static void SetOutline(GameObject target, bool on)
    {
        if (target == null) return;

        // 1) "Outline" 타입
        var outlines = target.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(c => c && c.GetType().Name == "Outline")
            .ToArray();
        foreach (var o in outlines)
        {
            var enabledProp = o.GetType().GetProperty("enabled");
            if (enabledProp != null) enabledProp.SetValue(o, on, null);
            else o.enabled = on;
        }

        // 2) "QuickOutline" 타입(자주 쓰는 오픈소스)
        var quicks = target.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(c => c && c.GetType().Name == "QuickOutline")
            .ToArray();
        foreach (var q in quicks)
        {
            var enabledProp = q.GetType().GetProperty("enabled");
            if (enabledProp != null) enabledProp.SetValue(q, on, null);
            else q.enabled = on;
        }

        if (outlines.Length == 0 && quicks.Length == 0)
        {
            Debug.LogWarning($"[OutlineHelper] {target.name} 에서 Outline/QuickOutline 컴포넌트를 찾지 못했습니다.");
        }
    }
}
