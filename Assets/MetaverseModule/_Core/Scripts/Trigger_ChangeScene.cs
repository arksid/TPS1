using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Trigger_ChangeScene : MonoBehaviour
{
    [SerializeField] private string sceneName;

    private GameObject _interactUI;
    private bool _canInteract = false;
    
    public Condition condition = Condition.None;
    
    [HideInInspector] public string itemName;
    [HideInInspector] public int itemAmount;
    [HideInInspector] public string msg = "Press 'F'";
    
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
        SceneManager.LoadScene(sceneName);
    }
}
