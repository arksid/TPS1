using System;
using System.Linq;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WaypointGuideTrigger : MonoBehaviour
{
    [Header("플레이어 식별")]
    public string playerTag = "Player";

    [Header("안내(필수)")]
    public SimpleWaypointUI waypointUI;   // 화면 마커 UI
    public Transform target;              // 이동해야 할 목적지(없으면 이 트리거의 Transform 사용)
    [TextArea] public string message = "다음 지역으로 이동";

    [Header("아웃라인(선택)")]
    public GameObject outlineTarget;      // 아웃라인 대상(비우면 target.gameObject)
    public bool autoAddOutline = true;    // QuickOutline/Outline 없으면 자동으로 추가

    [Header("표시/해제 타이밍")]
    public bool showOnEnable = false;     // 오브젝트 활성 시 자동 표시
    public bool clearExistingMarkers = true;
    public bool hideOnPlayerEnter = true; // 플레이어가 '이 트리거'에 들어오면 도착으로 간주 → 숨김
    public bool deactivateOnArrive = true;

    bool _shown;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (showOnEnable) ShowNow();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!hideOnPlayerEnter) return;
        if (!other.CompareTag(playerTag)) return;

        // 도착 처리: 마커/아웃라인 해제
        HideNow();
        if (deactivateOnArrive) gameObject.SetActive(false);
    }

    // ====== 외부에서 호출 가능 ======
    [ContextMenu("Show Now")]
    public void ShowNow()
    {
        if (!waypointUI) { Debug.LogWarning("[WaypointGuideTrigger] waypointUI 미지정"); return; }

        var t = target ? target : transform;

        if (clearExistingMarkers) WaypointDirector.Clear();
        WaypointDirector.Show(waypointUI, t, message);

        var go = (outlineTarget != null) ? outlineTarget : t.gameObject;
        EnsureOutlineEnabled(go);

        _shown = true;
    }

    [ContextMenu("Hide Now")]
    public void HideNow()
    {
        // 마커/아웃라인 모두 정리
        WaypointDirector.Clear();

        var t = target ? target : transform;
        var go = (outlineTarget != null) ? outlineTarget : t.gameObject;
        OutlineHelper.SetOutline(go, false);

        _shown = false;
    }

    // ====== 유틸: 아웃라인 보장 ======
    void EnsureOutlineEnabled(GameObject go)
    {
        if (!go) return;

        // 먼저 기존 헬퍼 호출
        OutlineHelper.SetOutline(go, true);

        // 이미 켜졌으면 끝
        if (HasEnabledOutline(go)) return;

        if (!autoAddOutline) return;

        // QuickOutline/Outline 자동 추가
        var qo = GetOrAddBehaviour(go, "QuickOutline");
        if (qo == null) qo = GetOrAddBehaviour(go, "Outline");
        if (qo != null) qo.enabled = true;
    }

    bool HasEnabledOutline(GameObject go)
    {
        var comps = go.GetComponents<Behaviour>();
        foreach (var c in comps)
        {
            if (!c) continue;
            var n = c.GetType().Name;
            if ((n == "QuickOutline" || n == "Outline") && c.enabled) return true;
        }
        return false;
    }

    Behaviour GetOrAddBehaviour(GameObject go, string typeName)
    {
        var t = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
            .FirstOrDefault(x => x != null && x.Name == typeName && typeof(Behaviour).IsAssignableFrom(x));
        if (t == null) return null;

        var exist = go.GetComponent(t) as Behaviour;
        if (exist != null) return exist;
        return go.AddComponent(t) as Behaviour;
    }
}
