using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // Keyboard.current
#endif

/// <summary>
/// 숫자키로 AugmentData를 즉시 적용/해제하는 테스트 도구.
/// - 1~9, 0, -, = : 적용
/// - Shift + (같은 키) : 해제
/// - F6 : StatModifierManager 리셋
/// - K : 처치 이벤트(HealOnKill 확인)
/// - H : 명중 이벤트(Ult 충전 확인)
/// </summary>
public class AugmentQuickTester : MonoBehaviour
{
    [Tooltip("빠르게 적용/해제할 AugmentData 리스트. 1~9,0,-,= 순서로 매핑됨")]
    public List<AugmentData> augments = new List<AugmentData>();

    public KeyCode resetAllKey = KeyCode.F6;
    public KeyCode simulateKillKey = KeyCode.K;
    public KeyCode simulateHitKey = KeyCode.H;

    void Start()
    {
        if (AugmentSystem.Instance == null)
            Debug.LogWarning("[QuickTester] AugmentSystem.Instance 가 null 입니다. 씬에 AugmentSystem 오브젝트를 배치하세요.");

        if (StatModifierManager.Instance == null)
            Debug.LogWarning("[QuickTester] StatModifierManager.Instance 가 null 입니다. 씬에 StatModifierManager 오브젝트를 배치하세요.");

        if (augments == null || augments.Count == 0)
            Debug.LogWarning("[QuickTester] augments 리스트가 비었습니다. 테스트할 AugmentData SO를 드래그해서 넣어주세요.");
    }

    void Update()
    {
        // 리셋/모의 이벤트 키
        if (Input.GetKeyDown(resetAllKey))
        {
            StatModifierManager.Instance?.ResetAll();
            Debug.Log("[QuickTester] ResetAll()");
        }
        if (Input.GetKeyDown(simulateKillKey))
        {
            StatModifierManager.Instance?.OnEnemyKilled();
            Debug.Log("[QuickTester] OnEnemyKilled()");
        }
        if (Input.GetKeyDown(simulateHitKey))
        {
            StatModifierManager.Instance?.OnPlayerHitEnemy();
            Debug.Log("[QuickTester] OnPlayerHitEnemy()");
        }

        // 숫자키 → 인덱스 계산
        int index = GetPressedIndex();
        if (index < 0) return;

        if (index >= augments.Count)
        {
            Debug.LogWarning($"[QuickTester] {index + 1} 슬롯에 매핑된 AugmentData 가 없습니다. (augments.Count={augments.Count})");
            return;
        }

        var data = augments[index];
        if (data == null)
        {
            Debug.LogWarning($"[QuickTester] {index + 1} 슬롯 AugmentData 가 null 입니다.");
            return;
        }

        bool remove = IsShiftPressed();
        if (remove)
        {
            AugmentSystem.Instance?.RemoveAugment(data);
            Debug.Log($"[QuickTester] REMOVE {index + 1}: {data.augmentName}");
        }
        else
        {
            AugmentSystem.Instance?.ApplyAugment(data);
            Debug.Log($"[QuickTester] APPLY {index + 1}: {data.augmentName}");
        }
    }

    /// <summary>Shift 눌림 여부(좌/우 모두)</summary>
    bool IsShiftPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
            return (kb.leftShiftKey?.isPressed ?? false) || (kb.rightShiftKey?.isPressed ?? false);
#endif
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    /// <summary>1~9,0,-,= 키 → 0~11 인덱스 반환. 누른 게 없으면 -1.</summary>
    int GetPressedIndex()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb != null)
        {
            // 상단 숫자키 또는 넘패드 둘 다 지원 (null 안전)
            if ((kb.digit1Key?.wasPressedThisFrame ?? false) || (kb.numpad1Key?.wasPressedThisFrame ?? false)) return 0;
            if ((kb.digit2Key?.wasPressedThisFrame ?? false) || (kb.numpad2Key?.wasPressedThisFrame ?? false)) return 1;
            if ((kb.digit3Key?.wasPressedThisFrame ?? false) || (kb.numpad3Key?.wasPressedThisFrame ?? false)) return 2;
            if ((kb.digit4Key?.wasPressedThisFrame ?? false) || (kb.numpad4Key?.wasPressedThisFrame ?? false)) return 3;
            if ((kb.digit5Key?.wasPressedThisFrame ?? false) || (kb.numpad5Key?.wasPressedThisFrame ?? false)) return 4;
            if ((kb.digit6Key?.wasPressedThisFrame ?? false) || (kb.numpad6Key?.wasPressedThisFrame ?? false)) return 5;
            if ((kb.digit7Key?.wasPressedThisFrame ?? false) || (kb.numpad7Key?.wasPressedThisFrame ?? false)) return 6;
            if ((kb.digit8Key?.wasPressedThisFrame ?? false) || (kb.numpad8Key?.wasPressedThisFrame ?? false)) return 7;
            if ((kb.digit9Key?.wasPressedThisFrame ?? false) || (kb.numpad9Key?.wasPressedThisFrame ?? false)) return 8;
            if ((kb.digit0Key?.wasPressedThisFrame ?? false) || (kb.numpad0Key?.wasPressedThisFrame ?? false)) return 9;
            if (kb.minusKey != null && kb.minusKey.wasPressedThisFrame) return 10;
            if (kb.equalsKey != null && kb.equalsKey.wasPressedThisFrame) return 11;
        }
#endif
        // 기존 입력 시스템(백업)
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha4)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha5)) return 4;
        if (Input.GetKeyDown(KeyCode.Alpha6)) return 5;
        if (Input.GetKeyDown(KeyCode.Alpha7)) return 6;
        if (Input.GetKeyDown(KeyCode.Alpha8)) return 7;
        if (Input.GetKeyDown(KeyCode.Alpha9)) return 8;
        if (Input.GetKeyDown(KeyCode.Alpha0)) return 9;
        if (Input.GetKeyDown(KeyCode.Minus)) return 10;
        if (Input.GetKeyDown(KeyCode.Equals)) return 11;

        return -1;
    }
}
