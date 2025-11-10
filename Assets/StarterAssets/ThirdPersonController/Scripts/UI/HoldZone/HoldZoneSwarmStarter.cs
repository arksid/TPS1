using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HoldZoneSwarmStarter : MonoBehaviour
{
    [Header("Start when")]
    public bool startOnEnable = false;
    public bool startOnEnter = true;
    public bool onlyOnce = true;

    [Header("Targets (choose one)")]
    [Tooltip("스포너(또는 컨테이너) GO들. SetActive(true)만 수행")]
    public GameObject[] objectsToEnable;

    [Tooltip("옵션: StartWaves/RunWaves 메서드가 있는 스크립트")]
    public MonoBehaviour swarm;

    bool _started;

    void Reset() { GetComponent<Collider>().isTrigger = true; }

    void OnEnable() { if (startOnEnable) TryStart(); }

    void OnTriggerEnter(Collider other)
    {
        if (!startOnEnter) return;
        if (onlyOnce && _started) return;
        if (!other.CompareTag("Player")) return;
        TryStart();
    }

    public void TryStart()
    {
        if (_started && onlyOnce) return;

        // (1) 오브젝트 활성화 방식
        if (objectsToEnable != null && objectsToEnable.Length > 0)
        {
            foreach (var go in objectsToEnable)
            {
                if (!go) continue;
                if (!go.activeSelf) go.SetActive(true);
                Debug.Log($"[HoldZoneSwarmStarter] 활성화: {go.name}");
            }
            _started = true;
            return;
        }

        // (2) 메서드 호출 방식
        if (swarm != null)
        {
            var t = swarm.GetType();
            var mStart = t.GetMethod("StartWaves",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (mStart != null)
            {
                mStart.Invoke(swarm, null);
                _started = true;
                Debug.Log("[HoldZoneSwarmStarter] StartWaves() 호출");
                return;
            }
            var mRun = t.GetMethod("RunWaves",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (mRun != null)
            {
                var routine = mRun.Invoke(swarm, null) as System.Collections.IEnumerator;
                if (routine != null) GameFlowRunner.Run(routine);
                _started = true;
                Debug.Log("[HoldZoneSwarmStarter] RunWaves() 시작");
                return;
            }
        }

        Debug.LogWarning("[HoldZoneSwarmStarter] 시작 대상이 없습니다. objectsToEnable 또는 swarm을 지정하세요.");
    }
}
