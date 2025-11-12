using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour, IHittable
{
    [Header("연결")]
    [Tooltip("이 히트박스가 연결된 DamageablePart")]
    public DamageablePart part;

    [Header("데미지")]
    [Tooltip("부위별 데미지 배율 (예: 머리 2.0, 장갑 0.5)")]
    public float damageMultiplier = 1f;

    [Header("피격 숫자 표시 (선택)")]
    [Tooltip("피격 데미지 숫자를 띄웁니다. CanvasManager.singleton.ShowDamage* 를 시도합니다.")]
    public bool showDamagePopup = true;
    [Tooltip("월드 좌표로 표시할 때 위치 기준 (없으면 이 히트박스 위치 사용)")]
    public Transform popupAnchor;

    [Header("궁극기(ULT) 게이지 기본")]
    [Tooltip("피격 시 ULT 게이지를 올립니다.")]
    public bool addUltOnHit = true;
    [Tooltip("히트 1회당 고정 추가량 (기존 방식 유지 옵션)")]
    public float ultPerHit = 1f;

    [Header("궁극기(ULT) 게이지 강화 옵션")]
    [Tooltip("맞은 '데미지 양'에 비례해서도 게이지를 추가합니다.")]
    public bool gaugeUsePerDamage = true;
    [Tooltip("데미지 1당 게이지 추가량 (예: 0.02면 50데미지에 +1)")]
    public float gaugePerDamage = 0.02f;
    [Tooltip("히트당 보너스(고정값)를 더해줍니다.")]
    public float gaugePerHitBonus = 0.0f;

    [Tooltip("보스라면 게이지 가중치 곱")]
    public bool isBoss = false;
    public float bossGaugeMultiplier = 1.2f;

    [Tooltip("약점(헤드샷 등) 히트박스면 가중치 곱")]
    public bool isWeakPoint = false;
    public float weakPointGaugeMultiplier = 1.5f;

    [Tooltip("한 번에 추가되는 게이지 최소/최대 제한")]
    public float minGaugeAdd = 0f;
    public float maxGaugeAdd = 5f;

    [Header("이벤트(원하면 인스펙터에서 바인딩)")]
    public UnityEvent<int> onHitDamage; // 최종 데미지를 이벤트로 알림

    public void OnHit(int damage)
    {
        if (part == null) return;

        // 1) 최종 데미지 산출
        int finalDamage = Mathf.Max(0, Mathf.RoundToInt(damage * damageMultiplier));
        // Debug.Log($"[Hitbox] part='{part.gameObject.name}' base={damage} mul={damageMultiplier} final={finalDamage}");

        // 2) 데미지 숫자 표시 (가능하면 월드 위치 버전 → 아니면 기본 버전)
        if (showDamagePopup && CanvasManager.singleton != null)
        {
            var worldPos = (popupAnchor ? popupAnchor.position : transform.position);
            var cm = CanvasManager.singleton;

            // ShowDamageAt(int, Vector3)이 있으면 우선 시도
            var mAt = cm.GetType().GetMethod(
                "ShowDamageAt",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(int), typeof(Vector3) },
                null
            );
            if (mAt != null) mAt.Invoke(cm, new object[] { finalDamage, worldPos });
            else
            {
                var m = cm.GetType().GetMethod(
                    "ShowDamage",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(int) },
                    null
                );
                if (m != null) m.Invoke(cm, new object[] { finalDamage });
            }
        }

        // 3) 궁극기 게이지 올리기
        if (addUltOnHit)
        {
            float add = 0f;

            // (1) 기존 고정 방식
            add += ultPerHit;

            // (2) 데미지 비례 방식
            if (gaugeUsePerDamage && gaugePerDamage > 0f)
                add += finalDamage * gaugePerDamage;

            // (3) 히트 보너스
            add += gaugePerHitBonus;

            // (4) 가중치(보스/약점)
            float mul = 1f;
            if (isBoss) mul *= bossGaugeMultiplier;
            if (isWeakPoint) mul *= weakPointGaugeMultiplier;

            add *= mul;
            add = Mathf.Clamp(add, minGaugeAdd, maxGaugeAdd);

            // UltimateSkill.AddGauge(float) 호출 (리플렉션)
            var ult = Object.FindObjectOfType(typeof(UltimateSkill));
            if (ult != null && add > 0f)
            {
                var addGauge = ult.GetType().GetMethod(
                    "AddGauge",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(float) },
                    null
                );
                if (addGauge != null)
                    addGauge.Invoke(ult, new object[] { add });
            }
        }

        // 4) 외부에서도 듣고 싶다면 이벤트로 송신
        if (onHitDamage != null) onHitDamage.Invoke(finalDamage);

        // 5) 실제 체력 감소 (보스 파트 → 보스 HP 반영)
        part.ApplyDamage(finalDamage);
    }

    private void Reset()
    {
        // 자동으로 트리거 제안
        var col = GetComponent<Collider>();
        col.isTrigger = true;

        // 기본 앵커를 파트로
        if (!popupAnchor) popupAnchor = transform;
    }
}
