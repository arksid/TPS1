using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ExitAction
{
    Open, 
    Close
}

public class Trigger_Exit : MonoBehaviour
{
    [SerializeField] private List<GameObject> target;
    [SerializeField] private ExitAction exitAction;

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Action();
    }

    private void Action()
    {
        foreach (var obj in target)
        {
            if (obj == null)
                continue;

            obj.SetActive(exitAction == ExitAction.Open);
        }
    }
}
