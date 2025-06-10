using System;
using System.Collections;
using System.Collections.Generic;
using StarterAssets;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class MetaverSystem : MonoBehaviour
{
    private static MetaverSystem _instance;
    public static MetaverSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("MetaverSystem");
                _instance = go.AddComponent<MetaverSystem>();
                DontDestroyOnLoad(_instance);
            }

            return _instance;
        }
    }

    private Text _interactUI;
    private Text InteractUI
    {
        get
        {
            if (_interactUI == null)
            {
                var ui = Resources.Load<GameObject>("UIInteract");
                _interactUI = Instantiate(ui).GetComponentInChildren<Text>();
            }

            return _interactUI;
        }
    }

    public void SetInteractText(string message, float time = 0)
    {
        CancelInvoke();
        InteractUI.text = message;
        SetInteractActive(true);
        
        if(time != 0)
            Invoke(nameof(ResetInteractUI), time);
    }

    public void SetInteractActive(bool active)
    {
        InteractUI.gameObject.SetActive(active);
    }
    
    public void ResetInteractUI()
    {
        InteractUI.text = "";
        SetInteractActive(false);
    }

    private SerializedDictionary<string, int> items = new SerializedDictionary<string, int>();

    public UnityEvent itemUpdated = new UnityEvent();

    public void AddItem(string itemName, int itemAmount)
    {
        if (items.ContainsKey(itemName))
            items[itemName] += itemAmount;
        else
            items.Add(itemName, itemAmount);
        
        if(itemUpdated != null)
            itemUpdated.Invoke();
    }

    public void UseItem(string itemName, int itemAmount)
    {
        if (items.ContainsKey(itemName))
            items[itemName] -= itemAmount;
        
        if(itemUpdated != null)
            itemUpdated.Invoke();
    }
    
    public int GetItem(string itemName)
    {
        if (!items.ContainsKey(itemName))
            return 0;
        
        return items[itemName];
    }


    private StarterAssetsInputs starterInput = null;

    private StarterAssetsInputs StarterInput
    {
        get
        {
            if (starterInput == null)
            {
                starterInput = FindObjectOfType<StarterAssetsInputs>();
                
                //starterInput.cursorLocked = false;
            }

            return starterInput;
        }
    }

    private void Update()
    {
        if(StarterInput == null)
            return;

        if(Input.GetKeyDown(KeyCode.LeftControl))
            SetCursor(true);

        if (Input.GetKeyUp(KeyCode.LeftControl))
            SetCursor(false);
    }

    public void SetCursor(bool active)
    {
        if (active)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            StarterInput.cursorInputForLook = false;
            StarterInput.cursorLocked = false;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            StarterInput.cursorInputForLook = true;
            StarterInput.cursorLocked = true; 
        }
    }

    private List<GameObject> cursorObjs = new List<GameObject>();

    public void AddCursorObj(GameObject obj)
    {
        cursorObjs.Add(obj);
        SetCursor(true);
    }

    public void RemoveCursorObj(GameObject obj)
    {
        if(cursorObjs.Contains(obj))
            cursorObjs.Remove(obj);

        for (int i = cursorObjs.Count - 1; i >= 0 ; i--)
        {
            if (cursorObjs[i] == null)
                cursorObjs.RemoveAt(i);
        }

        if(cursorObjs.Count == 0)
            SetCursor(false);
    }
}

public enum Condition
{
    None,
    Interact,
    CheckItem,
    UseItem
}


public enum ItemCondition
{
    None,
    Interact
}

public enum ButtonCondition
{
    None,
    CheckItem,
    UseItem
}