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
            Debug.LogError($"❌ CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        // 폴더 자동 생성
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

        // 헤더는 0번이므로 1부터
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cols = lines[i].Split(',');
            if (cols.Length < 6)
            {
                Debug.LogWarning($"⚠️ {i + 1}번째 줄의 데이터가 부족합니다. (6개 필요)");
                continue;
            }

            string augmentName = cols[0].Trim();
            string description = cols[1].Trim();
            AugmentRarity rarity = (AugmentRarity)System.Enum.Parse(typeof(AugmentRarity), cols[2].Trim());
            AugmentCategory category = (AugmentCategory)System.Enum.Parse(typeof(AugmentCategory), cols[3].Trim());

            // 🆕 Type 파싱
            string typeString = cols[4].Trim();
            if (!System.Enum.TryParse(typeString, out AugmentType parsedType))
            {
                Debug.LogError($"❌ '{typeString}' 은(는) 유효한 AugmentType이 아닙니다. ({i + 1}번째 줄)");
                continue;
            }

            float value;
            if (!float.TryParse(cols[5].Trim(), out value))
            {
                Debug.LogError($"❌ {i + 1}번째 줄의 value를 숫자로 변환할 수 없습니다.");
                continue;
            }

            AugmentData asset = ScriptableObject.CreateInstance<AugmentData>();
            asset.augmentName = augmentName;
            asset.description = description;
            asset.rarity = rarity;
            asset.category = category;
            asset.type = parsedType;  // ✅ Type 반영
            asset.value = value;
            asset.isStackable = true;

            string path = $"{saveFolder}/{augmentName}.asset";
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"✅ 생성됨: {augmentName} ({parsedType}, {value})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"🎉 총 {lines.Length - 1}개의 특성이 CSV로부터 자동 생성되었습니다!");
    }
}
