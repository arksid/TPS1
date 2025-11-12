using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UIButtonHoverColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("대상 그래픽")]
    public Image targetImage;                // 버튼 배경
    public TextMeshProUGUI targetText;       // 버튼 글자

    [Header("색상")]
    public Color normalImage = new Color(0.18f, 0.18f, 0.18f, 1f);
    public Color hoverImage = new Color(0.28f, 0.28f, 0.28f, 1f);
    public Color pressedImage = new Color(0.12f, 0.12f, 0.12f, 1f);

    public Color normalText = Color.white;
    public Color hoverText = new Color(1f, 0.85f, 0.2f, 1f);
    public Color pressedText = new Color(1f, 0.75f, 0.1f, 1f);

    [Header("옵션: 살짝 커지기")]
    public bool scaleOnHover = true;
    public float hoverScale = 1.05f;
    public float tweenSpeed = 12f;

    Vector3 _baseScale;
    bool _hover, _press;

    void Awake()
    {
        _baseScale = transform.localScale;
        if (!targetImage) targetImage = GetComponent<Image>();
        ApplyColors(true);
    }

    void Update()
    {
        if (!scaleOnHover) return;
        var target = _baseScale * (_press ? 0.97f : (_hover ? hoverScale : 1f));
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.unscaledDeltaTime * tweenSpeed);
    }

    public void OnPointerEnter(PointerEventData e) { _hover = true; ApplyColors(); }
    public void OnPointerExit(PointerEventData e) { _hover = false; _press = false; ApplyColors(); }
    public void OnPointerDown(PointerEventData e) { _press = true; ApplyColors(); }
    public void OnPointerUp(PointerEventData e) { _press = false; ApplyColors(); }

    void ApplyColors(bool normal = false)
    {
        if (targetImage) targetImage.color = normal ? normalImage : (_press ? pressedImage : (_hover ? hoverImage : normalImage));
        if (targetText) targetText.color = normal ? normalText : (_press ? pressedText : (_hover ? hoverText : normalText));
    }
}
