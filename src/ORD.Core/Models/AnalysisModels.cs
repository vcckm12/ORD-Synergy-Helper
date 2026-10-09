namespace ORD.Core.Models;

public static class NightmareCriteria
{
    // ==========================================
    // [v2.323 공식 기준 스펙 (ORDR / 최신 패치)]
    // ==========================================
    public const int V2323_NightmareArmor = 211;         // v2.323 악몽 풀방깎 기준 (정식 211깎)
    public const int V2323_ShinArmor = 201;              // v2.323 신 난이도 풀방깎 기준 (정식 201깎)
    public const int V2323_FullSlowCap = 102;            // v2.323 신/악몽 공통 풀이감 기준 (정식 102%)
    public const int V2323_EndRound = 65;                // v2.323 최종 완결 라운드 (65라운드)

    // 스턴 기준선
    public const double MinPhysicalStun = 2.0;           // 필수 2스턴 (드래곤, 이완히든 그블 등)
    public const double MaxPhysicalStun = 2.5;           // 스턴 과투자 방지 상한선
    public const double MinMagicStun = 2.5;              // 마딜 필수 스턴 (2.5~3스턴)
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
