using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class InteractableWeapon : MonoBehaviour, IInteractable
{
    [Header("무기 정보")]
    [SerializeField] private Weapon weaponPrefab; // 장착할 무기 프리팹
    [SerializeField] private string weaponName = "Unknown Weapon";

    [Header("시각 효과")]
    [SerializeField] private GameObject uiCanvas;   // [E] 텍스트용 Canvas
    [SerializeField] private TMP_Text uiText;       // TextMeshPro 텍스트
    [SerializeField] private Outline outline;       // 외곽선 컴포넌트

    private void Awake()
    {
        // 무기 프리팹 자동 감지
        if (weaponPrefab == null)
            weaponPrefab = GetComponent<Weapon>();

        // UI 기본 비활성화
        if (uiCanvas != null)
            uiCanvas.SetActive(false);

        // Outline 비활성화
        if (outline != null)
            outline.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character != null)
        {
            character.SetNearbyWeapon(this);

            // UI 표시
            if (uiCanvas != null)
            {
                uiCanvas.SetActive(true);
                if (uiText != null)
                    uiText.text = $"[E] {weaponName} 줍기";
            }

            // 외곽선 효과 켜기
            if (outline != null)
                outline.enabled = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var character = other.GetComponent<Character>();
        if (character != null)
        {
            character.ClearNearbyWeapon(this);

            // UI 숨기기
            if (uiCanvas != null)
                uiCanvas.SetActive(false);

            // 외곽선 효과 끄기
            if (outline != null)
                outline.enabled = false;
        }
    }

    // ✅ 실제 상호작용(줍기/교체)
    public void Interact(Character character)
    {
        if (character == null || weaponPrefab == null) return;

        int slot = character.GetCurrentSlotIndex();
        character.ReplaceWeaponInSlot(slot, weaponPrefab);

        // 줍기 후 UI/Outline 정리
        if (uiCanvas != null)
            uiCanvas.SetActive(false);
        if (outline != null)
            outline.enabled = false;

        // 무기 제거 (씬에서 없어짐)
        Destroy(gameObject);
    }
}
