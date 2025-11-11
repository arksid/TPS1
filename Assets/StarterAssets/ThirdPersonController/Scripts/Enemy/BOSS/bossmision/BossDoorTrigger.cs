using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossDoorTrigger : MonoBehaviour
{
    [Header("기본")]
    public string playerTag = "Player";
    public bool oneShot = true;
    bool _done;

    [Header("연동")]
    public BossRoomGuide guide;       // 있으면 guide 통해 스폰
    public BossSpawner directSpawner; // guide 없이 직접 스폰하고 싶을 때 사용

    [Header("옵션")]
    public bool clearWaypointOnEnter = true;  // 입장 시 길안내 정리(guide가 있으면 guide가 처리)

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_done && oneShot) return;
        if (!other.CompareTag(playerTag)) return;

        _done = true;

        if (guide)
        {
            // 길안내 정리 + 스폰
            guide.EnterBossRoomAndSpawn();
        }
        else if (directSpawner)
        {
            if (clearWaypointOnEnter)
                WaypointDirector.Clear();
            if (!directSpawner.HasSpawned)
                directSpawner.SpawnNow();
        }

        // 1회용이면 필요 시 트리거를 끄고 싶다면 주석 해제
        // gameObject.SetActive(false);
    }
}
