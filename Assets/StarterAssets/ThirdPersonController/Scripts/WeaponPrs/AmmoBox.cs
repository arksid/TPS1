using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [SerializeField] private int amountToAdd = 30;
    [SerializeField] private string ammoID = "9mm";

    private void OnEnable()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.RegisterAmmoBox(transform);
    }

    private void OnDisable()
    {
        if (RadarManager.Instance != null)
            RadarManager.Instance.UnregisterTarget(transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null && character.ammo != null)
        {
            if (character.ammo.id == ammoID)
            {
                character.ammo.amount += amountToAdd;
                if (CanvasManager.singleton != null && character.weapon != null)
                {
                    CanvasManager.singleton.UpdateAmmo(character.weapon.ammo, character.ammo.amount);
                }
                Destroy(gameObject);
            }
        }
    }
}
