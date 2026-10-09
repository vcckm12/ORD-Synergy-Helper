using ORD.Core.Models;

namespace ORD.Core.Engine;

public class SynergyEvaluator
{
    public DeckAnalysisResult Evaluate(IEnumerable<OrdUnit> currentDeck)
    {
        var deck = currentDeck.ToList();
        var result = new DeckAnalysisResult();

        if (!deck.Any())
        {
            result.Deficits.Add("필드에 유닛이 없습니다. 메인 캐리 유닛을 먼저 결정하세요.");
            return result;
        }

        // 1. 최상위 티어 유닛(메인 캐리) 판별
        var mainCarry = deck
            .OrderByDescending(u => (int)u.Tier)
            .FirstOrDefault();

        result.MainCarry = mainCarry;

        // 메인 딜링 속성 결정 (최상위 유닛 우선, 없으면 다수결)
        result.PrimaryType = mainCarry?.PrimaryDamageType != DamageType.Support && mainCarry?.PrimaryDamageType != DamageType.None
            ? mainCarry!.PrimaryDamageType
            : deck.Count(u => u.PrimaryDamageType == DamageType.Physical) >= deck.Count(u => u.PrimaryDamageType == DamageType.Magic)
                ? DamageType.Physical
                : DamageType.Magic;

        // 2. 전체 시너지 수치 합산
        result.TotalArmorReduction = deck.Sum(u => u.Synergy.ArmorReduction);
        result.TotalArmorBreak = deck.Sum(u => u.Synergy.ArmorBreakOverlap);
        result.TotalSlow = deck.Sum(u => u.Synergy.MovementSlow);
        result.TotalStun = deck.Sum(u => u.Synergy.StunValue);
        result.TotalMagicReduction = deck.Sum(u => u.Synergy.MagicArmorReduction);
        result.HasPercentDamage = deck.Any(u => u.Synergy.HasPercentDamage);
        result.HasSingleDamage = deck.Any(u => u.Synergy.HasSingleTargetDamage);
        result.HasBossKiller = deck.Any(u => u.Synergy.HasBossKiller);
        result.HasAttackBuff = deck.Any(u => u.Synergy.HasAttackBuff);

        // 3. 악몽(Nightmare) 기준 결손(Deficit) 분석
        EvaluateNightmareDeficits(result);

        return result;
    }

    private void EvaluateNightmareDeficits(DeckAnalysisResult result)
    {
        var deficits = result.Deficits;
        double score = 100.0;

        if (result.PrimaryType == DamageType.Physical)
        {
            // [물딜 1순위: 방깎]
            int totalEffectiveArmorCut = result.TotalArmorReduction + (int)(result.TotalArmorBreak * 0.7);
            if (totalEffectiveArmorCut < NightmareCriteria.MinPhysicalArmorReduction)
            {
                int missing = NightmareCriteria.SafePhysicalArmorReduction - totalEffectiveArmorCut;
                deficits.Add($"[치명적] 방깎 부족: 현재 {totalEffectiveArmorCut}깎 (악몽 권장 185깎 대비 {missing}깎 부족)");
                score -= Math.Min(50, missing * 0.6);
            }
            else if (totalEffectiveArmorCut < NightmareCriteria.SafePhysicalArmorReduction)
            {
                deficits.Add($"[경고] 방깎 보완 권장: 현재 {totalEffectiveArmorCut}깎 (185 풀방깎까지 {NightmareCriteria.SafePhysicalArmorReduction - totalEffectiveArmorCut}깎 남음)");
                score -= 10;
            }
            else if (totalEffectiveArmorCut >= NightmareCriteria.OvercapPhysicalArmor)
            {
                deficits.Add($"[초과 달성] 방깎 {totalEffectiveArmorCut}깎: 악몽 185 풀방깎을 초과했습니다. 추가 방깎은 한계 효용이 낮으므로 '이감 117% 캡'과 '스턴 홀딩' 완성이 절대적으로 우선됩니다.");
            }

            // [물딜 2순위: 이감]
            if (result.TotalSlow < NightmareCriteria.TargetPhysicalSlow)
            {
                int missingSlow = NightmareCriteria.TargetPhysicalSlow - result.TotalSlow;
                deficits.Add($"[이감 위험] 현재 {result.TotalSlow}% (최소 기준 110% 대비 {missingSlow}% 부족, 라인 몹 누수 위험)");
                score -= Math.Min(30, missingSlow * 0.4);
            }
            else if (result.TotalSlow < NightmareCriteria.TargetPhysicalSlowCap)
            {
                int missingSlow = NightmareCriteria.TargetPhysicalSlowCap - result.TotalSlow;
                deficits.Add($"[이감 보강 권장] 현재 {result.TotalSlow}% (완전 안정 캡 117%까지 {missingSlow}% 부족, 몹 뭉침 극대화 필요)");
                score -= 5;
            }

            // [물딜 3순위: 스턴]
            if (result.TotalStun < NightmareCriteria.MinPhysicalStun)
            {
                deficits.Add($"[스턴 부족] 현재 {result.TotalStun:F1}스턴 (최소 2.0스턴 필요, 라인사 위험)");
                score -= 25;
            }
            else if (result.TotalStun > NightmareCriteria.MaxPhysicalStun)
            {
                deficits.Add($"[스턴 과투자] 현재 {result.TotalStun:F1}스턴 (2.5스턴 초과로 방깎 딜로스 발생 가능)");
                score -= 5;
            }

            if (!result.HasAttackBuff)
            {
                deficits.Add($"[버프 부재] 공증/공속 버프 유닛 부재 (딜 포텐셜 20% 감소)");
                score -= 10;
            }
        }
        else // 마딜
        {
            // [마딜 1순위: 홀딩/스턴]
            if (result.TotalStun < NightmareCriteria.MinMagicStun)
            {
                deficits.Add($"[치명적] 스턴 부족: 현재 {result.TotalStun:F1}스턴 (마딜은 몹이 새면 즉사하므로 2.5~3.0스턴 필수)");
                score -= 40;
            }

            // [마딜 2순위: 끝딜 / 퍼뎀]
            if (!result.HasPercentDamage)
            {
                deficits.Add($"[치명적] 끝딜(퍼뎀) 부재: 고체력 라인 몹 정리가 불가능합니다.");
                score -= 30;
            }

            // [마딜 3순위: 단일 딜]
            if (!result.HasSingleDamage)
            {
                deficits.Add($"[단일 부족] 딸피 잔몹 삭제용 단일 점사 딜러 필요");
                score -= 15;
            }

            // [마딜 4순위: 보잡]
            if (!result.HasBossKiller)
            {
                deficits.Add($"[보잡 위험] 보스 전담 딜러 부재로 라인 겹침 시 스턴 분산 위험");
                score -= 15;
            }
        }

        result.NightmareClearProbabilityScore = Math.Max(5, Math.Min(100, score));
    }
}
