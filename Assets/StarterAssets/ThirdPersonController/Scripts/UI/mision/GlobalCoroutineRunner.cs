using System.Collections;
using UnityEngine;

public class GlobalCoroutineRunner : MonoBehaviour
{
    static GlobalCoroutineRunner _inst;
    public static GlobalCoroutineRunner Instance
    {
        get
        {
            if (_inst == null)
            {
                var go = new GameObject("~GlobalCoroutineRunner");
                DontDestroyOnLoad(go);
                _inst = go.AddComponent<GlobalCoroutineRunner>();
            }
            return _inst;
        }
    }

    public static void Run(IEnumerator routine)
    {
        Instance.StartCoroutine(routine);
    }
}
