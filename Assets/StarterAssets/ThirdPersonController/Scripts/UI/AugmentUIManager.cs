using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AugmentUIManager : MonoBehaviour
{
    public static AugmentUIManager Instance;

    [Header("UI References")]
    public GameObject augmentPanel;             // 전체 패널
    public Transform optionParent;              // 옵션이 배치될 부모
    public GameObject augmentOptionPrefab;      // 버튼 프리팹

    [Header("Augment Pool")]
    public List<AugmentData> allAugments = new List<AugmentData>();

    private List<AugmentData> currentChoices = new List<AugmentData>();

    private void Awake()
    {
        Instance = this;
        augmentPanel.SetActive(false);
    }

    public void ShowAugmentOptions()
    {
        Time.timeScale = 0f;
        Weapon.IsPaused = true;
        Time.timeScale = 0f;

        // 🖱️ 마우스 커서 보이게 하기
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        augmentPanel.SetActive(true);
        // 🔸 기존에 생성된 버튼 제거
        foreach (Transform child in optionParent)
            Destroy(child.gameObject);

        currentChoices.Clear();

        List<AugmentData> tempList = new List<AugmentData>(allAugments);

        float spacingX = 300f;     // 버튼 간격
        int totalCount = 3;        // 증강 버튼 수
        float centerOffset = (totalCount - 1) * spacingX / 2f; // 가운데 기준으로 이동값 계산

        for (int i = 0; i < totalCount && tempList.Count > 0; i++)
        {
            int index = Random.Range(0, tempList.Count);
            AugmentData picked = tempList[index];
            currentChoices.Add(picked);
            tempList.RemoveAt(index);

            GameObject buttonObj = Instantiate(augmentOptionPrefab, optionParent);

            // ✅ 가운데(2번째)를 기준으로 좌우로 퍼지게 정렬
            RectTransform rect = buttonObj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(i * spacingX - centerOffset, 0);

            // UI 텍스트 및 아이콘 세팅
            buttonObj.transform.Find("Name").GetComponent<TMPro.TextMeshProUGUI>().text = picked.augmentName;
            buttonObj.transform.Find("Desc").GetComponent<TMPro.TextMeshProUGUI>().text = picked.description;
            buttonObj.transform.Find("Icon").GetComponent<UnityEngine.UI.Image>().sprite = picked.icon;

            UnityEngine.UI.Button btn = buttonObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.AddListener(() => OnAugmentSelected(picked));
        }

        augmentPanel.SetActive(true);
    }


    private void OnAugmentSelected(AugmentData data)
    {

        // UI 닫기
        augmentPanel.SetActive(false);
        AugmentSystem.Instance.ApplyAugment(data);
        augmentPanel.SetActive(false);
        Weapon.IsPaused = false;    // ✅ 사격 다시 가능
        Time.timeScale = 1f;
        // 🖱️ 마우스 커서 다시 잠그기
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
