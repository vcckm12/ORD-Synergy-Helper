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

void PrintAnalysisAndRecommendations(List<OrdUnit> deck, SynergyEvaluator eval, BuildRecommender rec)
{
    var analysis = eval.Evaluate(deck);

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
