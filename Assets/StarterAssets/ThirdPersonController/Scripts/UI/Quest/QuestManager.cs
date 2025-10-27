// Assets/StarterAssets/ThirdPersonController/Scripts/Quest/QuestManager.cs
using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    [Header("시작할 퀘스트들(순차가 아니라 '동시' 등록)")]
    public List<Quest> startQuests = new List<Quest>();

    [Header("UI")]
    public QuestUI questUI;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        foreach (var q in startQuests)
            RegisterQuest(q);
    }

    public void RegisterQuest(Quest q)
    {
        if (q == null) return;
        // UI 갱신
        questUI?.Refresh(q);
    }

    public void OnQuestProgressChanged(Quest q)
    {
        questUI?.Refresh(q);
    }

    public void OnObjectiveProgressPing(QuestObjective obj)
    {
        // 단일 퀘스트 UI라고 가정(필요 시 확장)
        // obj 소속 퀘스트 찾아서 갱신
        var q = obj.GetComponentInParent<Quest>();
        if (q != null) questUI?.Refresh(q);
    }
}
