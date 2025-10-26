using UnityEngine;

public class DebugAugmentHUD : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F3;
    public bool show = true;
    public bool showDamageLogs = false; // 필요하면 데미지 로그 토글용

    private GUIStyle _style;
    private float _lastUpdate;
    private string _cachedText = "";

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            show = !show;

        if (!show) return;

        // 0.2초마다 갱신(부하 줄이기)
        if (Time.unscaledTime - _lastUpdate > 0.2f)
        {
            _lastUpdate = Time.unscaledTime;
            _cachedText = BuildText();
        }
    }

    string BuildText()
    {
        var mgr = StatModifierManager.Instance;
        var ch = Character.Instance;

        if (mgr == null) return "StatModifierManager: (null)\n";
        if (ch == null) return "Character: (null)\n";

        // 배율/가산 스냅샷
        return
            $"[Augment DEBUG]\n" +
            $"- DamageMul   : {mgr.DamageMultiplier:0.00}x\n" +
            $"- FireRateMul : {mgr.FireRateMultiplier:0.00}x\n" +
            $"- MoveSpeedMul: {mgr.MoveSpeedMultiplier:0.00}x\n" +
            $"- CritChance  : {ch.CriticalChance:0}%\n" +     // Character에 바로 가산 반영 중
            $"- HealOnKill  : {mgr.HealOnKill:0}\n" +
            $"- UltOnHit    : {mgr.UltimateOnHitCharge:0.##}\n" +
            $"- HP/Shield   : {ch.Health}/{ch.MaxHealth}  |  {ch.Shield}/{ch.MaxShield}\n" +
            $"- MoveSpeed   : {ch.moveSpeed:0.##}\n" +
            $"- DamageLog   : {(showDamageLogs ? "ON" : "OFF")} (QuickTester에서 T키로 토글)";
    }

    void OnGUI()
    {
        if (!show) return;

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label);
            _style.fontSize = 14;
            _style.normal.textColor = Color.white;
        }

        var rect = new Rect(10, 10, 420, 220);
        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect, _cachedText, _style);
    }
}
