using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player; // 추적할 대상
    public float moveSpeed = 3f;

    void Update()
    {
        if (player != null)
        {
            // 플레이어를 향해 이동
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    // 필요하다면 충돌 처리
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // 예: 플레이어에게 데미지 주기
            // PlayerHealth playerHp = collision.gameObject.GetComponent<PlayerHealth>();
            // if (playerHp != null) playerHp.TakeDamage(10);
        }
    }
}

