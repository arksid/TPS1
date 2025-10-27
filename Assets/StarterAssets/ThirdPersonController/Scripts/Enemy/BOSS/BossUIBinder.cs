using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUIBinder : MonoBehaviour
{
    public BossMonster boss;
    public Slider bossHpSlider;
    public TMP_Text bossName;

    void Start()
    {
        if (bossName) bossName.text = "MINIGUN & MISSILE BOSS";
        if (boss)
        {
            boss.onHpChanged.AddListener(OnHpChanged);
            // 시작 시 초기값 반영
            OnHpChanged(Mathf.Clamp(bossHpSlider ? (int)(boss.HpRatio * boss.maxHP) : 0, 0, boss.maxHP), boss.maxHP);
        }
    }

    void OnHpChanged(int current, int max)
    {
        if (!bossHpSlider) return;
        bossHpSlider.minValue = 0;
        bossHpSlider.maxValue = max;
        bossHpSlider.value = current;
    }
}
