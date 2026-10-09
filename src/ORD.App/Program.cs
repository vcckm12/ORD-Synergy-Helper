using System;
using System.Text;
using ORD.Core.Database;
using ORD.Core.Engine;
using ORD.Core.Models;

Console.OutputEncoding = Encoding.UTF8;
Console.WriteLine("=================================================================");
Console.WriteLine("  ORD Synergy & Build Helper (원랜디 악몽 클리어 스마트 추천기)  ");
Console.WriteLine("=================================================================\n");

var repo = new UnitRepository();
var evaluator = new SynergyEvaluator();
var recommender = new BuildRecommender(repo);

// -------------------------------------------------------------
// [시나리오 1: 루피 초월을 뽑은 상황 (물딜 메인 딜러)]
// -------------------------------------------------------------
Console.WriteLine(">>> [시나리오 1] 유저가 '루피 초월'을 뽑고 서포터로 '후지토라 전설'만 있는 상태");

var deck1 = new List<OrdUnit>
{
    repo.GetById("luffy_trans")!,    // 루피 초월 (물딜)
    repo.GetById("fujitora_leg")!    // 후지토라 전설 (12깎, 30이감, 1스턴)
};

PrintAnalysisAndRecommendations(deck1, evaluator, recommender);

Console.WriteLine("\n-------------------------------------------------------------\n");

// -------------------------------------------------------------
// [시나리오 2: 시라호시 초월을 뽑은 상황 (마딜 메인 딜러)]
// -------------------------------------------------------------
Console.WriteLine(">>> [시나리오 2] 유저가 '시라호시 초월'을 뽑고 스턴만 있는 상태 (마딜)");

var deck2 = new List<OrdUnit>
{
    repo.GetById("shirahoshi_trans")!, // 시라호시 초월 (1.5스턴, 마딜)
    repo.GetById("bartolomeo_leg")!    // 바르톨로메오 전설 (1.0스턴)
};

PrintAnalysisAndRecommendations(deck2, evaluator, recommender);

Console.WriteLine("\n-------------------------------------------------------------\n");

// -------------------------------------------------------------
// [시나리오 3: 유저 질문 상황 - 65라운드 완결 모드, 211깎, 이감 102%]
// -------------------------------------------------------------
Console.WriteLine(">>> [시나리오 3] 유저 질문 케이스: [65라운드 완결 모드] 211깎 / 이감 102% 상태에서 오로성 선택 분석");
Console.WriteLine("    - 65라 완결 기준 풀방깎은 150~155깎입니다!");
Console.WriteLine("    - 나스쥬로 선택 시: 이감 102% -> 117% (이감 +15%)");
Console.WriteLine("    - 워큐리 선택 시  : 방깎 211깎 -> 226깎 (방깎 +15)\n");

var deck3 = new List<OrdUnit>
{
    new OrdUnit
    {
        Id = "current_deck_core",
        Name = "루피 초월 + 풀물딜 덱",
        Tier = UnitTier.Transcendence,
        PrimaryDamageType = DamageType.Physical,
        Synergy = new UnitSynergy
        {
            ArmorReduction = 211,
            MovementSlow = 102,
            StunValue = 2.0,
            HasAttackBuff = true
        }
    }
};

PrintAnalysisAndRecommendations(deck3, evaluator, recommender, 65);

var analysis3 = evaluator.Evaluate(deck3, 65);
var allRecs = recommender.RecommendNextBuilds(analysis3, deck3, 15);
var nusjuroRec = allRecs.FirstOrDefault(r => r.TargetUnit.Id == "gorosei_nusjuro");
var warcuryRec = allRecs.FirstOrDefault(r => r.TargetUnit.Id == "gorosei_warcury");

Console.WriteLine("\n[4] ★ [65라운드 완결 모드 기준 오로성 선택 직관 비교]:");
if (nusjuroRec != null)
{
    Console.WriteLine($"   * [압도적 추천] {nusjuroRec.TargetUnit.Name} -> 추천점수: {nusjuroRec.RecommendationScore:F0}점 ({nusjuroRec.CoreReason})");
    Console.WriteLine($"     => 결과: 65라 보스/라인 몹이 제자리에 완전 고정(이감 117% 캡 도달). 211깎 초극딜로 5초 컷 클리어!");
}
if (warcuryRec != null)
{
    Console.WriteLine($"   * [비추천 낭비] {warcuryRec.TargetUnit.Name} -> 추천점수: {warcuryRec.RecommendationScore:F0}점 ({warcuryRec.CoreReason})");
    Console.WriteLine($"     => 결과: 65라 풀방깎(155깎) 대비 이미 +56깎 초과 상태에서 또 +15깎(총 226깎)은 200% 오버킬 낭비입니다.");
}


void PrintAnalysisAndRecommendations(List<OrdUnit> deck, SynergyEvaluator eval, BuildRecommender rec, int targetRound = 65)
{
    var analysis = eval.Evaluate(deck, targetRound);

    Console.WriteLine($"\n[1] 덱 분석 결과");
    Console.WriteLine($" * 최상위 코어: {analysis.MainCarry?.Name ?? "없음"} (판별: {analysis.PrimaryType})");
    Console.WriteLine($" * 현재 수치  : 방깎 {analysis.TotalArmorReduction}깎 (암브 +{analysis.TotalArmorBreak}) | 이감 {analysis.TotalSlow}% | 스턴 {analysis.TotalStun:F1} | 마방깎 {analysis.TotalMagicReduction}%");
    Console.WriteLine($" * 악몽 클리어 기대 점수: {analysis.NightmareClearProbabilityScore:F0} / 100점");
    
    Console.WriteLine("\n[2] 악몽 기준 결손(부족) 분석:");
    foreach (var def in analysis.Deficits)
    {
        Console.WriteLine($"   ! {def}");
    }

    var bestBuilds = rec.RecommendNextBuilds(analysis, deck, 3);
    Console.WriteLine("\n[3] ★ 최적의 다음 유닛 추천 (시너지 보완 랭킹):");
    int rank = 1;
    foreach (var b in bestBuilds)
    {
        Console.WriteLine($"   #{rank++} [{b.TargetUnit.Tier}] {b.TargetUnit.Name} (추천지수: {b.RecommendationScore:F0}점)");
        Console.WriteLine($"      -> 이유: {b.CoreReason}");
    }
}
