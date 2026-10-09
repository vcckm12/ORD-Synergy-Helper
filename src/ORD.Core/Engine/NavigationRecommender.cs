using ORD.Core.Models;
using ORD.Core.Database;

namespace ORD.Core.Engine;

public class NavigationRecommender
{
    private readonly NavigationRepository _repository;

    public NavigationRecommender(NavigationRepository repository)
    {
        _repository = repository;
    }

    public List<NavigationRecommendation> RecommendForRound21(DeckAnalysisResult analysis, IEnumerable<OrdUnit> currentDeck)
    {
        var deck = currentDeck.ToList();
        var styles = _repository.GetAll().ToList();
        var results = new List<NavigationRecommendation>();

        var firstCore = analysis.MainCarry ?? deck.FirstOrDefault();
        bool isPhysical = analysis.PrimaryType == DamageType.Physical;

        foreach (var style in styles)
        {
            var item = ScoreNavigation(style, firstCore, isPhysical, deck);
            results.Add(item);
        }

        return results.OrderByDescending(r => r.MatchScore).ToList();
    }

    private NavigationRecommendation ScoreNavigation(NavigationStyle style, OrdUnit? firstCore, bool isPhysical, List<OrdUnit> deck)
    {
        double score = 70.0;
        string rationale = string.Empty;
        string tip = string.Empty;

        string coreName = firstCore?.Name ?? "1전설";

        switch (style.Id)
        {
            case "alchemy": // 연금술
                score = 92.0;
                rationale = $"현재 [{coreName}] 스타트 기준으로 하위패 겹침(우솝/총병 몰림 등)을 즉시 다른 방깎/이감 재료로 치환할 수 있어 가장 안정적입니다.";
                tip = "3이완코브나 2서드 등 중복으로 뜬 패를 필수 유닛(시노부/킹 재료)으로 과감히 전환하세요.";
                break;

            case "emergency_call": // 긴급소집
                score = 88.0;
                rationale = "21라운드 이후 30라 전에 2번째 전설이나 히든을 확정적으로 뽑을 수 있도록 위습과 흔함을 대량 지원합니다.";
                tip = "선위(선택위습)를 최대한 아끼고, 긴급소집으로 받은 패로 2전설 각을 넓히세요.";
                break;

            case "trait_engineering": // 특성공학
                bool needsTrait = firstCore != null && (firstCore.Name.Contains("우타") || firstCore.Name.Contains("바질") || firstCore.Name.Contains("징베") || firstCore.Name.Contains("불릿"));
                if (needsTrait)
                {
                    score = 95.0; // 특강 유닛이면 1순위로 승격!
                    rationale = $"★ [{coreName}]는 특성 강화(특강) 의존도가 매우 높은 유닛입니다. 특성공학 선택 시 딜량이 1.5배 이상 폭증합니다.";
                    tip = "특강 포인트를 아끼지 말고 메인 캐리의 핵심 강화 스킬에 전부 투자하세요.";
                }
                else
                {
                    score = 75.0;
                    rationale = "향후 특성 강화가 강력한 상위패(우초, 징초, 불릿 등)를 지향할 때 강력한 포텐셜을 발휘합니다.";
                    tip = "2번째 전설이 특강 혜택을 받는지 확인 후 선택을 고려하세요.";
                }
                break;

            case "casino": // 카지노
                score = 82.0;
                rationale = $"[{coreName}]로 이미 20~30라 라인 클리어가 안정적인 상태라면, 위습 도박을 통해 유카 0급 고점을 뚫어낼 수 있습니다.";
                tip = "도박이 망해도 최소한의 스턴과 방깎이 유지될 수 있도록 선위 1~2개는 세이프티로 남겨두세요.";
                break;

            case "bounty_hunter": // 바운티 헌터
                score = 78.0;
                rationale = "스토리 및 보스 처치 현상금을 극대화하여 2.323 최신 시스템인 깎 레전드 강화 및 공 유니크 강화 재화를 조기 선점합니다.";
                tip = "첫 전설이 보잡(레베카, 킬러, 징베 등)을 겸하고 있을 때 효율이 극대화됩니다.";
                break;
        }

        return new NavigationRecommendation
        {
            Style = style,
            MatchScore = score,
            Rationale = rationale,
            PracticalTip = tip
        };
    }
}
