using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private Transform _weaponHolder = null;

    private Weapon _weapon = null; public Weapon weapon { get { return _weapon; } }
    private List<Item> _items = new List<Item>();
    private RigManager _rigManager = null;

    private void Awake()
    {
        _rigManager = GetComponent<RigManager>();
        Initialized(new Dictionary<string, int> { { "PP-19",1 } });

    }

    public void Initialized(Dictionary<string, int> items)
    {
        if (items != null && PrefabManager.singleton != null)
        {
            int firstWeaponIndex = -1;
            foreach (var itemData in items)
            {
                Item prefab = PrefabManager.singleton.GetItemPrefab(itemData.Key);
                if (prefab != null && itemData.Value > 0)
                {
                    for (int i = 0; i < itemData.Value; i++)
                    {
                        bool done = false;
                        Item item = Instantiate(prefab, transform);

                        if (item.GetType() == typeof(Weapon))
                        {
                            Weapon w = (Weapon)item;
                            item.transform.SetParent(_weaponHolder);
                            item.transform.localPosition = w.rightHandPosition;
                            item.transform.localEulerAngles = w.rightHandRotation;
                            if (firstWeaponIndex < 0)
                            {
                                firstWeaponIndex = _items.Count;
                            }   
                        }
                        else if (item.GetType() == typeof(Ammo))
                        {
                            Ammo a = (Ammo)item;
                            a.amount = itemData.Value; 
                            done = true;

                        }
                        item.gameObject.SetActive(false);
                        _items.Add(item);
                        if (done)
                        {
                            break;
                        }
                    }
                }
            }
            if(firstWeaponIndex >=0 && _weapon == null)
            {
                EquipWeapon((Weapon)_items[firstWeaponIndex]);
            }
        }
    }

    public void EquipWeapon(Weapon weapon)
    {
        if (_weapon != null)
        {
            Holsterweapon();
        }

        // 이 줄은 _weapon을 초기화한 후이므로 다시 null이므로 조건은 필요 없음
        if (weapon.transform.parent != _weaponHolder)
        {
            weapon.transform.SetParent(_weaponHolder);
            weapon.transform.localPosition = weapon.rightHandPosition;
            weapon.transform.localEulerAngles = weapon.rightHandRotation;
        }
        _rigManager.SetLeftHandGrioData(weapon.leftHandPosition, weapon.leftHandRotation);
        weapon.gameObject.SetActive(true);
        _weapon = weapon;
    }
    //원래스크립트
   /* public void EquipWeapon(Weapon weapon)
    {
        if (_weapon != null)
        {
            Holsterweapon();
        }
        if (_weapon != null)
        {
            if (weapon.transform.parent != _weaponHolder)
            {
                weapon.transform.SetParent(_weaponHolder);
                weapon.transform.localPosition = weapon.rightHandPosition;
                weapon.transform.localEulerAngles = weapon.rightHandRotation;
            }
            _rigManager.SetLeftHandGrioData(weapon.leftHandPosition, weapon.leftHandRotation);
            weapon.gameObject.SetActive(true);
            _weapon = weapon;
        }
    }
   */

    public void Holsterweapon()
    {
        if(_weapon != null)
        {
            _weapon.gameObject.SetActive(false);
            _weapon = null;
        }
    }
}
