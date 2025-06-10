using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonAction_Switch : MonoBehaviour 
{
    [SerializeField] private List<GameObject> openTarget;
    [SerializeField] private List<GameObject> closeTarget;

    public ButtonCondition condition = ButtonCondition.None;

    [HideInInspector] public string itemName;
    [HideInInspector] public int itemAmount;

    private Button _myBtn;

    private void Start()
    {
        _myBtn = GetComponent<Button>();
        _myBtn.onClick.AddListener(CheckCondition);
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