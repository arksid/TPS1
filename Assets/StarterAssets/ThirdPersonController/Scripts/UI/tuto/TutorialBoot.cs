using System.Collections;
using UnityEngine;

public static class TutorialBoot
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Run()
    {
        WaypointDirector.DisableHints();   // ★ 게임 시작 시 표시권한 잠금 + 남은 표식 정리
        // 다음 프레임에 실행
        var runner = new GameObject("TutorialBootRunner").AddComponent<TutorialBootRunner>();
        Object.DontDestroyOnLoad(runner.gameObject);
    }

    class TutorialBootRunner : MonoBehaviour
    {
        IEnumerator Start()
        {
            yield return null; // ★ 한 프레임 대기
            var mgr = FindFirstObjectByType<ControlsTutorialManager>();
            if (mgr != null)
            {
                if (mgr.ui) mgr.ui.Show();
                mgr.StartTutorial(); // 이제 steps 채워진 상태
            }
            Destroy(gameObject);
        }
    }
}
