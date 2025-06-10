using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAction_Teleport : MonoBehaviour
{
    [SerializeField] private Transform teleportPosition;
    private Transform _player;
    
    public ButtonCondition condition = ButtonCondition.None;
    
    [HideInInspector] public string itemName;
    [HideInInspector] public int itemAmount;

    private Button _myByn;
    
    private void Start()
    {
        _player = GameObject.FindWithTag("Player").transform;
        
        _myByn = GetComponent<Button>();
        _myByn.onClick.AddListener(CheckCondition);
    }

    private void CheckCondition()
    {
        switch (condition)
        {
            case ButtonCondition.None:
                Action();
                break;

            case ButtonCondition.CheckItem:
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
            case ButtonCondition.UseItem:
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

    private void Action()
    {
        var cc = _player.GetComponent<CharacterController>();

        cc.enabled = false;
        _player.position = teleportPosition.position;
        cc.enabled = true;
    }
}
