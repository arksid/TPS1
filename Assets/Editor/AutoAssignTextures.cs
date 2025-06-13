using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AutoAssignTextures : EditorWindow
{
    [MenuItem("Tools/Auto Assign Textures")]
    static void AssignTextures()
    {
        foreach (Object obj in Selection.objects)
        {
            var mat = obj as Material;
            if (mat == null) continue;

            string matPath = AssetDatabase.GetAssetPath(mat);
            string matName = mat.name;
            string dir = System.IO.Path.GetDirectoryName(matPath);

            string[] textures = AssetDatabase.FindAssets(matName, new[] { dir });

            foreach (string guid in textures)
            {
                string texPath = AssetDatabase.GUIDToAssetPath(guid);
                Texture tex = AssetDatabase.LoadAssetAtPath<Texture>(texPath);
                if (tex == null) continue;

                string texName = tex.name.ToLower();

                if (texName.Contains("albedo") || texName.Contains("diffuse") || texName.Contains("base"))
                {
                    // Base Map 적용 (URP or HDRP 지원)
                    if (mat.HasProperty("_BaseMap"))
                        mat.SetTexture("_BaseMap", tex);
                    else
                        mat.SetTexture("_MainTex", tex);
                }
                else if (texName.Contains("normal"))
                {
                    mat.SetTexture("_BumpMap", tex);
                    mat.EnableKeyword("_NORMALMAP");
                }
                else if (texName.Contains("metal"))
                {
                    mat.SetTexture("_MetallicGlossMap", tex);
                    mat.SetFloat("_Metallic", 1.0f);
                    mat.EnableKeyword("_METALLICGLOSSMAP");
                }
                else if (texName.Contains("ao") || texName.Contains("occlusion"))
                {
                    mat.SetTexture("_OcclusionMap", tex);
                    mat.SetFloat("_OcclusionStrength", 1.0f);
                }
                else if (texName.Contains("emission"))
                {
                    mat.SetTexture("_EmissionMap", tex);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                }
            }
        }

        Debug.Log("Base Map 포함 텍스처 자동 연결 완료!");
    }
}
