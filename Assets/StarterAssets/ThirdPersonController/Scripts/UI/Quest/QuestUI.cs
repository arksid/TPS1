// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/QuestUI.cs
using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("TMP UI 참조")]
    public TextMeshProUGUI questTitleText;
    public TextMeshProUGUI questDescText;
    public TextMeshProUGUI objectiveTitleText;
    public TextMeshProUGUI objectiveProgressText;

    /// <summary>
    /// 퀘스트 정보로 UI 갱신
    /// </summary>
    public void Refresh(Quest q)
    {
        if (q == null) return;

        if (questTitleText) questTitleText.text = q.questName;
        if (questDescText) questDescText.text = q.description ?? "";

        if (q.IsCompleted)
        {
            if (objectiveTitleText) objectiveTitleText.text = "모든 목표 완료!";
            if (objectiveProgressText) objectiveProgressText.text = "";
            return;
        }

        var obj = q.CurrentObjective;
        if (obj != null)
        {
            if (objectiveTitleText) objectiveTitleText.text = obj.Title;
            if (objectiveProgressText) objectiveProgressText.text = obj.ProgressText;
        }
        else
        {
            if (objectiveTitleText) objectiveTitleText.text = "";
            if (objectiveProgressText) objectiveProgressText.text = "";
        }
    }
}
