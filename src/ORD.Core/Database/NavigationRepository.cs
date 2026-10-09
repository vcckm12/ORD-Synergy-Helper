using ORD.Core.Models;

namespace ORD.Core.Database;

public class NavigationRepository
{
    private readonly List<NavigationStyle> _styles = new()
    {
        new NavigationStyle
        {
            Id = "alchemy",
            Name = "연금술",
            Description = "유닛 및 패를 변환하여 겹치는 패를 원하는 재료로 유연하게 전환하는 1티어 만능 항법",
            Strengths = "중복 패 억까 완벽 해소, 패 소모 최적화, 2전설 빌드업 안정성 최고",
            BestMatchDecks = "특정 패(우솝, 총병 등)가 쏠렸을 때, 물딜/마딜 전설 스타트 공통"
        },
        new NavigationStyle
        {
            Id = "emergency_call",
            Name = "긴급소집",
            Description = "초반 패 부족을 즉각 해결하고 다량의 위습을 확정 수급하는 안정성 최우선 항법",
            Strengths = "초반 라인 안정화, 빠른 2전설/1상위 진입, 선위 아끼기 최적화",
            BestMatchDecks = "21라 시점에 패가 골고루 말라 2전설 각이 잘 안 보일 때"
        },
        new NavigationStyle
        {
            Id = "casino",
            Name = "카지노",
            Description = "도박 및 확률 강화를 통해 고점(유카 0)을 뚫어내는 하이리스크 하이리턴 항법",
            Strengths = "위습 대박 시 압도적 스펙 폭발 (날먹 클리어), 고점 달성률 최고",
            BestMatchDecks = "이미 첫 전설로 딜이 충분하여 도박 여유가 있거나 유카 0 랭킹 노릴 때"
        },
        new NavigationStyle
        {
            Id = "trait_engineering",
            Name = "특성공학",
            Description = "특성 강화(특강) 포인트를 대량 획득하여 상위 유닛의 스킬 포텐셜을 극대화하는 항법",
            Strengths = "특강 의존도가 높은 1상위의 딜량 1.5배 증폭, 보스 점사력 강화",
            BestMatchDecks = "우타 초월(우초), 불릿, 징초, 바질초 등 특강이 핵심인 상위 지향 시"
        },
        new NavigationStyle
        {
            Id = "bounty_hunter",
            Name = "바운티 헌터",
            Description = "스토리 및 보스 처치 현상금을 극대화하여 후반 강화와 도움소를 싹쓸이하는 경제형 항법",
            Strengths = "2.323 신규 시스템(깎 레전드 강화, 공 유니크 강화)에 필요한 재화 대량 수급",
            BestMatchDecks = "스토리 딜과 보잡이 빠른 덱 (레베카, 킬러, 징베 스타트)"
        }
    };

    public IEnumerable<NavigationStyle> GetAll() => _styles;
    public NavigationStyle? GetById(string id) => _styles.FirstOrDefault(s => s.Id == id);
}
