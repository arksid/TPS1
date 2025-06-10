using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAction_AddItem : MonoBehaviour
{
    [SerializeField] private string itemName;
    [SerializeField] private int itemAmount = 1;
    
    [SerializeField] private GameObject activeTargetWhenGetItem;
    
    public ButtonCondition condition = ButtonCondition.None;
    
    [HideInInspector] public string needItemName;
    [HideInInspector] public int needItemAmount;

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
                if (string.IsNullOrEmpty(needItemName))
                    return;
                
                var amount = MetaverSystem.Instance.GetItem(needItemName);
                if (amount < needItemAmount)
                {
                    MetaverSystem.Instance.SetInteractText($"{needItemName}이(가) {needItemAmount} 필요합니다. (보유:{amount})", 2);
                    return;
                }
                Action();
            }
                break;
            case ButtonCondition.UseItem:
            {
                if (string.IsNullOrEmpty(needItemName))
                    return;
                
                var amount = MetaverSystem.Instance.GetItem(needItemName);
                if (amount < needItemAmount)
                {
                    MetaverSystem.Instance.SetInteractText($"{needItemName}이(가) {needItemAmount} 필요합니다. (보유:{amount})", 2);
                    return;
                }
                MetaverSystem.Instance.UseItem(needItemName, needItemAmount);
                Action();
            }   
                break;
        }
    }

    private void Action()
    {
        MetaverSystem.Instance.AddItem(itemName, itemAmount);
        
        if(activeTargetWhenGetItem)
            activeTargetWhenGetItem.SetActive(true);
    }
}
