using UnityEngine;

public class AmmoBox : MonoBehaviour
{
    [SerializeField] private int amountToAdd = 30;   // 한 번에 주는 탄약량
    [SerializeField] private string ammoID = "9mm";  // 어떤 탄약인지

    private void OnTriggerEnter(Collider other)
    {
        Character character = other.GetComponent<Character>();
        if (character != null && character.ammo != null)
        {
            // ✅ 캐릭터에 현재 사용하는 ammo가 있을 경우
            if (character.ammo.id == ammoID)
            {
                character.ammo.amount += amountToAdd;
                if (CanvasManager.singleton != null && character.weapon != null)
                {
                    CanvasManager.singleton.UpdateAmmo(character.weapon.ammo, character.ammo.amount);
                }
                Destroy(gameObject); // 박스 제거
            }
        }
    }
}
