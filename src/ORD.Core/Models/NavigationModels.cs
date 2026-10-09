namespace ORD.Core.Models;

public class NavigationStyle
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Strengths { get; set; } = string.Empty;      // 강점 (예: "중복 패 전환, 유연한 덱 완성")
    public string BestMatchDecks { get; set; } = string.Empty;  // 어울리는 덱 (예: "물딜 전설 스타트, 겹치는 패가 많을 때")
}

public class NavigationRecommendation
{
    public NavigationStyle Style { get; set; } = null!;
    public double MatchScore { get; set; }                      // 적합도 점수 (0~100)
    public string Rationale { get; set; } = string.Empty;       // 현재 21라 덱 기준 추천 이유
    public string PracticalTip { get; set; } = string.Empty;    // 실전 운영 팁
}
