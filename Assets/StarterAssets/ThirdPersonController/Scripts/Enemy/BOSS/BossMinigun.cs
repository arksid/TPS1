using System.Collections;
using UnityEngine;

public class BossMinigun : MonoBehaviour
{
    [Header("기본")]
    public Transform[] muzzles;             // 총구들(좌/우 포드 등)
    public GameObject bulletPrefab;         // BossBullet 등 IHittable을 때리는 탄환
    public Transform player;                // 비워두면 tag=Player 자동 찾기

    [Header("발사 파라미터")]
    [Tooltip("분당 발사수(RPM) - 600이면 0.1초당 1발")]
    public float rpm = 900f;
    [Tooltip("한 발당 퍼짐(각도)")]
    public float spreadDeg = 2.0f;
    [Tooltip("한 트리거 당 연속 발사 시간(초). 0이면 무제한")]
    public float fireWindow = 0f;

    [Header("데미지/속도(탄환이 자체 스크립트로 이동한다면 생략 가능)")]
    public int damagePerBullet = 6;
    public float bulletSpeed = 80f;

    [Header("상태")]
    public bool autoStartOnEnable = false;
    public bool enraged = false;            // 분노 모드(발사 주기 가속)
    public string projectileTag = "EnemyProjectile";

    [Header("사운드/비주얼(선택)")]
    public AudioSource sfxLoop;
    public ParticleSystem[] muzzleFlashes;

    Coroutine _loop;
    float _localTimeScale = 1f;

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

    public void StartFiring()
    {
        if (_loop != null) return;
        if (sfxLoop) sfxLoop.Play();
        foreach (var fx in muzzleFlashes) if (fx) fx.Play();
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
        foreach (var fx in muzzleFlashes) if (fx) fx.Stop();
    }

    public void SetEnraged(bool on)
    {
        enraged = on;
    }

    public void SetLocalTimeScale(float scale)
    {
        _localTimeScale = Mathf.Clamp(scale, 0.05f, 4f);
    }

    IEnumerator Co_FireLoop()
    {
        if (muzzles == null || muzzles.Length == 0 || bulletPrefab == null)
            yield break;

        float effectiveRpm = rpm * (enraged ? 1.5f : 1f);   // 분노 시 1.5배
        float secPerShot = 60f / Mathf.Max(60f, effectiveRpm); // 최소 60RPM 가드

        float timer = 0f;
        while (true)
        {
            // fireWindow가 0이 아니면 일정 시간 후 자동 정지
            if (fireWindow > 0f)
            {
                timer += Time.deltaTime * _localTimeScale;
                if (timer >= fireWindow) { StopFiring(); yield break; }
            }

            // 모든 총구에서 1발씩
            for (int i = 0; i < muzzles.Length; i++)
            {
                var m = muzzles[i];
                if (!m) continue;

                // 조준 벡터 + 퍼짐
                Vector3 dir = (player ? (player.position + Vector3.up * 1.2f - m.position).normalized : m.forward);
                dir = ApplySpread(dir, spreadDeg);

                // 스폰
                var go = Instantiate(bulletPrefab, m.position, Quaternion.LookRotation(dir));
                if (!string.IsNullOrEmpty(projectileTag)) go.tag = projectileTag;

                // (옵션) 탄환에 초기 속도/데미지 주입
                var rb = go.GetComponent<Rigidbody>();
                if (rb) rb.velocity = dir * bulletSpeed;

                // IHittable형 BossBullet이 자체적으로 데미지를 처리한다면 생략 가능
                var hittable = go.GetComponent<IHittable>(); // 보통 탄환이 IHittable을 "때림"이지, 탄환이 IHittable은 아님
                // 필요 시 탄환 스크립트에 초기화 API가 있다면 여기서 호출

                // 머즐 플래시
                if (i < muzzleFlashes.Length && muzzleFlashes[i]) muzzleFlashes[i].Play();
            }

            // 발사 간격
            float wait = secPerShot / _localTimeScale;
            yield return new WaitForSeconds(wait);
        }
    }

    Vector3 ApplySpread(Vector3 forward, float degrees)
    {
        if (degrees <= 0.01f) return forward;
        // 무작위 축 회전으로 간단한 콘 스프레드
        Quaternion yaw = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(Random.Range(-degrees, degrees), Vector3.right);
        return (yaw * pitch) * forward;
    }
}
