using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;

    private void Awake()
    {
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();

        SetRagdollState(false);
    }

    public void ActivateRagdoll()
    {
        if (animator != null) animator.enabled = false;
        SetRagdollState(true);
    }

    private void SetRagdollState(bool isActive)
    {
        foreach (var rb in ragdollBodies)
        {
            if (rb != null && rb.gameObject != this.gameObject)
                rb.isKinematic = !isActive;
        }

        foreach (var col in ragdollColliders)
        {
            if (col != null && col.gameObject != this.gameObject)
                col.enabled = isActive;
        }
    }
}
