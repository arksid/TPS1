using UnityEngine;

public class BossRoomGuide : MonoBehaviour
{
    [Header("연동 오브젝트")]
    public Transform bossRoomTarget;      // 보스 방(문 앞/중앙) 위치
    public GameObject outlineTarget;      // 아웃라인 줄 오브젝트(문/문틀 등)
    public SimpleWaypointUI waypointUI;   // 웨이포인트 UI
    public BossSpawner bossSpawner;       // 보스 스폰 담당

    [Header("UI 라벨")]
    [TextArea] public string waypointLabel = "보스 방으로 이동";

    [Header("옵션")]
    public bool clearNavWhenSpawned = true;   // 스폰되면 길안내 자동 정리
    public bool subscribeSpawnEvent = true;   // BossSpawner.onSpawned 구독해 자동 정리

    bool _navActive;

    void Awake()
    {
        if (!waypointUI) waypointUI = FindObjectOfType<SimpleWaypointUI>(true);
    }

    void OnEnable()
    {
        if (subscribeSpawnEvent && bossSpawner)
            bossSpawner.onSpawned.AddListener(OnBossSpawned);
    }

    void OnDisable()
    {
        if (subscribeSpawnEvent && bossSpawner)
            bossSpawner.onSpawned.RemoveListener(OnBossSpawned);
    }

    void OnBossSpawned(BossMonster boss)
    {
        if (!clearNavWhenSpawned) return;
        ShowBossNavigation(false);
    }

    // === 인스펙터에서 HoldZoneMission.onCompleted 에 연결할 메서드 ===
    public void ShowBossNav_On()
    {
        ShowBossNavigation(true);
    }

    public void ShowBossNav_Off()
    {
        ShowBossNavigation(false);
    }

    public void ShowBossNavigation(bool on)
    {
        if (!waypointUI || !bossRoomTarget) return;

        if (on)
        {
            // 아웃라인 ON
            OutlineHelper.SetOutline(outlineTarget, true);

            // 웨이포인트 표시
            WaypointDirector.EnableHints();
            WaypointDirector.Show(waypointUI, bossRoomTarget, waypointLabel);

            _navActive = true;
        }
        else
        {
            // 아웃라인/웨이포인트 OFF
            OutlineHelper.SetOutline(outlineTarget, false);
            WaypointDirector.Clear();
            _navActive = false;
        }
    }

    // 문/입구 트리거에서 호출: 길안내 정리 + 보스 스폰
    public void EnterBossRoomAndSpawn()
    {
        if (_navActive) ShowBossNavigation(false);
        if (bossSpawner && !bossSpawner.HasSpawned) bossSpawner.SpawnNow();
    }
}
