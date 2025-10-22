using UnityEngine;
using UnityEditor;
using System.IO;

public class AugmentCSVImporter : EditorWindow
{
    private string csvPath = "Assets/augment_list.csv";  // CSV 파일 경로
    private string saveFolder = "Assets/Augments";      // ScriptableObject가 저장될 폴더 경로

    [MenuItem("Tools/Augment CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AugmentCSVImporter), false, "Augment CSV Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("CSV로 특성 자동 생성", EditorStyles.boldLabel);
        csvPath = EditorGUILayout.TextField("CSV 파일 경로", csvPath);
        saveFolder = EditorGUILayout.TextField("저장 폴더 경로", saveFolder);

        if (GUILayout.Button("CSV 불러와서 생성"))
        {
            ImportCSV();
        }
    }

    void ImportCSV()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"? CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 폴더 없으면 자동 생성
        if (!AssetDatabase.IsValidFolder(saveFolder))
        {
            string[] parts = saveFolder.Split('/');
            string parent = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string pathToCheck = string.Join("/", parts, 0, i + 1);
                if (!AssetDatabase.IsValidFolder(pathToCheck))
                    AssetDatabase.CreateFolder(parent, parts[i]);
                parent = pathToCheck;
            }
        }

        string[] lines = File.ReadAllLines(csvPath);

        // 첫 줄은 헤더이므로 1부터 시작
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length < 5) continue;

            string augmentName = cols[0].Trim();
            string description = cols[1].Trim();
            AugmentRarity rarity = (AugmentRarity)System.Enum.Parse(typeof(AugmentRarity), cols[2].Trim());
            AugmentCategory category = (AugmentCategory)System.Enum.Parse(typeof(AugmentCategory), cols[3].Trim());
            float value = float.Parse(cols[4].Trim());

            AugmentData asset = ScriptableObject.CreateInstance<AugmentData>();
            asset.augmentName = augmentName;
            asset.description = description;
            asset.rarity = rarity;
            asset.category = category;
            asset.value = value;
            asset.isStackable = true;

            string path = $"{saveFolder}/{augmentName}.asset";
            AssetDatabase.CreateAsset(asset, path);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"? 총 {lines.Length - 1}개의 특성이 CSV로부터 자동 생성되었습니다!");
    }
}
