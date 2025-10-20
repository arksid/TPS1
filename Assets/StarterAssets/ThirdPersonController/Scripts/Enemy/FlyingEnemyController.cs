using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class FlyingEnemyController : MonoBehaviour, ISlowable
{
    [Header("기본 설정")]
    public float moveSpeed = 10f;
    public float turnSpeed = 5f;
    public float hoverHeight = 6f;
    public float minDistance = 15f;
    public float attackRange = 25f;
    public int expReward = 20;
    public float ultimateGaugeReward = 10f;

    [Header("드랍 확률 가중치 설정 (총합 100 기준 권장)")]
    public float weaponDropWeight = 50f;
    public float healDropWeight = 30f;
    public float ammoDropWeight = 20f;
    public float noDropWeight = 0f;

    [Header("힐팩 / 탄약 프리팹")]
    public GameObject healPackPrefab;
    public GameObject ammoPackPrefab;

    [Header("무기 ID 목록 (PrefabManager 사용)")]
    public string[] weaponIDs;

    private Transform playerTarget;
    private Rigidbody rb;
    private bool isDead = false;
    private bool canShoot = true;
    private bool hasTarget = false;

    // 궁극기 관련
    private float baseMoveSpeed;
    private float baseProjectileSpeed;
    private float baseAttackCooldown;
    private float localTimeScale = 1f;

    // 이동 및 회피 관련
    private Vector3 evadeOffset;
    private float nextEvadeTime = 0f;
    private float orbitAngle = 0f;
    private int orbitDirection = 1;
    private float nextOrbitSwitch = 0f;

    [Header("공격 설정")]
    public float attackCooldown = 2f;
    public float projectileSpeed = 25f;
    public float projectileDamage = 15f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    [Header("체력")]
    public int maxHealth = 100;
    private int currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;

        baseMoveSpeed = moveSpeed;
        baseProjectileSpeed = projectileSpeed;
        baseAttackCooldown = attackCooldown;

        PickNewEvadeOffset();
        nextOrbitSwitch = Time.time + 5f;

        // 궁극기 활성화 상태면 슬로우 적용
        if (UltimateSkill.IsUltimateActive)
        {
            SetLocalTimeScale(UltimateSkill.CurrentSlowFactor);
        }
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (!hasTarget || playerTarget == null) return;

        Vector3 playerPos = new Vector3(playerTarget.position.x, hoverHeight, playerTarget.position.z);
        float distance = Vector3.Distance(transform.position, playerPos);

        if (Time.time >= nextEvadeTime) PickNewEvadeOffset();
        if (Time.time >= nextOrbitSwitch)
        {
            orbitDirection *= -1;
            nextOrbitSwitch = Time.time + 5f;
        }

        orbitAngle += 6f * orbitDirection * Time.fixedDeltaTime;
        Vector3 orbitOffset = new Vector3(Mathf.Cos(orbitAngle), 0, Mathf.Sin(orbitAngle)) * minDistance;
        Vector3 moveTarget = playerPos + orbitOffset + evadeOffset;

        Quaternion targetRot = Quaternion.LookRotation(playerPos - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);

        Vector3 dir = (moveTarget - transform.position).normalized;
        rb.velocity = dir * (moveSpeed * localTimeScale);

        if (distance <= attackRange && canShoot)
            StartCoroutine(ShootRoutine());
    }

    void PickNewEvadeOffset()
    {
        Vector2 randomCircle = Random.insideUnitCircle * 4f;
        evadeOffset = new Vector3(randomCircle.x, 0, randomCircle.y);
        nextEvadeTime = Time.time + 2f;
    }

    IEnumerator ShootRoutine()
    {
        canShoot = false;
        if (firePoint != null && projectilePrefab != null && playerTarget != null)
        {
            Vector3 shootDir = (playerTarget.position + Vector3.up * 1.2f - firePoint.position).normalized;
            GameObject bullet = Instantiate(projectilePrefab, firePoint.position, Quaternion.LookRotation(shootDir));
            Rigidbody rbBullet = bullet.GetComponent<Rigidbody>();

            if (rbBullet != null)
                rbBullet.velocity = shootDir * (projectileSpeed * localTimeScale);
        }
        yield return new WaitForSeconds(attackCooldown / localTimeScale);
        canShoot = true;
    }

    public void SetLocalTimeScale(float scale)
    {
        localTimeScale = scale;
        moveSpeed = baseMoveSpeed * localTimeScale;
        projectileSpeed = baseProjectileSpeed * localTimeScale;
        attackCooldown = baseAttackCooldown / localTimeScale;
    }

    public void ResetLocalTimeScale()
    {
        localTimeScale = 1f;
        moveSpeed = baseMoveSpeed;
        projectileSpeed = baseProjectileSpeed;
        attackCooldown = baseAttackCooldown;
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHealth -= Mathf.RoundToInt(dmg);
        if (CanvasManager.singleton != null)
            CanvasManager.singleton.ShowDamage(dmg);

        if (currentHealth <= 0) Die();
    }

    void Die()
{
    if (isDead) return;
    isDead = true;

    if (PlayerLevelSystem.Instance != null)
        PlayerLevelSystem.Instance.AddExp(expReward);

    var ult = FindObjectOfType<UltimateSkill>();
    if (ult != null)
        ult.AddGauge(ultimateGaugeReward);

    // 💥 공통 드랍 시스템 호출
    var dropSystem = GetComponent<EnemyDropSystem>();
    if (dropSystem != null) dropSystem.DropWeapon();

    Destroy(gameObject, 0.1f);
}

    private void TryDropItemByWeight()
    {
        float totalWeight = weaponDropWeight + healDropWeight + ammoDropWeight + noDropWeight;
        if (totalWeight <= 0f) return;

        float roll = Random.Range(0f, totalWeight);

        if (roll < weaponDropWeight)
        {
            DropWeapon();
        }
        else if (roll < weaponDropWeight + healDropWeight)
        {
            DropHeal();
        }
        else if (roll < weaponDropWeight + healDropWeight + ammoDropWeight)
        {
            DropAmmo();
        }
        else
        {
            Debug.Log("[FlyingEnemy] 아무것도 드랍되지 않음");
        }
    }

    private void DropWeapon()
    {
        var manager = PrefabManager.singleton;
        if (manager == null)
        {
            Debug.LogWarning("❌ PrefabManager를 찾을 수 없습니다.");
            return;
        }

        var items = manager._items;
        if (items == null || items.Length == 0)
        {
            Debug.LogWarning("[FlyingEnemy] PrefabManager에 아이템이 없습니다.");
            return;
        }

        // 🧠 현재 프리팹 이름 (자기 자신)
        string currentPrefabName = this.gameObject.name;

        var filteredList = new System.Collections.Generic.List<Item>();
        foreach (var item in items)
        {
            if (item == null) continue;

            // 자기 자신 프리팹만 제외
            if (item.gameObject.name == currentPrefabName)
                continue;

            filteredList.Add(item);
        }

        if (filteredList.Count == 0)
        {
            Debug.LogWarning("[FlyingEnemy] 자기 자신을 제외한 무기가 없습니다.");
            return;
        }

        // 랜덤으로 하나 선택
        int randIndex = Random.Range(0, filteredList.Count);
        Item selected = filteredList[randIndex];

        // 드랍
        Instantiate(selected.gameObject, transform.position, Quaternion.identity);
        Debug.Log($"[FlyingEnemy] 자기 자신 제외 후 무기 랜덤 드랍: {selected.name}");
    }




    private void DropHeal()
    {
        if (healPackPrefab != null)
        {
            Instantiate(healPackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[FlyingEnemy] 힐팩 드랍");
        }
    }

    private void DropAmmo()
    {
        if (ammoPackPrefab != null)
        {
            Instantiate(ammoPackPrefab, transform.position, Quaternion.identity);
            Debug.Log("[FlyingEnemy] 탄약팩 드랍");
        }
    }

    public void SetTarget(Transform playerTransform)
    {
        playerTarget = playerTransform;
        hasTarget = true;
    }
}
