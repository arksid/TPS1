using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Header("General")]

    [SerializeField] private string _id = ""; public string Id { get { return _id; } }
}
