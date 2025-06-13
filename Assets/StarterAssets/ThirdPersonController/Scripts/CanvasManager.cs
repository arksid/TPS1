using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    public GameObject aimUI;

    public static CanvasManager singleton;

    private void Awake()
    {
        singleton = this;
    }

    public void HideAimUI()
    {
        if (aimUI != null)
            aimUI.SetActive(false);
    }
    public void ShowAimUI()
    {
        if (aimUI != null)
            aimUI.SetActive(true);
    }
}