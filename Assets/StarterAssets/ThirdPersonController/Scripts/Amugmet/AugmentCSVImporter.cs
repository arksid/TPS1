using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

public class AugmentCSVImporter : EditorWindow
{
    // CSV 컬럼(새 포맷): Name,Description,Rarity,Category,Type,Value,IsStackable
    private string csvPath = "Assets/augment_list.csv";
    private string saveFolder = "Assets/Augments";

    [MenuItem("Tools/Augment CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AugmentCSVImporter), false, "Augment CSV Importer");
    }

    void OnGUI()
    {
        GUILayout.Label("CSV로 특성 자동 생성 / 업데이트", EditorStyles.boldLabel);
        csvPath = EditorGUILayout.TextField("CSV 파일 경로", csvPath);
        saveFolder = EditorGUILayout.TextField("저장 폴더 경로", saveFolder);

        EditorGUILayout.Space();

        if (GUILayout.Button("CSV 템플릿 만들기 (15개 예시)"))
        {
            CreateTemplateCSV();
        }

        if (GUILayout.Button("CSV 불러와서 생성/업데이트"))
        {
            ImportCSV();
        }

        EditorGUILayout.HelpBox(
            "CSV 헤더(필수): Name,Description,Rarity,Category,Type,Value,IsStackable\n" +
            "- Rarity: AugmentRarity enum 이름 사용 (예: Common, Rare, Epic, Legendary)\n" +
            "- Category: AugmentCategory enum 이름 사용 (예: Offense, Defense, Utility, Movement, Normal, Special)\n" +
            "- Type: AugmentType enum 이름 사용\n" +
            "- Value: float (0.2 = +20%)\n" +
            "- IsStackable: true/false (비우면 false)\n",
            MessageType.Info);
    }

    void ImportCSV()
    {
        if (!File.Exists(csvPath))
        {
            Debug.LogError($"❌ CSV 파일을 찾을 수 없습니다: {csvPath}");
            return;
        }

        EnsureFolder(saveFolder);

        string[] lines = File.ReadAllLines(csvPath, Encoding.UTF8);
        if (lines.Length <= 1)
        {
            Debug.LogWarning("⚠️ CSV에 데이터가 없습니다. 헤더 + 최소 1줄 이상 필요합니다.");
            return;
        }

        // 헤더 파싱
        var header = ParseCsvLine(lines[0]);
        int idxName = FindIndex(header, "Name");
        int idxDesc = FindIndex(header, "Description");
        int idxRarity = FindIndex(header, "Rarity");
        int idxCategory = FindIndex(header, "Category");
        int idxType = FindIndex(header, "Type");
        int idxValue = FindIndex(header, "Value");
        int idxStackable = FindIndex(header, "IsStackable");

        if (idxName < 0 || idxDesc < 0 || idxType < 0 || idxValue < 0)
        {
            Debug.LogError("❌ 헤더가 올바르지 않습니다. Name, Description, Type, Value 는 필수입니다.");
            return;
        }

        int created = 0;
        int updated = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = ParseCsvLine(lines[i]);
            if (cols.Count == 0 || string.IsNullOrWhiteSpace(string.Join("", cols))) continue; // 빈 줄

            string name = Safe(cols, idxName)?.Trim();
            string desc = Safe(cols, idxDesc)?.Trim();
            string typeStr = Safe(cols, idxType)?.Trim();
            string valStr = Safe(cols, idxValue)?.Trim();

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(typeStr) || string.IsNullOrEmpty(valStr))
            {
                Debug.LogWarning($"⚠️ {i + 1}행: Name/Type/Value 중 하나가 비었습니다. 스킵합니다.");
                continue;
            }

            if (!System.Enum.TryParse(typeStr, true, out AugmentType parsedType))
            {
                Debug.LogError($"❌ {i + 1}행: '{typeStr}' 은(는) 유효한 AugmentType이 아닙니다.");
                continue;
            }

            if (!float.TryParse(valStr, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                Debug.LogError($"❌ {i + 1}행: Value '{valStr}' 를 float로 해석할 수 없습니다. (예: 0.2)");
                continue;
            }

            // 선택 항목: Rarity / Category / IsStackable
            AugmentRarity rarity = AugmentRarity.Common;
            if (idxRarity >= 0)
            {
                string r = Safe(cols, idxRarity)?.Trim();
                if (!string.IsNullOrEmpty(r))
                    System.Enum.TryParse(r, true, out rarity);
            }

            AugmentCategory category = AugmentCategory.Offense;
            if (idxCategory >= 0)
            {
                string c = Safe(cols, idxCategory)?.Trim();
                if (!string.IsNullOrEmpty(c))
                    System.Enum.TryParse(c, true, out category);
            }

            bool isStackable = false;
            if (idxStackable >= 0)
            {
                bool.TryParse(Safe(cols, idxStackable), out isStackable);
            }

            // 에셋 경로
            string fileName = MakeSafeFileName(name);
            string path = $"{saveFolder}/{fileName}.asset";

            AugmentData asset = AssetDatabase.LoadAssetAtPath<AugmentData>(path);
            bool isNew = (asset == null);

            if (isNew)
            {
                asset = ScriptableObject.CreateInstance<AugmentData>();
                AssetDatabase.CreateAsset(asset, path);
                created++;
            }
            else
            {
                updated++;
            }

            // AugmentData에 값 채우기 (당신 프로젝트의 실제 필드에 맞춤)
            asset.augmentName = name;
            asset.description = desc;
            asset.rarity = rarity;
            asset.category = category;
            asset.type = parsedType;
            asset.value = value;
            asset.isStackable = isStackable;

            EditorUtility.SetDirty(asset);
            Debug.Log($"✅ {(isNew ? "생성" : "업데이트")}: {name} ({parsedType}, value={value}, stackable={isStackable})");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"🎉 생성 {created} / 업데이트 {updated} 완료!");
    }

