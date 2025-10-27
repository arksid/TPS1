using UnityEngine;
using System.Collections;

public class AutoDespawn : MonoBehaviour
{
    [Tooltip("이 시간이 지나면 오브젝트를 삭제합니다(초).")]
    public float lifetime = 30f;

    [Tooltip("삭제 직전에 깜빡임 등 경고 시간을 줄 수 있습니다(초). 0이면 없음.")]
    public float warnBefore = 3f;

    [Tooltip("경고 중 깜빡임 주기(초).")]
    public float blinkInterval = 0.2f;

    Renderer[] rends;
    bool pickedUp = false; // 플레이어가 주웠다면 true로 만들어 파괴 중단

    void Awake()
    {
        rends = GetComponentsInChildren<Renderer>(true);
    }

    void OnEnable()
    {
        StartCoroutine(CoDespawn());
    }

    public void Cancel() // 아이템을 습득했을 때 외부에서 호출
    {
        pickedUp = true;
        StopAllCoroutines();
    }

    IEnumerator CoDespawn()
    {
        // 경고 시간 제외 대기
        float wait = Mathf.Max(0f, lifetime - warnBefore);
        float t = 0f;
        while (t < wait)
        {
            if (pickedUp) yield break;
            t += Time.unscaledDeltaTime; // 일시정지에 영향받지 않게 UI 타이밍처럼 처리
            yield return null;
        }

        // 경고(깜빡임)
        if (warnBefore > 0f && blinkInterval > 0f)
        {
            float remain = warnBefore;
            bool on = true;
            while (remain > 0f)
            {
                if (pickedUp) yield break;

                on = !on;
                SetRenderersEnabled(on);
                yield return new WaitForSeconds(blinkInterval);
                remain -= blinkInterval;
            }
        }

        // 최종 삭제
        Destroy(gameObject);
    }

    void SetRenderersEnabled(bool enabled)
    {
        if (rends == null) return;
        for (int i = 0; i < rends.Length; i++)
            if (rends[i] != null) rends[i].enabled = enabled;
    }
}
