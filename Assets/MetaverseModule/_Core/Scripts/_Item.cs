using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class _Item : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private int itemAmount = 1;
    
    [SerializeField] private GameObject activeTargetWhenGetItem;
    
    private GameObject _interactUI;

    private bool _canInteract = false;
    
    public ItemCondition condition = ItemCondition.None;

    private void Update()
    {
        if(!_canInteract)
            return;

        if(!Input.GetKeyDown(KeyCode.F)) 
            return;

        Action();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
            _canInteract = true;

        if(condition == ItemCondition.None)
            Action();
        else
        {
            MetaverSystem.Instance.SetInteractText("Press 'F'");
            _canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            _canInteract = false;

        if(condition == ItemCondition.None)
            Action();
        else
        {
            MetaverSystem.Instance.ResetInteractUI();
            _canInteract = false;
        }
    }

    private void Action()
    {
        if(string.IsNullOrEmpty(itemName))
            return;
        
        MetaverSystem.Instance.AddItem(itemName, itemAmount);
        gameObject.SetActive(false);
        
        MetaverSystem.Instance.ResetInteractUI();
        
        if(activeTargetWhenGetItem)
            activeTargetWhenGetItem.SetActive(true);
    }
}
