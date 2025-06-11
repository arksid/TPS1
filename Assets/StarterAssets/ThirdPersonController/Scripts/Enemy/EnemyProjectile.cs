using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 10f;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.velocity = transform.forward * speed;
        Destroy(gameObject, 5f); // 일정 시간 후 제거
    }

    private void OnCollisionEnter(Collision collision)
    {
        Character player = collision.transform.GetComponentInParent<Character>();
        if (player != null)
        {
            player.ApplyDamage(null, collision.transform, damage);
        }

        Destroy(gameObject);
    }
}

