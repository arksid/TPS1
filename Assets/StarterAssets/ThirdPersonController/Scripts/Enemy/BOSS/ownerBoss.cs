using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(BossMonster))]
public class BossMonsterAutoLinker : MonoBehaviour
{
    [Tooltip("자식에서 DamageablePart를 찾아 ownerBoss 필드를 자동 연결합니다.")]
    public bool autoAssignBossOnParts = true;

#if UNITY_EDITOR
    [ContextMenu("Auto Link Parts (Assign DamageablePart.ownerBoss)")]
    public void AutoLink()
    {
        var boss = GetComponent<BossMonster>();
        if (boss == null)
        {
            Debug.LogError("[AutoLink] BossMonster가 없습니다.");
            return;
        }

        var parts = GetComponentsInChildren<DamageablePart>(includeInactive: true);
        int linkedBoss = 0;
        int linkedHitbox = 0;

        foreach (var p in parts)
        {
            if (p == null) continue;

            // ownerBoss 자동 연결
            if (autoAssignBossOnParts && p.ownerBoss == null)
            {
                Undo.RecordObject(p, "Assign Boss on Part");
                p.ownerBoss = boss;
                EditorUtility.SetDirty(p);
                linkedBoss++;
            }

            // Hitbox.part 자동 연결(누락 대비)
            var hitboxes = p.GetComponentsInChildren<Hitbox>(includeInactive: true);
            foreach (var hb in hitboxes)
            {
                if (hb != null && hb.part == null)
                {
                    Undo.RecordObject(hb, "Assign Part on Hitbox");
                    hb.part = p;
                    EditorUtility.SetDirty(hb);
                    linkedHitbox++;
                }
            }
        }

        Debug.Log($"[AutoLink] DamageablePart {parts.Length}개, ownerBoss 연결 {linkedBoss}개, Hitbox 연결 {linkedHitbox}개 완료");
    }
#endif
}
