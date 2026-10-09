using System.Text.RegularExpressions;
using ORD.Core.Models;

namespace ORD.Core.Database;

public static class OrdDataLoader
{
    public static List<OrdUnit> LoadUnits(string? dataFilePath = null)
    {
        string path = ResolveDataPath(dataFilePath);
        if (!File.Exists(path))
        {
            Console.WriteLine($"[OrdDataLoader] Data file not found at: {path}. Using fallback predefined units.");
            return GetFallbackUnits();
        }

        try
        {
            string content = File.ReadAllText(path);
            var units = ParseOrdrDataJs(content);
            Console.WriteLine($"[OrdDataLoader] Successfully parsed {units.Count} units from v2.323 database.");
            return units;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OrdDataLoader] Error parsing data file: {ex.Message}. Using fallback predefined units.");
            return GetFallbackUnits();
        }
    }

    private static string ResolveDataPath(string? customPath)
    {
        if (!string.IsNullOrEmpty(customPath) && File.Exists(customPath))
            return customPath;

        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "data", "ordr_data.js"),
            Path.Combine(Directory.GetCurrentDirectory(), "data", "ordr_data.js"),
            Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "data", "ordr_data.js"),
            @"C:\Users\vcckm\ORD-Synergy-Helper\data\ordr_data.js"
        };

        foreach (var c in candidates)
        {
            if (File.Exists(c)) return c;
        }

        return candidates[0];
    }

    public static List<OrdUnit> ParseOrdrDataJs(string jsContent)
    {
        var result = new List<OrdUnit>();

        // 1. unitDB 배열 영역 추출
        int startIndex = jsContent.IndexOf("const unitDB = [");
        if (startIndex == -1) return GetFallbackUnits();

        int endIndex = jsContent.IndexOf("];", startIndex);
        if (endIndex == -1) endIndex = jsContent.Length;

        string dbSub = jsContent.Substring(startIndex, endIndex - startIndex);

        // 2. 각 유닛 객체 블록 추출: { ... }
        // 정규식: 객체 하나씩 추출
        var objRegex = new Regex(@"\{([^{}]+(?:\{[^{}]*\}[^{}]*)*)\}", RegexOptions.Singleline);
        var matches = objRegex.Matches(dbSub);

        foreach (Match match in matches)
        {
            string block = match.Value;

            // code
            var codeMatch = Regex.Match(block, @"code\s*:\s*""([^""]+)""");
            if (!codeMatch.Success) continue;
            string code = codeMatch.Groups[1].Value.Trim();

            // hide 여부 체크
            if (block.Contains("hide: true")) continue;

            // name
            var nameMatch = Regex.Match(block, @"name\s*:\s*""([^""]+)""");
            string name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : code;

            // grade
            var gradeMatch = Regex.Match(block, @"grade\s*:\s*""([^""]+)""");
            string grade = gradeMatch.Success ? gradeMatch.Groups[1].Value.Trim() : "common";

            // req: { "C5": 1, ... }
            var recipe = new Dictionary<string, int>();
            var reqBlockMatch = Regex.Match(block, @"req\s*:\s*\{([^}]+)\}");
            if (reqBlockMatch.Success)
            {
                var reqItems = Regex.Matches(reqBlockMatch.Groups[1].Value, @"""([^""]+)""\s*:\s*(\d+)");
                foreach (Match rm in reqItems)
                {
                    string reqCode = rm.Groups[1].Value;
                    if (int.TryParse(rm.Groups[2].Value, out int count))
                    {
                        recipe[reqCode] = count;
                    }
                }
            }

            // stats: [ ... ]
            var stats = new List<string>();
            var statsBlockMatch = Regex.Match(block, @"stats\s*:\s*\[([\s\S]*?)\]");
            if (statsBlockMatch.Success)
            {
                var statItems = Regex.Matches(statsBlockMatch.Groups[1].Value, @"""([^""]+)""");
                foreach (Match sm in statItems)
                {
                    stats.Add(sm.Groups[1].Value);
                }
            }

            var unit = new OrdUnit
            {
                Id = code,
                Name = name,
                Tier = ParseTier(grade),
                Recipe = recipe,
                Synergy = ExtractSynergy(stats)
            };

            unit.PrimaryDamageType = DetermineDamageType(unit);
            ApplyMetaOverrides(unit);

            result.Add(unit);
        }

        // 오로성 유닛 추가 (v2.323 특수 픽)
        AddGoroseiUnits(result);

        return result;
    }

    private static UnitTier ParseTier(string grade) => grade.ToLowerInvariant() switch
    {
        "common" => UnitTier.Common,
        "uncommon" => UnitTier.Uncommon,
        "special" or "specialty" => UnitTier.Special,
        "rare" => UnitTier.Rare,
        "legendary" => UnitTier.Legendary,
        "hidden" => UnitTier.Hidden,
        "transmute" => UnitTier.Change,
        "limited" => UnitTier.Limited,
        "distorted" => UnitTier.Hidden,
        "transcend" => UnitTier.Transcendence,
        "immortal" => UnitTier.Immortal,
        "eternal" => UnitTier.Eternal,
        "seraphim" => UnitTier.Limited,
        "random" or "mystic" => UnitTier.RandomExclusive,
        _ => UnitTier.Common
    };

    private static UnitSynergy ExtractSynergy(List<string> stats)
    {
        var syn = new UnitSynergy();

        foreach (var stat in stats)
        {
            // 방어력 감소 (마법방어력 감소 제외)
            if (!stat.Contains("마법") && !stat.Contains("마방"))
            {
                var armorMatch = Regex.Match(stat, @"방어력\s*감소\s*(\d+)");
                if (armorMatch.Success && int.TryParse(armorMatch.Groups[1].Value, out int armor))
                {
                    syn.ArmorReduction = Math.Max(syn.ArmorReduction, armor);
                }
            }

            // 아머브레이크
            var abMatch = Regex.Match(stat, @"아머브레이크\s*(\d+)");
            if (abMatch.Success && int.TryParse(abMatch.Groups[1].Value, out int ab))
            {
                syn.ArmorBreakOverlap = Math.Max(syn.ArmorBreakOverlap, ab);
            }

            // 이동속도 감소
            var slowMatch = Regex.Match(stat, @"이동속도\s*감소\s*(\d+)%");
            if (slowMatch.Success && int.TryParse(slowMatch.Groups[1].Value, out int slow))
            {
                syn.MovementSlow = Math.Max(syn.MovementSlow, slow);
            }

            // 스턴
            var stunMatch = Regex.Match(stat, @"(\d+(?:\.\d+)?)\s*초\s*스턴");
            if (stunMatch.Success && double.TryParse(stunMatch.Groups[1].Value, out double sec))
            {
                if (sec >= 2.0) syn.StunValue = Math.Max(syn.StunValue, 1.0);
                else if (sec >= 0.5) syn.StunValue = Math.Max(syn.StunValue, 0.5);
            }
            else if (stat.Contains("단일스턴") || stat.Contains("범위스턴"))
            {
                syn.StunValue = Math.Max(syn.StunValue, 0.5);
            }

            // 마법 방어력 감소
            var magicArmorMatch = Regex.Match(stat, @"마법\s*방어력\s*감소\s*(\d+)%");
            if (magicArmorMatch.Success && int.TryParse(magicArmorMatch.Groups[1].Value, out int ma))
            {
                syn.MagicArmorReduction = Math.Max(syn.MagicArmorReduction, ma);
            }

            // 끝딜 (체력 비례 데미지)
            if (stat.Contains("현재체력") || stat.Contains("전체체력") || stat.Contains("잃은체력"))
            {
                syn.HasPercentDamage = true;
            }

            // 보스딜러
            if (stat.Contains("보스") && stat.Contains("%"))
            {
                syn.HasBossKiller = true;
            }

            // 공격력/공속 버프
            if (stat.Contains("공격력 증가") || stat.Contains("공격속도 증가") || stat.Contains("공격력증가") || stat.Contains("공격속도증가"))
            {
                syn.HasAttackBuff = true;
            }
        }

        return syn;
    }

    private static DamageType DetermineDamageType(OrdUnit unit)
    {
        if (unit.Synergy.ArmorReduction > 0 || unit.Synergy.ArmorBreakOverlap > 0)
        {
            if (unit.Synergy.MagicArmorReduction > 0) return DamageType.Hybrid;
            return DamageType.Physical;
        }

        if (unit.Synergy.MagicArmorReduction > 0 || unit.Synergy.HasPercentDamage)
        {
            return DamageType.Magic;
        }

        if (unit.Synergy.MovementSlow > 0 || unit.Synergy.StunValue > 0)
        {
            return DamageType.Support;
        }

        return DamageType.Physical;
    }

    private static void ApplyMetaOverrides(OrdUnit unit)
    {
        // 2.323 최신 메타 핵심 캐리 및 서포터 수치 보정
        switch (unit.Id)
        {
            case "TR8": // 루피 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 10;
                unit.Synergy.ArmorBreakOverlap = 25;
                unit.Synergy.StunValue = 0.5;
                break;
            case "TR20": // 조로 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.ArmorBreakOverlap = 10;
                unit.Synergy.MovementSlow = 35;
                break;
            case "TR22": // 징베 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.ArmorBreakOverlap = 25;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasBossKiller = true;
                break;
            case "Z10": // 킹 제한
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.ArmorBreakOverlap = 30;
                unit.Synergy.MovementSlow = 45;
                break;
            case "I11": // 카이도 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.ArmorBreakOverlap = 30;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR9": // 바질호킨스 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.MovementSlow = 15;
                unit.Synergy.StunValue = 1.0;
                break;
            case "E9": // 우타 영원
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 20;
                unit.Synergy.StunValue = 0.5;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "I9": // 시키 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 40;
                unit.Synergy.StunValue = 1.0;
                break;
            case "L21": // 시노부 전설
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.MovementSlow = 15;
                break;
            case "H10": // 베르고 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.ArmorBreakOverlap = 20;
                break;
            case "L37": // 후지토라 전설
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.MovementSlow = 25;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.ArmorReduction = 12;
                break;
            case "L4": // 드래곤 전설
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.ArmorReduction = 10;
                unit.Synergy.MovementSlow = 10;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "TR16": // 아오키지 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 70;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR15": // 시라호시 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.5;
                unit.Synergy.HasPercentDamage = true;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "SR4": // 세라핌 S-베어 (쿠라핌)
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasPercentDamage = true;
                unit.Synergy.MovementSlow = 30;
                break;
            case "TR30": // 베가펑크 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MagicArmorReduction = 20;
                unit.Synergy.MovementSlow = 40;
                unit.Synergy.StunValue = 1.0;
                break;
        }
    }

    private static void AddGoroseiUnits(List<OrdUnit> list)
    {
        if (list.Any(u => u.Id == "gorosei_warcury")) return;

        list.Add(new OrdUnit
        {
            Id = "gorosei_warcury",
            Name = "오로성 워큐리",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 15 }
        });

        list.Add(new OrdUnit
        {
            Id = "gorosei_nusjuro",
            Name = "오로성 나스쥬로",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { MovementSlow = 15 }
        });

        list.Add(new OrdUnit
        {
            Id = "gorosei_saturn",
            Name = "오로성 새턴",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { StunValue = 0.5 }
        });
    }

    private static List<OrdUnit> GetFallbackUnits()
    {
        // 파일 로드 실패 시 동작할 최소 핵심 12개 유닛
        var list = new List<OrdUnit>
        {
            new() { Id = "luffy_trans", Name = "루피 초월 (해적왕)", Tier = UnitTier.Transcendence, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 10, ArmorBreakOverlap = 25, StunValue = 0.5 } },
            new() { Id = "zoro_trans", Name = "조로 초월", Tier = UnitTier.Transcendence, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 10, MovementSlow = 35 } },
            new() { Id = "jinbe_trans", Name = "징베 초월", Tier = UnitTier.Transcendence, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 25, ArmorBreakOverlap = 25, StunValue = 1.0, HasBossKiller = true } },
            new() { Id = "king_limit", Name = "킹 제한 (킹제)", Tier = UnitTier.Limited, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 30, ArmorBreakOverlap = 30, MovementSlow = 45 } },
            new() { Id = "shinobu_leg", Name = "시노부 전설", Tier = UnitTier.Legendary, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 30, MovementSlow = 15 } },
            new() { Id = "vergo_hid", Name = "베르고 히든", Tier = UnitTier.Hidden, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 20 } },
            new() { Id = "fujitora_leg", Name = "후지토라 전설", Tier = UnitTier.Legendary, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { MovementSlow = 25, StunValue = 1.0, ArmorReduction = 12 } },
            new() { Id = "dragon_leg", Name = "드래곤 전설", Tier = UnitTier.Legendary, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { StunValue = 1.0, ArmorReduction = 10, MovementSlow = 10, HasAttackBuff = true } },
            new() { Id = "gorosei_warcury", Name = "오로성 워큐리", Tier = UnitTier.Gorosei, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 15 } },
            new() { Id = "gorosei_nusjuro", Name = "오로성 나스쥬로", Tier = UnitTier.Gorosei, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { MovementSlow = 15 } },
            new() { Id = "gorosei_saturn", Name = "오로성 새턴", Tier = UnitTier.Gorosei, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { StunValue = 0.5 } }
        };
        return list;
    }
}
