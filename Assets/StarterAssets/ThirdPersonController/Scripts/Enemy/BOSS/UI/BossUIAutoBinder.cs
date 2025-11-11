using UnityEngine;

public class BossUIAutoBinder : MonoBehaviour
{
    [Header("Optional")]
    public string displayNameOverride = "BOSS";

    void OnEnable()
    {
        var boss = GetComponent<BossMonster>();
        if (!boss) return;

        if (BossUIBinder.Instance)
        {
            BossUIBinder.Instance.ShowFor(boss, displayNameOverride);
        }
    }
}
