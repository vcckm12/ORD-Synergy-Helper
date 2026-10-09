namespace ORD.Core.Recipes;

public class BaseCommonsCount
{
    public int Luffy { get; set; }       // C1: 루피
    public int Zoro { get; set; }        // C2: 조로
    public int Nami { get; set; }        // C3: 나미
    public int Usopp { get; set; }       // C4: 우솝
    public int Sanji { get; set; }       // C5: 상디
    public int Chopper { get; set; }     // C6: 쵸파
    public int Buggy { get; set; }       // C7: 버기
    public int Gunner { get; set; }      // C8: 총병
    public int Swordman { get; set; }    // C9: 칼병

    public int Total => Luffy + Zoro + Nami + Usopp + Sanji + Chopper + Buggy + Gunner + Swordman;

    public Dictionary<string, int> ToDictionary() => new()
    {
        { "루피", Luffy },
        { "조로", Zoro },
        { "나미", Nami },
        { "우솝", Usopp },
        { "상디", Sanji },
        { "쵸파", Chopper },
        { "버기", Buggy },
        { "총병", Gunner },
        { "칼병", Swordman }
    };

    public void Add(string code, int count = 1)
    {
        switch (code)
        {
            case "C1": Luffy += count; break;
            case "C2": Zoro += count; break;
            case "C3": Nami += count; break;
            case "C4": Usopp += count; break;
            case "C5": Sanji += count; break;
            case "C6": Chopper += count; break;
            case "C7": Buggy += count; break;
            case "C8": Gunner += count; break;
            case "C9": Swordman += count; break;
        }
    }

    public void Add(BaseCommonsCount other, int multiplier = 1)
    {
        Luffy += other.Luffy * multiplier;
        Zoro += other.Zoro * multiplier;
        Nami += other.Nami * multiplier;
        Usopp += other.Usopp * multiplier;
        Sanji += other.Sanji * multiplier;
        Chopper += other.Chopper * multiplier;
        Buggy += other.Buggy * multiplier;
        Gunner += other.Gunner * multiplier;
        Swordman += other.Swordman * multiplier;
    }
}

public class MaterialDecomposition
{
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public BaseCommonsCount BaseCommons { get; set; } = new();
    public Dictionary<string, int> DirectIngredients { get; set; } = new();
    public string ExtraCost { get; set; } = string.Empty;
}

public class InventoryReadiness
{
    public string TargetUnitCode { get; set; } = string.Empty;
    public string TargetUnitName { get; set; } = string.Empty;
    public double ReadinessPercentage { get; set; } // 0 ~ 100%
    public int TotalRequiredCommons { get; set; }
    public int TotalFulfilledCommons { get; set; }
    public int TotalMissingCommons { get; set; }
    public Dictionary<string, int> MissingCommons { get; set; } = new(); // 이름 -> 부족 개수
    public List<string> MissingDirectUnits { get; set; } = new();         // 아직 보유하지 못한 직접 하위 조합 유닛
}