    // ──────────────────────────────────────
    // 템플릿 생성 (15개 예시)
    // ──────────────────────────────────────
    void CreateTemplateCSV()
    {
        EnsureFolder(Path.GetDirectoryName(csvPath)?.Replace("\\", "/"));
        var sb = new StringBuilder();
        sb.AppendLine("Name,Description,Rarity,Category,Type,Value,IsStackable");

        // 아래 타입 이름들은 AugmentData.cs의 AugmentType과 반드시 일치해야 합니다.
        // (당신 프로젝트에 실제로 들어있는 enum 값 사용)
        sb.AppendLine("\"Berserker\",\"공격력이 20% 증가\",Rare,Offense,Berserker,0.2,false");
        sb.AppendLine("\"Slayer\",\"크리티컬 확률 +15%\",Rare,Offense,Slayer,0.15,false");
        sb.AppendLine("\"Overdrive\",\"발사 속도 +20%\",Rare,Offense,Overdrive,0.2,false");
        sb.AppendLine("\"IronSkin\",\"최대 실드 +50\",Common,Defense,IronSkin,50,false");
        sb.AppendLine("\"Survivor\",\"처치 시 체력 10 회복\",Common,Defense,Survivor,10,true");
        sb.AppendLine("\"Retaliation\",\"피격 시 실드 10 회복\",Rare,Defense,Retaliation,10,false");
        sb.AppendLine("\"Predator\",\"체력 50% 이하일 때 공격력 +25%\",Epic,Offense,Predator,0.25,false");
        sb.AppendLine("\"TriggerRush\",\"처치 시 3초간 이속 +30%\",Epic,Movement,TriggerRush,0.3,false");
        sb.AppendLine("\"AdrenalSurge\",\"연속 명중 시 공속 증가(스택)\",Epic,Offense,AdrenalSurge,0.15,true");
        sb.AppendLine("\"ChainReaction\",\"처치 시 폭발 효과\",Legendary,Offense,ChainReaction,1,false");
        sb.AppendLine("\"Vengeance\",\"피격 후 5초간 공격력 +20%\",Epic,Offense,Vengeance,0.2,false");
        sb.AppendLine("\"BulletFever\",\"연속 사격 시 크리티컬 상승\",Legendary,Offense,BulletFever,0.05,true");
        sb.AppendLine("\"ColdRage\",\"체력 낮을수록 크리티컬 증가\",Legendary,Offense,ColdRage,0.3,false");
        sb.AppendLine("\"SecondWind\",\"체력 20% 이하 시 실드 50 자동 회복\",Rare,Defense,SecondWind,50,false");
        sb.AppendLine("\"UltCharger\",\"명중 시 궁극기 게이지 +1%\",Legendary,Utility,UltCharger,1,true");

        File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"📝 템플릿을 생성했습니다: {csvPath}");
    }

    // ──────────────────────────────────────
    // 유틸
    // ──────────────────────────────────────
    static int FindIndex(List<string> header, string key)
    {
        for (int i = 0; i < header.Count; i++)
        {
            if (string.Equals(header[i].Trim(), key, System.StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    static string Safe(List<string> cols, int idx)
    {
        if (idx < 0 || idx >= cols.Count) return null;
        return cols[idx];
    }

    static void EnsureFolder(string folder)
    {
        if (string.IsNullOrEmpty(folder)) return;
        var parts = folder.Split('/');
        string current = parts[0];
        if (!AssetDatabase.IsValidFolder(current)) return; // "Assets" 가 최상위여야 함

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }

    static string MakeSafeFileName(string s)
    {
        if (string.IsNullOrEmpty(s)) return "Augment";
        foreach (char c in Path.GetInvalidFileNameChars())
            s = s.Replace(c.ToString(), "_");
        return s;
    }

    // 따옴표 지원 CSV 파서
    static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        if (line == null) return result;

        bool inQuotes = false;
        var sb = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"'); // "" -> "
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Length = 0;
            }
            else
            {
                sb.Append(c);
            }
        }
        result.Add(sb.ToString());
        return result;
    }
}
