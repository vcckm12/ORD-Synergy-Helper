using ORD.Core.Models;
using ORD.Core.Database;

namespace ORD.Core.Engine;

public class BuildRecommender
{
    private readonly UnitRepository _repository;

    public BuildRecommender(UnitRepository repository)
    {
        _repository = repository;
    }

    public List<RecommendationItem> RecommendNextBuilds(DeckAnalysisResult analysis, IEnumerable<OrdUnit> currentDeck, int topCount = 5)
    {
        var existingIds = currentDeck.Select(u => u.Id).ToHashSet();
        var candidates = _repository.GetAll()
            .Where(u => !existingIds.Contains(u.Id)) // 이미 보유한 유닛 제외
            .Where(u => u.Tier >= UnitTier.Hidden && u.Tier <= UnitTier.Legendary) // 추천 대상은 주로 조합 목표인 전설/히든/변이
            .ToList();

        var recommendations = new List<RecommendationItem>();

        foreach (var candidate in candidates)
        {
            var item = ScoreCandidate(candidate, analysis);
            if (item != null && item.RecommendationScore > 30)
            {
                recommendations.Add(item);
            }
        }

        return recommendations
            .OrderByDescending(r => r.RecommendationScore)
            .Take(topCount)
            .ToList();
    }

    private RecommendationItem? ScoreCandidate(OrdUnit candidate, DeckAnalysisResult analysis)
    {
        double score = 50.0;
        var reasons = new List<string>();
        var solvedDeficits = new List<string>();

        // 속성 역행 페널티 (물딜 덱인데 순수 마딜 유닛 추천 금지 등)
        if (analysis.PrimaryType == DamageType.Physical)
        {
            if (candidate.PrimaryDamageType == DamageType.Magic && candidate.Synergy.ArmorReduction == 0)
            {
                return null; // 물딜 덱에 무의미한 순수 마딜 탈락
            }

            // 1. 방깎 기여도 평가
            int totalCut = candidate.Synergy.ArmorReduction + candidate.Synergy.ArmorBreakOverlap;
            if (totalCut > 0)
            {
                int missingCut = Math.Max(0, NightmareCriteria.SafePhysicalArmorReduction - analysis.TotalArmorReduction);
                if (missingCut > 0)
                {
                    score += Math.Min(35, totalCut * 0.9);
                    reasons.Add($"방깎 +{totalCut} 보완");
                    solvedDeficits.Add("방깎");
                }
            }

            // 2. 이감 기여도 평가
            if (candidate.Synergy.MovementSlow > 0 && analysis.TotalSlow < NightmareCriteria.TargetPhysicalSlow)
            {
                score += Math.Min(25, candidate.Synergy.MovementSlow * 0.7);
                reasons.Add($"이감 +{candidate.Synergy.MovementSlow}% 보완");
                solvedDeficits.Add("이감");
            }

            // 3. 스턴 기여도 평가
            if (candidate.Synergy.StunValue > 0)
            {
                if (analysis.TotalStun < NightmareCriteria.MinPhysicalStun)
                {
                    score += 25;
                    reasons.Add($"스턴 +{candidate.Synergy.StunValue:F1} 확보 (라인 안정화)");
                    solvedDeficits.Add("스턴");
                }
                else if (analysis.TotalStun >= NightmareCriteria.MaxPhysicalStun)
                {
                    score -= 20; // 스턴 과투자는 깎 딜로스이므로 감점
                }
            }

            // 4. 버프/보잡 보너스
            if (candidate.Synergy.HasAttackBuff && !analysis.HasAttackBuff)
            {
                score += 15;
                reasons.Add("공증/공속 버프 제공");
                solvedDeficits.Add("버프");
            }
        }
        else // 마딜 덱
        {
            if (candidate.PrimaryDamageType == DamageType.Physical && candidate.Synergy.MovementSlow == 0 && candidate.Synergy.StunValue == 0)
            {
                return null; // 마딜 덱에 무의미한 순수 물딜 탈락
            }

            if (candidate.Synergy.StunValue > 0 && analysis.TotalStun < NightmareCriteria.MinMagicStun)
            {
                score += 30;
                reasons.Add($"필수 마딜 스턴 +{candidate.Synergy.StunValue:F1} 보강");
                solvedDeficits.Add("스턴");
            }

            if (candidate.Synergy.HasPercentDamage && !analysis.HasPercentDamage)
            {
                score += 35;
                reasons.Add("고체력 라인 녹이기 위한 끝딜(퍼뎀) 확보");
                solvedDeficits.Add("끝딜");
            }

            if (candidate.Synergy.HasSingleTargetDamage && !analysis.HasSingleDamage)
            {
                score += 20;
                reasons.Add("딸피 처리용 단일 점사 확보");
                solvedDeficits.Add("단일딜");
            }

            if (candidate.Synergy.HasBossKiller && !analysis.HasBossKiller)
            {
                score += 20;
                reasons.Add("보스 전담(보잡) 확보");
                solvedDeficits.Add("보잡");
            }
        }

        // 다중 시너지(복합 보완) 보너스 (예: 방깎과 이감을 동시에 채우는 유닛)
        if (solvedDeficits.Count >= 2)
        {
            score += 15;
            reasons.Add("★ 복합 결손 해소 (1타 2피 유닛)");
        }

        return new RecommendationItem
        {
            TargetUnit = candidate,
            RecommendationScore = Math.Min(100, score),
            CoreReason = string.Join(", ", reasons),
            SolvedDeficits = solvedDeficits,
            RecipeReadiness = 0.0 // 추후 하위패 인벤토리 연동 시 계산
        };
    }
}
