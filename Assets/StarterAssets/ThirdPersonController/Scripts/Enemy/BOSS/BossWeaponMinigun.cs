using System.Collections;
using UnityEngine;

public class BossWeaponMinigun : MonoBehaviour, ISlowable
{
    [Header("참조")]
    public Transform player;                    // 비워두면 Tag=Player 자동 탐색
    public Transform muzzle;                    // 단일 총구(써도 되고)
    public Transform[] muzzles;                 // 다중 총구(여러 개면 여기에 넣기)
    public GameObject enemyProjectilePrefab;    // ← 반드시 EnemyProjectile 프리팹 지정!

    [Header("발사 파라미터")]
    [Tooltip("분당 발사수(RPM)")]
    public float rpm = 900f;
    [Tooltip("한 발 당 퍼짐 각도(도)")]
    public float spreadDeg = 2.0f;
    [Tooltip("탄 속도")]
    public float bulletSpeed = 80f;
    [Tooltip("탄 데미지")]
    public float bulletDamage = 6f;
    [Tooltip("연속 발사 모드(켜면 무한 연사)")]
    public bool continuousFire = true;
    [Tooltip("버스트 모드: 몇 발 쏠지(continuousFire=false 일 때만 사용)")]
    public int burstCount = 20;
    [Tooltip("버스트 간 휴식(초)")]
    public float burstRest = 0.2f;
    [Tooltip("분노 모드 배율(RPM 가속)")]
    public float enragedRpmMultiplier = 1.5f;
    [Tooltip("스폰 시 총구 앞으로 밀어낼 거리(관통/자기충돌 방지)")]
    public float spawnForwardOffset = 1.5f;
    [Tooltip("머리쪽 살짝 조준 보정(미세 상향)")]
    public float aimOffsetY = 1.2f;

    [Header("동작 옵션")]
    public bool autoStartOnEnable = false;
    public bool enraged = false;                // 페이즈에서 true로 바꾸면 RPM↑

    [Header("연출(선택)")]
    public AudioSource sfxLoop;
    public ParticleSystem[] muzzleFlashes;

    // ISlowable용(궁극기 슬로우 등)
    float _localTimeScale = 1f;

    Coroutine _loop;

    void Start()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
        if (autoStartOnEnable) StartFiring();
    }

    void OnDisable()
    {
        StopFiring();
    }

    // ===== 외부 제어 API =====
    public void StartFiring()
    {
        if (_loop != null) return;
        if (!enemyProjectilePrefab)
        {
            Debug.LogWarning("[BossWeaponMinigun] enemyProjectilePrefab 미지정");
            return;
        }
        if (sfxLoop) sfxLoop.Play();
        PlayMuzzleFx(true);
        _loop = StartCoroutine(Co_FireLoop());
    }

    public void StopFiring()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
        if (sfxLoop) sfxLoop.Stop();
        PlayMuzzleFx(false);
    }

    public void SetEnraged(bool on) => enraged = on;

    // 궁극기 슬로우 등 외부 시간 배율
    public void SetLocalTimeScale(float scale)
    {
        _localTimeScale = Mathf.Clamp(scale, 0.05f, 4f);
    }

    // ===== 내부 루프 =====
    IEnumerator Co_FireLoop()
    {
        var mouths = GetMuzzles();
        if (mouths.Length == 0)
        {
            Debug.LogWarning("[BossWeaponMinigun] 총구가 없습니다(muzzle/muzzles).");
            yield break;
        }

        while (true)
        {
            // 유효 RPM 계산(분노/슬로우 반영)
            float effectiveRpm = rpm * (enraged ? enragedRpmMultiplier : 1f);
            effectiveRpm = Mathf.Max(60f, effectiveRpm); // 최소 60RPM 가드
            float secPerShot = 60f / effectiveRpm;

            if (continuousFire)
            {
                // 무한 연사
                FireOnce(mouths);
                yield return new WaitForSeconds(secPerShot / _localTimeScale);
            }
            else
            {
                // 버스트 n발 → 휴식
                for (int i = 0; i < Mathf.Max(1, burstCount); i++)
                {
                    FireOnce(mouths);
                    yield return new WaitForSeconds(secPerShot / _localTimeScale);
                }
                yield return new WaitForSeconds(burstRest / _localTimeScale);
            }
        }
    }

    void FireOnce(Transform[] mouths)
    {
        var t = ResolveTarget();
        for (int i = 0; i < mouths.Length; i++)
        {
            var m = mouths[i];
            if (!m) continue;

            // 조준 방향 + 퍼짐
            Vector3 dir = (t ? (t.position + Vector3.up * aimOffsetY - m.position).normalized : m.forward);
            dir = ApplySpread(dir, spreadDeg);

            // 스폰 위치(총구 앞)
            Vector3 spawnPos = m.position + dir * spawnForwardOffset;
            Quaternion rot = Quaternion.LookRotation(dir);

            var go = Instantiate(enemyProjectilePrefab, spawnPos, rot);
            // 잔류 정리용 태그(프로젝트에서 사용 중이면 꼭 유지)
            go.tag = "EnemyProjectile";

            // EnemyProjectile 초기화(발사자/방향/속도/데미지)
            var ep = go.GetComponent<EnemyProjectile>();
            if (ep != null)
            {
                ep.Init(shooter: this.gameObject, direction: dir, speed: bulletSpeed, damage: bulletDamage);
            }

            // 머즐 플래시
            if (i < muzzleFlashes.Length && muzzleFlashes[i]) muzzleFlashes[i].Play();
        }
    }

    // ===== 유틸 =====
    Transform[] GetMuzzles()
    {
        if (muzzles != null && muzzles.Length > 0) return muzzles;
        if (muzzle) return new[] { muzzle };
        return new Transform[0];
    }

    Vector3 ApplySpread(Vector3 forward, float degrees)
    {
        if (degrees <= 0.01f) return forward;
        // 수평/수직의 작은 무작위 회전 두 번
        Quaternion yaw = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.right);
        return (yaw * pitch) * forward;
    }

    Transform ResolveTarget()
    {
        if (player) return player;
        var p = GameObject.FindGameObjectWithTag("Player");
        return p ? p.transform : null;
    }

    void PlayMuzzleFx(bool on)
    {
        if (muzzleFlashes == null) return;
        foreach (var fx in muzzleFlashes)
        {
            if (!fx) continue;
            if (on) fx.Play(); else fx.Stop();
        }
    }
}
