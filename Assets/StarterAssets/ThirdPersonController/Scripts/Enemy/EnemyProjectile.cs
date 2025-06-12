using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 10f;

    private void Start()
    {
        Collider[] myColliders = GetComponents<Collider>();
        Collider[] enemyColliders = FindObjectsOfType<EnemyController>().SelectMany(e => e.GetComponentsInChildren<Collider>()).ToArray();

        foreach (var myCol in myColliders)
        {
            foreach (var enemyCol in enemyColliders)
            {
                Physics.IgnoreCollision(myCol, enemyCol);
            }
        }

        GetComponent<Rigidbody>().velocity = transform.forward * speed;
        Destroy(gameObject, 5f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        Character player = collision.transform.GetComponentInParent<Character>();
        if (player != null)
        {
            player.ApplyDamage(null, collision.transform, damage);
        }

        SemiBossController boss = collision.transform.GetComponentInParent<SemiBossController>();
        if (boss != null)
        {
            boss.TakeDamage(damage);
        }

        Destroy(gameObject);
    }
}

