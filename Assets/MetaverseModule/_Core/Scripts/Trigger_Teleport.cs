using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;

public class Trigger_Teleport : MonoBehaviour
{
    [SerializeField] private Transform teleportPosition;

    private Transform _player;

    private bool _canInteract = false;

    
    public Condition condition = Condition.None;
    
    [HideInInspector] public string itemName;
    [HideInInspector] public int itemAmount;
    [HideInInspector] public string msg = "Press 'F'";
    
    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
    }

    private void Update()
    {
        if(!_canInteract)
            return;

        if(!Input.GetKeyDown(KeyCode.F)) 
            return;

        
        switch (condition)
        {
            case Condition.Interact:
                Action();
                break;

            case Condition.CheckItem:
            {
                if (string.IsNullOrEmpty(itemName))
                    return;
                
                var amount = MetaverSystem.Instance.GetItem(itemName);
                if (amount < itemAmount)
                {
                    MetaverSystem.Instance.SetInteractText($"{itemName}이(가) {itemAmount} 필요합니다. (보유:{amount})", 2);
                    return;
                }
                Action();
            }
                break;
            case Condition.UseItem:
            {
                if (string.IsNullOrEmpty(itemName))
                    return;
                
                var amount = MetaverSystem.Instance.GetItem(itemName);
                if (amount < itemAmount)
                {
                    MetaverSystem.Instance.SetInteractText($"{itemName}이(가) {itemAmount} 필요합니다. (보유:{amount})", 2);
                    return;
                }

                MetaverSystem.Instance.UseItem(itemName, itemAmount);
                Action();
            }   
                break;
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!other.CompareTag("Player"))
            return;

        if(condition == Condition.None)
            Action();
        else
        {
            MetaverSystem.Instance.SetInteractText(msg);
            _canInteract = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(!other.CompareTag("Player"))
            return;
        
        _canInteract = false;
        MetaverSystem.Instance.ResetInteractUI();
    }

    private void Action()
    {
        var cc = _player.GetComponent<CharacterController>();

        cc.enabled = false;
        _player.position = teleportPosition.position;
        cc.enabled = true;
        _canInteract = false;
        
        MetaverSystem.Instance.ResetInteractUI();
    }
}
