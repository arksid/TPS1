using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BossDoorTrigger : MonoBehaviour
{
    public BossRoomGuide guide;
    public bool oneShot = true;
    bool _done;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_done && oneShot) return;
        if (!other.CompareTag("Player")) return;

        _done = true;
        if (guide) guide.EnterBossRoomAndSpawn();

        // 원하면 트리거 비활성화
        // gameObject.SetActive(false);
    }
}
