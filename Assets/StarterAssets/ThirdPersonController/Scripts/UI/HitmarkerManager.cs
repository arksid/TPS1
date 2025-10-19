using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitmarkerManager : MonoBehaviour
{
    public static HitmarkerManager instance;
    public Image hitmarkerImage;
    public float displayTime = 0.1f;  // 히트마커 표시 시간

    private void Awake()
    {
        instance = this;
        if (hitmarkerImage != null)
            hitmarkerImage.enabled = false;
    }

    public void ShowHitmarker()
    {
        if (hitmarkerImage == null) return;
        StopAllCoroutines();
        StartCoroutine(ShowHitmarkerRoutine());
    }

    private IEnumerator ShowHitmarkerRoutine()
    {
        hitmarkerImage.enabled = true;
        yield return new WaitForSeconds(displayTime);
        hitmarkerImage.enabled = false;
    }
}
