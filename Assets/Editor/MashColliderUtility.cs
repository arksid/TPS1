using UnityEditor;
using UnityEngine;

public class MeshColliderUtility
{
    [MenuItem("Tools/Add MeshColliders To Selection Recursively")]
    static void AddMeshCollidersToSelected()
    {
        GameObject[] selected = Selection.gameObjects;
        int total = 0;

        foreach (GameObject go in selected)
        {
            MeshFilter[] meshFilters = go.GetComponentsInChildren<MeshFilter>(true);

            foreach (MeshFilter mf in meshFilters)
            {
                if (mf.sharedMesh == null) continue;

                MeshCollider mc = mf.GetComponent<MeshCollider>();
                if (mc == null)
                {
                    mf.gameObject.AddComponent<MeshCollider>();
                    total++;
                }
            }
        }

        Debug.Log($"MeshColliders added to {total} objects.");
    }
}
