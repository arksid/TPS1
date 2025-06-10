using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonAction_Open : MonoBehaviour
{
    [SerializeField] private List<GameObject> target;
    
    public ButtonCondition condition = ButtonCondition.None;
    
    [HideInInspector] public string itemName;
    [HideInInspector] public int itemAmount;

    private Button _myByn;
    
    private void Start()
    {
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
        foreach (var obj in target)
        {
            if(obj == null)
                continue;
                    
            if(!obj.activeSelf)
                obj.SetActive(true);   
        }
    }
}
