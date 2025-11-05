using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SimpleWaypointUI : MonoBehaviour
{
    [Header("연결 (필수)")]
    public Camera worldCamera;              // 보통 Main Camera
    public Canvas canvas;                   // Screen Space - Overlay
    public RectTransform marker;            // 마커 아이콘(이미지 또는 빈 오브젝트)
    public TextMeshProUGUI messageLabel;    // "이 지역으로 이동해"
    public TextMeshProUGUI distanceLabel;   // "123m"

    [Header("표시 옵션")]
    public Vector2 screenMargin = new Vector2(40, 40); // 가장자리 마진
    public float hideDistanceUnder = 0.5f;             // 목표와 너무 가까우면 숨기기(미터)

    Transform _target;
    bool _active;

    public void Activate(Transform target, string message)
    {
        _target = target;
        if (messageLabel) messageLabel.text = message ?? "";
        _active = true;

        if (!worldCamera) worldCamera = Camera.main;
        if (!canvas) canvas = GetComponentInParent<Canvas>();
        if (marker) marker.gameObject.SetActive(true);
        if (distanceLabel) distanceLabel.gameObject.SetActive(true);
        if (messageLabel) messageLabel.gameObject.SetActive(true);
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        _active = false;
        if (marker) marker.gameObject.SetActive(false);
        if (distanceLabel) distanceLabel.gameObject.SetActive(false);
        if (messageLabel) messageLabel.gameObject.SetActive(false);
        // 필요 시 gameObject 자체를 끄고 싶으면 아래 주석 해제
        // gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!_active || _target == null || worldCamera == null || canvas == null) return;

        Vector3 ws = _target.position;
        Vector3 ss = worldCamera.WorldToScreenPoint(ws);

        // 거리 표시
        float dist = Vector3.Distance(worldCamera.transform.position, ws);
        if (distanceLabel) distanceLabel.text = $"{Mathf.RoundToInt(dist)}m";

        // 너무 가까우면 숨김
        if (dist < hideDistanceUnder)
        {
            if (marker) marker.gameObject.SetActive(false);
            if (distanceLabel) distanceLabel.gameObject.SetActive(false);
            return;
        }
        else
        {
            if (marker && !marker.gameObject.activeSelf) marker.gameObject.SetActive(true);
            if (distanceLabel && !distanceLabel.gameObject.activeSelf) distanceLabel.gameObject.SetActive(true);
        }

        // 스크린 밖이면 화면 가장자리로 클램프
        bool behind = ss.z < 0f;
        if (behind) ss *= -1f; // 뒤에 있으면 반전해서 가장자리로 보정

        RectTransform canvasRT = canvas.transform as RectTransform;
        Vector2 canvasSize = canvasRT.sizeDelta;
        Vector2 clamped = new Vector2(
            Mathf.Clamp(ss.x, screenMargin.x, Screen.width - screenMargin.x),
            Mathf.Clamp(ss.y, screenMargin.y, Screen.height - screenMargin.y)
        );

        // 월드 스크린 좌표 → 캔버스 좌표
        Vector2 anchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, clamped, null, out anchored);

        if (marker)
        {
            marker.anchoredPosition = anchored;

            // 화살표 회전(목표 방향 대략 표시하고 싶다면 마커에 화살표 스프라이트 사용)
            if (behind)
            {
                marker.localRotation = Quaternion.Euler(0, 0, 180);
            }
            else
            {
                marker.localRotation = Quaternion.identity;
            }
        }
    }
}
