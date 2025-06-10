using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
public class UI_GetItem : MonoBehaviour
{
    [SerializeField] private string itemName; 
    
    private TMP_Text myText;
    void Start()
    {
        myText = GetComponent<TMP_Text>();

        UpdateItem();
        
    }

    private void OnEnable()
    {
        MetaverSystem.Instance.itemUpdated.AddListener(UpdateItem);
    }

    private void OnDisable()
    {
        MetaverSystem.Instance.itemUpdated.RemoveListener(UpdateItem);
    }

    void UpdateItem()
    {
        myText.text = MetaverSystem.Instance.GetItem(itemName).ToString();
    }
}
