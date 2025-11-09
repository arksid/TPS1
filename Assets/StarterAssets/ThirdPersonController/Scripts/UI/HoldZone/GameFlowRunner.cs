using System.Collections;
using UnityEngine;

/// <summary>
/// 어디서든 안전하게 코루틴을 돌릴 수 있는 전역 러너.
/// </summary>
public class GameFlowRunner : MonoBehaviour
{
    static GameFlowRunner _inst;

    public static void Run(IEnumerator routine)
    {
        if (_inst == null)
        {
            var go = new GameObject("[GameFlowRunner]");
            Object.DontDestroyOnLoad(go);
            _inst = go.AddComponent<GameFlowRunner>();
        }
        _inst.StartCoroutine(routine);
    }
}
