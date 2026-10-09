namespace ORD.Core.Models;

public static class NightmareCriteria
{
    // === 65라운드 완결 기준 (65라 종료 모드) ===
    public const int Round65MinPhysicalArmor = 135;      // 65라 최소 방깎
    public const int Round65SafePhysicalArmor = 155;     // 65라 풀방깎 기준 (150~155깎이면 충분)
    public const int Round65OvercapPhysicalArmor = 175;  // 65라 초과 방깎 (175 이상은 방깎 투자 무의미)

    // === 80라운드 정규 기준 ===
    public const int Round80MinPhysicalArmor = 165;      // 80라 최소 방깎
    public const int Round80SafePhysicalArmor = 185;     // 80라 풀방깎 기준
    public const int Round80OvercapPhysicalArmor = 200;  // 80라 초과 방깎

    // 이감 & 스턴 기준 (라운드 무관 필수 공통)
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
    public int TargetRound { get; set; } = 65;           // 목표 라운드 (기본 65라 완결)
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
