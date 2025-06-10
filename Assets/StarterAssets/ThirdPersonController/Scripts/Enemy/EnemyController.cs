using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public Transform player; // ������ ���
    public float moveSpeed = 3f;

    void Update()
    {
        if (player != null)
        {
            // �÷��̾ ���� �̵�
            Vector3 direction = (player.position - transform.position).normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }

    // �ʿ��ϴٸ� �浹 ó��
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // ��: �÷��̾�� ������ �ֱ�
            // PlayerHealth playerHp = collision.gameObject.GetComponent<PlayerHealth>();
            // if (playerHp != null) playerHp.TakeDamage(10);
        }
    }
}

