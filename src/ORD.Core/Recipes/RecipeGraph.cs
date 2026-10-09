using ORD.Core.Models;

namespace ORD.Core.Recipes;

public class RecipeGraph
{
    private readonly Dictionary<string, MaterialDecomposition> _cache = new();
    private readonly Dictionary<string, OrdUnit> _units = new();

    public void Initialize(IEnumerable<OrdUnit> units)
    {
        _cache.Clear();
        _units.Clear();
        foreach (var u in units)
        {
            _units[u.Id] = u;
        }

        // 전체 유닛에 대해 하위 흔함 재료 분해 트리 사전 계산
        foreach (var u in _units.Values)
        {
            GetDecomposition(u.Id);
        }
    }

    public MaterialDecomposition GetDecomposition(string unitCode)
    {
        if (_cache.TryGetValue(unitCode, out var cached))
        {
            return cached;
        }

        if (!_units.TryGetValue(unitCode, out var unit))
        {
            return new MaterialDecomposition { UnitCode = unitCode, UnitName = unitCode };
        }

        var decomp = new MaterialDecomposition
        {
            UnitCode = unit.Id,
            UnitName = unit.Name,
            DirectIngredients = new Dictionary<string, int>(unit.Recipe)
        };

        // 기본 흔함 유닛(C1~C9)인 경우 자기 자신이 1개
        if (IsBaseCommon(unit.Id))
        {
            decomp.BaseCommons.Add(unit.Id, 1);
            _cache[unitCode] = decomp;
            return decomp;
        }

        // 레시피가 없거나 특수 유닛인 경우
        if (unit.Recipe.Count == 0)
        {
            _cache[unitCode] = decomp;
            return decomp;
        }

        // 재귀적으로 하위 재료 분해
        foreach (var (reqCode, reqCount) in unit.Recipe)
        {
            var subDecomp = GetDecomposition(reqCode);
            decomp.BaseCommons.Add(subDecomp.BaseCommons, reqCount);
        }

        _cache[unitCode] = decomp;
        return decomp;
    }

    public InventoryReadiness CalculateReadiness(string targetUnitCode, IEnumerable<string> ownedUnitCodes)
    {
        var targetDecomp = GetDecomposition(targetUnitCode);
        var targetUnit = _units.GetValueOrDefault(targetUnitCode);
        string targetName = targetUnit?.Name ?? targetUnitCode;

        int totalRequired = targetDecomp.BaseCommons.Total;
        if (totalRequired == 0)
        {
            return new InventoryReadiness
            {
                TargetUnitCode = targetUnitCode,
                TargetUnitName = targetName,
                ReadinessPercentage = 100.0,
                TotalRequiredCommons = 0,
                TotalFulfilledCommons = 0,
                TotalMissingCommons = 0
            };
        }

        // 보유 중인 유닛들의 하위 재료 풀 구성
        var ownedCommons = new BaseCommonsCount();
        var ownedUnitSet = new HashSet<string>(ownedUnitCodes);
        var missingDirect = new List<string>();

        foreach (var (directCode, directCount) in targetDecomp.DirectIngredients)
        {
            if (!ownedUnitSet.Contains(directCode))
            {
                var directUnit = _units.GetValueOrDefault(directCode);
                missingDirect.Add(directUnit?.Name ?? directCode);
            }
        }

        foreach (var code in ownedUnitCodes)
        {
            if (_units.TryGetValue(code, out var ownedUnit))
            {
                // 보유 유닛이 타겟의 하위 재료에 기여하는 분량 합산
                var ownedDecomp = GetDecomposition(code);
                ownedCommons.Add(ownedDecomp.BaseCommons);
            }
        }

        var targetCommonsDict = targetDecomp.BaseCommons.ToDictionary();
        var ownedCommonsDict = ownedCommons.ToDictionary();
        var missingCommons = new Dictionary<string, int>();

        int fulfilledCommonsCount = 0;
        int missingCommonsCount = 0;

        foreach (var (name, reqCount) in targetCommonsDict)
        {
            int owned = ownedCommonsDict.GetValueOrDefault(name, 0);
            int fulfilled = Math.Min(reqCount, owned);
            int missing = Math.Max(0, reqCount - owned);

            fulfilledCommonsCount += fulfilled;
            missingCommonsCount += missing;

            if (missing > 0)
            {
                missingCommons[name] = missing;
            }
        }

        double readinessPct = totalRequired > 0 
            ? Math.Round((double)fulfilledCommonsCount / totalRequired * 100.0, 1) 
            : 0.0;

        return new InventoryReadiness
        {
            TargetUnitCode = targetUnitCode,
            TargetUnitName = targetName,
            ReadinessPercentage = Math.Min(100.0, readinessPct),
            TotalRequiredCommons = totalRequired,
            TotalFulfilledCommons = fulfilledCommonsCount,
            TotalMissingCommons = missingCommonsCount,
            MissingCommons = missingCommons,
            MissingDirectUnits = missingDirect
        };
    }

    private static bool IsBaseCommon(string code) =>
        code is "C1" or "C2" or "C3" or "C4" or "C5" or "C6" or "C7" or "C8" or "C9";
}
