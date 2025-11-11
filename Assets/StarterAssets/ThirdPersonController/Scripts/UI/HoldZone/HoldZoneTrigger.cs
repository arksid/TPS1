using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoldZoneTrigger : MonoBehaviour
{
    [Header("Player")]
    public string playerTag = "Player";

    [Header("Guide/UI")]
    public SimpleWaypointUI waypointUI;
    public Transform holdZone; // 비우면 자기 transform
    [TextArea] public string message = "지정된 구역으로 이동해 거점을 유지하세요!";

    [Header("Mission link")]
    public HoldZoneMission mission;

    [Header("Spawner enable on enter")]
    [Tooltip("플레이어가 트리거에 들어오면 아래 스포너(또는 컨테이너) GO를 SetActive(true)")]
    public bool enableSpawnersOnEnter = true;
    public GameObject[] spawnersToEnable;   // 시작 시 비활성 상태
    public bool enableOnlyOnce = true;

    [Header("Misc")]
    public bool clearWaypointOnEnter = true;
    public bool hideWaypointUIOnEnable = true;
    public bool warmStartOverlap = true;
    public float warmStartDelay = 0.05f;

    Transform _player;
    bool _enabledOnce;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p) _player = p.transform;
    }

    void OnEnable()
    {
        _enabledOnce = false;

        if (hideWaypointUIOnEnable)
            WaypointDirector.HideUIOnly();

        var target = holdZone ? holdZone : transform;
        WaypointDirector.EnableHints();
        WaypointDirector.Show(waypointUI, target, message);

        if (warmStartOverlap) StartCoroutine(CoWarmStart());
    }

    System.Collections.IEnumerator CoWarmStart()
    {
        yield return new WaitForSeconds(warmStartDelay);
        var col = GetComponent<Collider>();
        if (!_player || !col || !col.enabled) yield break;

        Vector3 pp = _player.position + Vector3.up * 0.2f;
        bool inside = (col.ClosestPoint(pp) - pp).sqrMagnitude < 1e-6f;
        if (inside)
        {
            if (mission) mission.ForceEnter();
            OnPlayerEntered();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (mission) mission.ForceEnter();
        OnPlayerEntered();
    }

    void OnPlayerEntered()
    {
        if (clearWaypointOnEnter)
            WaypointDirector.Clear();

        // ★ 미션이 이미 완료됐으면 UI를 다시 켜지 않음
        if (mission && mission.ui && !mission.IsCompleted)
            mission.ui.Show();

        if (enableSpawnersOnEnter && (!enableOnlyOnce || !_enabledOnce))
        {
            _enabledOnce = true;
            if (spawnersToEnable != null)
            {
                foreach (var go in spawnersToEnable)
                {
                    if (!go) continue;
                    ActivateHierarchy(go); // 부모까지 켜기
                    Debug.Log($"[HoldZoneTrigger] 활성화: {go.name}");
                }
            }
        }
    }

    static void ActivateHierarchy(GameObject leaf)
    {
        var stack = new Stack<Transform>();
        var t = leaf.transform;
        while (t != null) { stack.Push(t); t = t.parent; }
        while (stack.Count > 0)
        {
            var cur = stack.Pop().gameObject;
            if (!cur.activeSelf) cur.SetActive(true);
        }
    }
}
