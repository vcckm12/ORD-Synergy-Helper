namespace ORD.Core.Models;

public class UnitSynergy
{
    // === 물리(물딜) 필수 시너지 ===
    public int ArmorReduction { get; set; }           // 기본/오라 방깎
    public int ArmorBreakOverlap { get; set; }       // 암브(타격 시 누적 방깎)
    public int MovementSlow { get; set; }             // 이감 (%)
    public double StunValue { get; set; }             // 스턴 수치 (예: 1스턴 = 1.0, 0.5스턴 = 0.5)
    public bool HasAttackBuff { get; set; }           // 공증/공속 버프 여부

    // === 마법(마딜) 필수 시너지 ===
    public int MagicArmorReduction { get; set; }      // 마법 방어력 감소 / 딜 증폭 (%)
    public bool HasPercentDamage { get; set; }        // 끝딜 (현퍼/최퍼 체력 비례 딜)
    public bool HasSingleTargetDamage { get; set; }   // 단일 점사 딜러 (딸피 정리)
    public bool HasBossKiller { get; set; }           // 보잡 (보스 전담)
    public bool HasManaSkillSynergy { get; set; }     // 마나젠 / 마나번 시너지
}

public class OrdUnit
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UnitTier Tier { get; set; }
    public DamageType PrimaryDamageType { get; set; }
    public UnitSynergy Synergy { get; set; } = new();
    
    // 재료 트리: 재료 유닛 Id -> 필요 개수
    public Dictionary<string, int> Recipe { get; set; } = new();
    
    public override string ToString() => $"[{Tier}] {Name} ({PrimaryDamageType})";
}
