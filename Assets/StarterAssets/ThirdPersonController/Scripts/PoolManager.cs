using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;

    [System.Serializable]
    public class Pool
    {
        public string key;
        public GameObject prefab;
        public int size = 10;
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.key, objectPool);
        }
    }

    public GameObject Get(string key, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"풀 '{key}'가 존재하지 않습니다!");
            return null;
        }

        GameObject obj;

        // ✅ 비활성화된 오브젝트가 남아 있으면 사용
        if (poolDictionary[key].Count > 0 && !poolDictionary[key].Peek().activeInHierarchy)
        {
            obj = poolDictionary[key].Dequeue();
        }
        else
        {
            // 풀 다 썼으면 새로 생성 (혹은 확장)
            Pool pool = pools.Find(p => p.key == key);
            obj = Instantiate(pool.prefab);
        }

        obj.SetActive(true);
        obj.transform.SetPositionAndRotation(position, rotation);

        return obj;
    }

    public void Return(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.SetParent(transform); // 정리용 (Hierarchy 정돈)
    }
}
