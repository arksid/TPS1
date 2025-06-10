using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trigger_Switch : MonoBehaviour
{
    [SerializeField] private List<GameObject> openTarget;
    [SerializeField] private List<GameObject> closeTarget;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Action();
    }

    private void Action()
    {
        foreach (var obj in openTarget)
        {
            if (obj == null)
                continue;

            if (!obj.activeSelf)
                obj.SetActive(true);
        }

        foreach (var obj in closeTarget)
        {
            if (obj == null)
                continue;

            if (obj.activeSelf)
                obj.SetActive(false);
        }
    }
}
