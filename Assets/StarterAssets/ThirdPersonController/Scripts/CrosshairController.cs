using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public enum Mode { Auto, SingleImageScale, FourBars }

    [Header("Mode")]
    [SerializeField] private Mode mode = Mode.Auto;

    [Header("Single Image")]
    [SerializeField] private RectTransform singleImage;
    [SerializeField] private float baseScaleAim = 0.85f;
    [SerializeField] private float baseScaleHip = 1.15f;
    [SerializeField] private float scalePerDegree = 0.03f;

    [Header("Four Bars")]
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private RectTransform leftBar;
    [SerializeField] private RectTransform rightBar;
    [SerializeField] private float baseGapAim = 10f;
    [SerializeField] private float baseGapHip = 20f;
    [SerializeField] private float pixelsPerDegree = 3.5f;

    [Header("Amplify / Curve")]
    [Tooltip("최종 UI 반영 전에 곱해줄 과장 배율")]
    [SerializeField] private float amplify = 1.6f;
    [Tooltip("입력: degrees(0~maxExpectedDegrees), 출력: 0~1 비율")]
    [SerializeField] private AnimationCurve response = AnimationCurve.EaseInOut(0, 0, 10, 1);
    [Tooltip("커브의 X축 상한(이 값 이상부터는 1로 고정 가정)")]
    [SerializeField] private float maxExpectedDegrees = 10f;

    [Header("Clamp / Smoothing / Visibility")]
    [SerializeField] private float minGapOrScale = 0.5f;
    [SerializeField] private float maxGapOrScale = 999f;
    [SerializeField] private float lerpSpeed = 12f;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float showHideLerp = 16f;

    private float _target;
    private float _current;

    private bool UseFourBars =>
        mode == Mode.FourBars || (mode == Mode.Auto && CountAssignedBars() >= 2);

    private int CountAssignedBars()
    {
        int c = 0;
        if (topBar) c++;
        if (bottomBar) c++;
        if (leftBar) c++;
        if (rightBar) c++;
        return c;
    }

    /// <summary>
    /// degrees: Weapon.VisualSpreadDeg 결과(도), aiming: 조준 여부, visible: 보이기/숨기기
    /// </summary>
    public void SetSpreadDegrees(float degrees, bool aiming, bool visible)
    {
        // Show/Hide
        if (canvasGroup != null)
        {
            float targetAlpha = visible ? 1f : 0f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, showHideLerp * Time.deltaTime);
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else gameObject.SetActive(visible);

        // 1) 커브로 비선형 과장 비율 계산
        float x = Mathf.Clamp(degrees, 0f, maxExpectedDegrees);
        float t = response.Evaluate(x);      // 0~1
        float curveAmp = Mathf.Lerp(1f, amplify, t);

        // 2) 선형 기초값 + 커브 과장 적용
        if (UseFourBars)
        {
            float baseGap = aiming ? baseGapAim : baseGapHip;
            float gap = baseGap + degrees * pixelsPerDegree * curveAmp;
            _target = Mathf.Clamp(gap, minGapOrScale, maxGapOrScale);
        }
        else
        {
            float baseScale = aiming ? baseScaleAim : baseScaleHip;
            float sc = baseScale + degrees * scalePerDegree * curveAmp;
            _target = Mathf.Clamp(sc, minGapOrScale, maxGapOrScale);
        }
    }

    private void Update()
    {
        _current = Mathf.Lerp(_current, _target, lerpSpeed * Time.deltaTime);

        if (UseFourBars) ApplyGap(_current);
        else ApplyScale(_current);
    }

    private void ApplyGap(float gap)
    {
        if (topBar) topBar.anchoredPosition = new Vector2(0, gap);
        if (bottomBar) bottomBar.anchoredPosition = new Vector2(0, -gap);
        if (leftBar) leftBar.anchoredPosition = new Vector2(-gap, 0);
        if (rightBar) rightBar.anchoredPosition = new Vector2(gap, 0);
    }

    private void ApplyScale(float scale)
    {
        if (!singleImage) return;
        singleImage.localScale = Vector3.one * Mathf.Max(0.01f, scale);
    }
}
