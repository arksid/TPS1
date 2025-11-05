using UnityEngine;

public static class WaypointDirector
{
    // ===== 표시 권한 (튜토리얼 끝나기 전엔 표시 금지) =====
    static bool _enabled; // false이면 Show() 호출을 모두 무시
    public static void EnableHints() { _enabled = true; }
    public static void DisableHints() { _enabled = false; Clear(); }

    // ===== 현재 표시 상태 =====
    static SimpleWaypointUI _ui;
    static Transform _currentTarget;

    // ===== 디버그 옵션 =====
    public static bool DebugLogCallers = false; // 필요 시 true로
    static void Log(string msg) { if (DebugLogCallers) Debug.Log("[WaypointDirector] " + msg); }
    static void Warn(string msg) { Debug.LogWarning("[WaypointDirector] " + msg); }

    /// <summary>
    /// 웨이포인트 + 아웃라인 표시. (권한 없으면 무시)
    /// </summary>
    public static void Show(SimpleWaypointUI ui, Transform target, string message)
    {
        if (!_enabled)
        {
            Warn("Show() 무시됨: 튜토리얼 미완료 상태");
            return;
        }

        // 이전 표식 정리
        Clear();

        _ui = ui;
        _currentTarget = target;

        if (_currentTarget)
        {
            OutlineHelper.SetOutline(_currentTarget.gameObject, true); // 아웃라인 ON
            Log($"Outline ON -> {_currentTarget.name}");
        }

        if (_ui && _currentTarget)
        {
            _ui.Activate(_currentTarget, message); // 마커/UI ON
            Log($"Waypoint ON -> {_currentTarget.name}  msg='{message}'");
        }
        else
        {
            Warn("Show() 실패: ui 또는 target 누락");
        }
    }

    /// <summary>
    /// 웨이포인트 "UI만" 숨깁니다. (아웃라인은 유지)
    /// </summary>
    public static void HideUIOnly()
    {
        if (_ui != null)
        {
            _ui.Deactivate();
            Log("Waypoint UI OFF (outline kept)");
        }
        // _currentTarget(아웃라인 대상)은 유지
    }

    /// <summary>
    /// 마커 + 아웃라인 전부 정리.
    /// </summary>
    public static void Clear()
    {
        if (_currentTarget)
        {
            OutlineHelper.SetOutline(_currentTarget.gameObject, false); // 아웃라인 OFF
            Log($"Outline OFF -> {_currentTarget.name}");
        }

        if (_ui)
        {
            _ui.Deactivate(); // 마커/UI OFF
            Log("Waypoint OFF");
        }

        _currentTarget = null;
        _ui = null;
    }
}
