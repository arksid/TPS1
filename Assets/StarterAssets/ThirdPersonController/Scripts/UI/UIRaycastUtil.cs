using UnityEngine;
using UnityEngine.UI;

public static class UIRaycastUtil
{
    /// <summary>
    /// 지정 루트 하위에서 버튼/스크롤 등 '클릭 가능한 그래픽'을 제외하고,
    /// 장식용 Image/Text 등의 RaycastTarget을 자동으로 끕니다.
    /// </summary>
    public static void MakeDecorationsNonBlocking(Transform root)
    {
        if (root == null) return;

        // 모든 Graphic 훑기 (Image, TextMeshProUGUI 등 포함)
        var graphics = root.GetComponentsInChildren<Graphic>(true);
        foreach (var g in graphics)
        {
            // 이미 Button/Toggle/Scrollbar 등과 연결된 그래픽은 건드리지 않음
            bool isInteractiveGraphic =
                g.GetComponent<Button>() != null ||
                g.GetComponent<Toggle>() != null ||
                g.GetComponent<Scrollbar>() != null ||
                g.GetComponent<Dropdown>() != null ||
                g.GetComponent<InputField>() != null;

            // 이름으로도 예외 처리 (필요시 추가)
            bool isDecorLikely = g.name.Equals("Icon") || g.name.Equals("bg");

            if (!isInteractiveGraphic && isDecorLikely)
            {
                g.raycastTarget = false; // ✅ 장식 그래픽은 클릭 차단 금지
            }
        }
    }
}
