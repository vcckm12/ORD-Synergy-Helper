using ORD.Core.Models;
using ORD.Core.Recipes;

namespace ORD.Core.Database;

public class UnitRepository
{
    private readonly Dictionary<string, OrdUnit> _units = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase);
    public RecipeGraph RecipeGraph { get; } = new();

    public UnitRepository()
    {
        InitializeAliases();
        var loaded = OrdDataLoader.LoadUnits();
        foreach (var u in loaded)
        {
            _units[u.Id] = u;
        }

        RecipeGraph.Initialize(_units.Values);
    }

    public OrdUnit? GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;

        if (_units.TryGetValue(id, out var directUnit))
            return directUnit;

        if (_aliases.TryGetValue(id, out var realCode) && _units.TryGetValue(realCode, out var aliasedUnit))
            return aliasedUnit;

        // 이름으로 매칭 시도
        var byName = _units.Values.FirstOrDefault(u => u.Name.Equals(id, StringComparison.OrdinalIgnoreCase) || u.Name.StartsWith(id, StringComparison.OrdinalIgnoreCase));
        return byName;
    }

    public IEnumerable<OrdUnit> GetAll() => _units.Values;

    public InventoryReadiness CalculateReadiness(string targetUnitCode, IEnumerable<string> ownedCodes)
    {
        string realCode = ResolveCode(targetUnitCode);
        var resolvedOwned = ownedCodes.Select(ResolveCode).Distinct();
        return RecipeGraph.CalculateReadiness(realCode, resolvedOwned);
    }

    public MaterialDecomposition GetDecomposition(string unitCode)
    {
        string realCode = ResolveCode(unitCode);
        return RecipeGraph.GetDecomposition(realCode);
    }

    public string ResolveCode(string codeOrAlias)
    {
        if (_aliases.TryGetValue(codeOrAlias, out var realCode)) return realCode;
        return codeOrAlias;
    }

    private void InitializeAliases()
    {
        _aliases["luffy_trans"] = "TR8";
        _aliases["zoro_trans"] = "TR20";
        _aliases["jinbe_trans"] = "TR22";
        _aliases["king_limit"] = "Z10";
        _aliases["kaido_limit"] = "I11";
        _aliases["hawkins_trans"] = "TR9";
        _aliases["uta_trans"] = "E9";
        _aliases["vegapunk_limit"] = "TR30";
        _aliases["seraphim_kuma"] = "SR4";
        _aliases["aokiji_trans"] = "TR16";
        _aliases["shirahoshi_trans"] = "TR15";
        _aliases["shinobu_leg"] = "L21";
        _aliases["vergo_hid"] = "H10";
        _aliases["fujitora_leg"] = "L37";
        _aliases["dragon_leg"] = "L4";
        _aliases["shiki_immo"] = "I9";
        _aliases["bartolomeo_leg"] = "L13";
        _aliases["reiju_leg"] = "L6";
        _aliases["lucci_leg"] = "L9";
        _aliases["killer_hid"] = "H21";
        _aliases["ivankov_hid"] = "H18";
        _aliases["bonkure_hid"] = "H11";
        _aliases["rebecca_hid"] = "H3";
        _aliases["smoker_leg"] = "L20";
        _aliases["moria_leg"] = "L12";
        _aliases["ace_chg"] = "D5";
        _aliases["king_leg"] = "L34";
    }
}
