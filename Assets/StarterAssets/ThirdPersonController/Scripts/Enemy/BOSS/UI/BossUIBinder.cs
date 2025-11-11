using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossUIBinder : MonoBehaviour
{
    public static BossUIBinder Instance { get; private set; }

    [Header("UI Refs")]
    public Slider bossHpSlider;
    public TMP_Text bossName;
    public CanvasGroup canvasGroup; // 있으면 페이드에 사용(없어도 OK)

    [Header("Options")]
    public string defaultBossName = "BOSS";
    public bool startHidden = true;

    BossMonster _boss;

    void Awake()
    {
        Instance = this;
        if (!bossHpSlider) bossHpSlider = GetComponentInChildren<Slider>(true);
        if (!bossName) bossName = GetComponentInChildren<TMP_Text>(true);
        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (startHidden) InstantHide();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Unbind();
    }

    // ===== 외부에서 호출 =====
    public void ShowFor(BossMonster boss, string displayName = null)
    {
        if (!boss) return;

        Unbind();
        _boss = boss;

        // UI 활성화
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (canvasGroup) canvasGroup.alpha = 1f;

        // 텍스트/슬라이더 초기화
        if (bossName) bossName.text = string.IsNullOrEmpty(displayName) ? defaultBossName : displayName;
        if (bossHpSlider)
        {
            bossHpSlider.minValue = 0;
            bossHpSlider.maxValue = boss.maxHP;
            bossHpSlider.value = boss.CurrentHP;
        }

        // 이벤트 구독
        _boss.onHpChanged.AddListener(OnHpChanged);
        _boss.onBossDead.AddListener(OnBossDead);

        Debug.Log("[BossUIBinder] UI bound to boss: " + boss.name);
    }

    public void Hide()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        Unbind();
    }

    // ===== 내부 =====
    void Unbind()
    {
        if (_boss)
        {
            _boss.onHpChanged.RemoveListener(OnHpChanged);
            _boss.onBossDead.RemoveListener(OnBossDead);
            _boss = null;
        }
    }

    void InstantHide()
    {
        if (canvasGroup) canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }

    void OnHpChanged(int current, int max)
    {
        if (!bossHpSlider) return;
        bossHpSlider.maxValue = max;
        bossHpSlider.value = current;
    }

    void OnBossDead()
    {
        Hide();
    }
}
