namespace ORD.Core.Models;

public static class NightmareCriteria
{
    // === 악몽 물딜 합격 기준선 ===
    public const int MinPhysicalArmorReduction = 165;    // 최소 방깎 (이 이하면 라인사 위험 급증)
    public const int SafePhysicalArmorReduction = 185;   // 안정권 풀방깎
    public const int OvercapPhysicalArmor = 200;         // 초과 방깎 (이 이상은 방깎보다 이감/스턴이 우선)
    public const int TargetPhysicalSlow = 110;           // 필수 이감 110%
    public const int TargetPhysicalSlowCap = 117;        // 안정권 이감 캡 (117%+)
    public const double MinPhysicalStun = 2.0;           // 필수 2스턴
    public const double MaxPhysicalStun = 2.5;           // 스턴 과투자 방지 상한선

    // === 악몽 마딜 합격 기준선 ===
    public const double MinMagicStun = 2.5;              // 마딜은 새면 즉사하므로 최소 2.5스턴
    public const int TargetMagicSlow = 100;              // 마딜 이감 100%
    public const int MinMagicArmorReduction = 20;        // 마방깎 / 마뎀증 20%+
}

public class DeckAnalysisResult
{
    public OrdUnit? MainCarry { get; set; }              // 최상위 코어 유닛
    public DamageType PrimaryType { get; set; }          // 덱 전체 속성 (물딜 or 마딜)
    
    // 현재 총합 수치
    public int TotalArmorReduction { get; set; }
    public int TotalArmorBreak { get; set; }
    public int TotalSlow { get; set; }
    public double TotalStun { get; set; }
    public int TotalMagicReduction { get; set; }
    public bool HasPercentDamage { get; set; }
    public bool HasSingleDamage { get; set; }
    public bool HasBossKiller { get; set; }
    public bool HasAttackBuff { get; set; }

    // 결손/부족 요소
    public List<string> Deficits { get; set; } = new();
    public double NightmareClearProbabilityScore { get; set; } // 0~100점
}

public class RecommendationItem
{
    public OrdUnit TargetUnit { get; set; } = null!;
    public double RecommendationScore { get; set; }      // 추천 우선순위 점수 (100점 만점)
    public string CoreReason { get; set; } = string.Empty; // 추천 이유 (예: "방깎 30 + 이감 20% 동시 충족")
    public List<string> SolvedDeficits { get; set; } = new(); // 해결해 주는 결손 항목들
    public double RecipeReadiness { get; set; }           // 보유 패 기반 조합 완성도 (%)
}
