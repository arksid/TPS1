using UnityEngine;

public class AugmentSystem : MonoBehaviour
{
    public static AugmentSystem Instance { get; private set; }

    private Character player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
        {
            player = Character.Instance;
            Debug.Log($"[AugmentSystem] Player 초기화: {player}");
        }
    }

    public void ApplyAugment(AugmentData data)
    {
        if (player == null)
        {
            player = Character.Instance;
            Debug.LogWarning("[AugmentSystem] Player가 null이라 다시 할당했습니다.");
        }

        Debug.Log("===== [AugmentSystem] 특성 적용 시작 =====");
        Debug.Log($"data.type  = {data.type}");
        Debug.Log($"data.value = {data.value}");
        Debug.Log($"Before MaxHealth = {player.MaxHealth}");
        Debug.Log($"Before Health = {player.Health}");
        
        switch (data.type)
        {
            case AugmentType.MaxShieldUp:
                player.MaxShield += (int)data.value;
                player.Shield = player.MaxShield;
                break;

            case AugmentType.CriticalChanceUp:
                player.CriticalChance += data.value;
                break;
        }

        switch (data.type)
        {
            case AugmentType.MaxHealthUp:
                Debug.Log("[AugmentSystem] ▶ MaxHealthUp 적용 중…");
                player.MaxHealth += (int)data.value;
                player.Health = player.MaxHealth;
                player.RefreshStats();
                break;

            default:
                Debug.LogWarning($"[AugmentSystem] ▶ {data.type} 타입은 현재 처리 대상이 아님");
                break;
        }

        Debug.Log($"After MaxHealth = {player.MaxHealth}");
        Debug.Log($"After Health = {player.Health}");

        if (CanvasManager.singleton == null)
        {
            CanvasManager.singleton = FindObjectOfType<CanvasManager>();
            Debug.LogWarning("[AugmentSystem] CanvasManager.singleton이 null이라 재할당함");
        }

        if (CanvasManager.singleton != null)
        {
            CanvasManager.singleton.UpdateHealth(player.Health, player.MaxHealth);
            Debug.Log("[AugmentSystem] 체력 UI 갱신 호출 완료");
        }
        else
        {
            Debug.LogError("[AugmentSystem] CanvasManager를 찾을 수 없음!");
        }

        Debug.Log("===== [AugmentSystem] 특성 적용 종료 =====");
    }

}
