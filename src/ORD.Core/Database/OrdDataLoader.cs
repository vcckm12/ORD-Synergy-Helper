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

        int startIndex = jsContent.IndexOf("const unitDB = [");
        if (startIndex == -1) return GetFallbackUnits();

        int endIndex = jsContent.IndexOf("];", startIndex);
        if (endIndex == -1) endIndex = jsContent.Length;

        string dbSub = jsContent.Substring(startIndex, endIndex - startIndex);

        var objRegex = new Regex(@"\{([^{}]+(?:\{[^{}]*\}[^{}]*)*)\}", RegexOptions.Singleline);
        var matches = objRegex.Matches(dbSub);

        foreach (Match match in matches)
        {
            string block = match.Value;

            var codeMatch = Regex.Match(block, @"code\s*:\s*""([^""]+)""");
            if (!codeMatch.Success) continue;
            string code = codeMatch.Groups[1].Value.Trim();

            if (block.Contains("hide: true")) continue;

            var nameMatch = Regex.Match(block, @"name\s*:\s*""([^""]+)""");
            string name = nameMatch.Success ? nameMatch.Groups[1].Value.Trim() : code;

            var gradeMatch = Regex.Match(block, @"grade\s*:\s*""([^""]+)""");
            string grade = gradeMatch.Success ? gradeMatch.Groups[1].Value.Trim() : "common";

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

        // 오로성 3대 핵심 유닛 등록 (워큐리, 나스쥬로, 새턴)
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
            // 1. 방어력 감소 (마법 방어력 제외)
            if (!stat.Contains("마법") && !stat.Contains("마방"))
            {
                var m1 = Regex.Match(stat, @"방어력\s*감소\s*[-]?(\d+)");
                if (m1.Success && int.TryParse(m1.Groups[1].Value, out int a1)) syn.ArmorReduction = Math.Max(syn.ArmorReduction, a1);

                var m2 = Regex.Match(stat, @"방어력감소\s*[-]?(\d+)");
                if (m2.Success && int.TryParse(m2.Groups[1].Value, out int a2)) syn.ArmorReduction = Math.Max(syn.ArmorReduction, a2);

                var m3 = Regex.Match(stat, @"방어력\s*[-]?(\d+)\s*(?:추가\s*)?감소");
                if (m3.Success && int.TryParse(m3.Groups[1].Value, out int a3)) syn.ArmorReduction = Math.Max(syn.ArmorReduction, a3);

                var m4 = Regex.Match(stat, @"적\s*방어력\s*[-]?(\d+)\s*감소");
                if (m4.Success && int.TryParse(m4.Groups[1].Value, out int a4)) syn.ArmorReduction = Math.Max(syn.ArmorReduction, a4);

                var m5 = Regex.Match(stat, @"방깎\s*[-]?(\d+)");
                if (m5.Success && int.TryParse(m5.Groups[1].Value, out int a5)) syn.ArmorReduction = Math.Max(syn.ArmorReduction, a5);
            }

            // 2. 아머브레이크
            var abMatch = Regex.Match(stat, @"아머브레이크\s*(\d+)");
            if (abMatch.Success && int.TryParse(abMatch.Groups[1].Value, out int ab))
            {
                syn.ArmorBreakOverlap = Math.Max(syn.ArmorBreakOverlap, ab);
            }
            if (stat.Contains("아머브레이크") && stat.Contains("최대 75"))
            {
                syn.ArmorBreakOverlap = Math.Max(syn.ArmorBreakOverlap, 20);
            }

            // 3. 이동속도 감소 (다양한 한국어 표기 완벽 파싱)
            var s1 = Regex.Match(stat, @"이동속도\s*감소\s*(\d+)%");
            if (s1.Success && int.TryParse(s1.Groups[1].Value, out int sl1)) syn.MovementSlow = Math.Max(syn.MovementSlow, sl1);

            var s2 = Regex.Match(stat, @"이동속도감소\s*(\d+)%");
            if (s2.Success && int.TryParse(s2.Groups[1].Value, out int sl2)) syn.MovementSlow = Math.Max(syn.MovementSlow, sl2);

            var s3 = Regex.Match(stat, @"이동속도\s*(\d+)%\s*감소");
            if (s3.Success && int.TryParse(s3.Groups[1].Value, out int sl3)) syn.MovementSlow = Math.Max(syn.MovementSlow, sl3);

            var s4 = Regex.Match(stat, @"(\d+)%\s*(?:의\s*)?이동속도\s*감소");
            if (s4.Success && int.TryParse(s4.Groups[1].Value, out int sl4)) syn.MovementSlow = Math.Max(syn.MovementSlow, sl4);

            var s5 = Regex.Match(stat, @"이감\s*(\d+)%");
            if (s5.Success && int.TryParse(s5.Groups[1].Value, out int sl5)) syn.MovementSlow = Math.Max(syn.MovementSlow, sl5);

            var s6 = Regex.Match(stat, @"둔화\s*(\d+)%");
            if (s6.Success && int.TryParse(s6.Groups[1].Value, out int sl6)) syn.MovementSlow = Math.Max(syn.MovementSlow, sl6);

            // 4. 스턴
            var stunMatch = Regex.Match(stat, @"(\d+(?:\.\d+)?)\s*초\s*스턴");
            if (stunMatch.Success && double.TryParse(stunMatch.Groups[1].Value, out double sec))
            {
                if (sec >= 2.0) syn.StunValue = Math.Max(syn.StunValue, 1.0);
                else if (sec >= 0.4) syn.StunValue = Math.Max(syn.StunValue, 0.5);
            }
            else if (stat.Contains("단일스턴") || stat.Contains("범위스턴") || stat.Contains("단일 적") && stat.Contains("스턴"))
            {
                syn.StunValue = Math.Max(syn.StunValue, 0.5);
            }

            // 5. 마법 방어력 감소 및 데미지 증폭
            var magicArmorMatch = Regex.Match(stat, @"마법\s*방어력\s*감소\s*(\d+)%");
            if (magicArmorMatch.Success && int.TryParse(magicArmorMatch.Groups[1].Value, out int ma)) syn.MagicArmorReduction = Math.Max(syn.MagicArmorReduction, ma);

            var ma2 = Regex.Match(stat, @"마법방어력\s*감소\s*(\d+)%");
            if (ma2.Success && int.TryParse(ma2.Groups[1].Value, out int ma2Val)) syn.MagicArmorReduction = Math.Max(syn.MagicArmorReduction, ma2Val);

            var ma3 = Regex.Match(stat, @"마방깎\s*(\d+)%");
            if (ma3.Success && int.TryParse(ma3.Groups[1].Value, out int ma3Val)) syn.MagicArmorReduction = Math.Max(syn.MagicArmorReduction, ma3Val);

            var maAmp = Regex.Match(stat, @"마법\s*데미지\s*(\d+)%\s*증폭");
            if (maAmp.Success && int.TryParse(maAmp.Groups[1].Value, out int amp)) syn.MagicArmorReduction = Math.Max(syn.MagicArmorReduction, amp);

            var expAmp = Regex.Match(stat, @"폭발형\s*데미지\s*(\d+)%\s*증폭");
            if (expAmp.Success && int.TryParse(expAmp.Groups[1].Value, out int eAmp)) syn.MagicArmorReduction = Math.Max(syn.MagicArmorReduction, eAmp);

            // 6. 끝딜 (체력 비례 데미지)
            if (stat.Contains("현재체력") || stat.Contains("전체체력") || stat.Contains("잃은체력"))
            {
                syn.HasPercentDamage = true;
            }

            // 7. 보스딜러
            if (stat.Contains("보스") && (stat.Contains("%") || stat.Contains("추가")))
            {
                syn.HasBossKiller = true;
            }

            // 8. 공격력/공속 버프
            if (stat.Contains("공격력 증가") || stat.Contains("공격속도 증가") || stat.Contains("공격력증가") || stat.Contains("공격속도증가"))
            {
                syn.HasAttackBuff = true;
            }

            // 9. 삭제 / 처형
            if (stat.Contains("삭제") || stat.Contains("처형") || stat.Contains("즉사"))
            {
                syn.HasSingleTargetDamage = true;
            }

            // 10. 마나 시너지
            if (stat.Contains("마나재생") || stat.Contains("마나회복") || stat.Contains("마나 초당"))
            {
                syn.HasManaSkillSynergy = true;
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
            // === 초월함 (Transcendence) ===
            case "TR1": // 검은수염 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 75;
                unit.Synergy.MagicArmorReduction = 10;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "TR2": // 나미 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.MagicArmorReduction = 2;
                break;
            case "TR3": // 도플라밍고 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 60;
                unit.Synergy.MovementSlow = 75;
                break;
            case "TR4": // 로빈 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 45;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "TR5": // 로우 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "TR6": // 료쿠규 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 50;
                unit.Synergy.MovementSlow = 25;
                break;
            case "TR7": // 루치 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasSingleTargetDamage = true;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "TR8": // 루피 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 10;
                unit.Synergy.ArmorBreakOverlap = 25;
                unit.Synergy.StunValue = 0.5;
                break;
            case "TR9": // 바질호킨스 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.MovementSlow = 15;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR10": // 브룩 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 20;
                unit.Synergy.MagicArmorReduction = 13;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR11": // 사보 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.MovementSlow = 35;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR12": // 상디 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 50;
                unit.Synergy.StunValue = 0.5;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "TR13": // 샹크스 초월
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.StunValue = 1.5;
                break;
            case "TR14": // 스네이크맨 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR15": // 시라호시 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.5;
                unit.Synergy.HasPercentDamage = true;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "TR16": // 아오키지 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 70;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR17": // 아카이누 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 12;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasBossKiller = true;
                break;
            case "TR18": // 야마토 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR19": // 우솝 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR20": // 조로 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.ArmorBreakOverlap = 10;
                unit.Synergy.MovementSlow = 35;
                break;
            case "TR21": // 보니 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 40;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR22": // 징베 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.ArmorBreakOverlap = 25;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasBossKiller = true;
                break;
            case "TR23": // 쵸파 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 50;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "TR24": // 코비 초월
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.HasAttackBuff = true;
                unit.Synergy.StunValue = 0.5;
                break;
            case "TR25": // 키드 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 35;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR26": // 키자루 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MagicArmorReduction = 15;
                unit.Synergy.StunValue = 1.0;
                break;
            case "TR27": // 타시기 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.ArmorBreakOverlap = 20;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "TR28": // 프랑키 초월
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.MagicArmorReduction = 5;
                break;
            case "TR29": // 후지토라 초월
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.MovementSlow = 55;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.ArmorBreakOverlap = 15;
                break;
            case "TR30": // 베가펑크 초월
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MagicArmorReduction = 20;
                unit.Synergy.MovementSlow = 40;
                unit.Synergy.StunValue = 1.0;
                break;

            // === 불멸함 (Immortal) ===
            case "I1": // 거프 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "I2": // 드래곤 불멸
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                unit.Synergy.MagicArmorReduction = 20;
                break;
            case "I3": // 레일리 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 20;
                unit.Synergy.ArmorBreakOverlap = 5;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "I4": // 로져 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 60;
                unit.Synergy.MovementSlow = 50;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "I5": // 불릿 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorBreakOverlap = 15;
                unit.Synergy.StunValue = 1.0;
                break;
            case "I6": // 빅맘 불멸
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 70;
                unit.Synergy.StunValue = 1.0;
                break;
            case "I7": // 센고쿠 불멸
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "I8": // 스코퍼가반 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 60;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "I9": // 시키 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 40;
                unit.Synergy.StunValue = 1.0;
                break;
            case "I10": // 제트 불멸
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 35;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "I11": // 카이도 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.ArmorBreakOverlap = 30;
                unit.Synergy.MovementSlow = 60;
                unit.Synergy.StunValue = 1.0;
                break;
            case "I12": // 흰수염 불멸
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 45;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.StunValue = 1.0;
                break;

            // === 영원한 (Eternal) ===
            case "E1": // 니카 (루피)
            case "E2": // 니카 (스네이크)
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.HasAttackBuff = true;
                unit.Synergy.StunValue = 1.0;
                break;
            case "E4": // 미호크 영원
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "E5": // 버기 영원
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.MovementSlow = 25;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "E7": // 에이스 영원
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.StunValue = 1.5;
                break;
            case "E8": // 오뎅 영원
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.HasAttackBuff = true;
                unit.Synergy.StunValue = 1.0;
                break;
            case "E9": // 우타 영원
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 20;
                unit.Synergy.StunValue = 0.5;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "E10": // 카번딧슈 영원
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.ArmorBreakOverlap = 5;
                unit.Synergy.StunValue = 1.0;
                break;
            case "E12": // 핸콕 영원
                unit.PrimaryDamageType = DamageType.Hybrid;
                unit.Synergy.ArmorReduction = 40;
                unit.Synergy.MovementSlow = 60;
                unit.Synergy.MagicArmorReduction = 5;
                unit.Synergy.StunValue = 1.0;
                break;

            // === 제한됨 (Limited) ===
            case "Z2": // 레베카 제한
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 38;
                unit.Synergy.MovementSlow = 50;
                unit.Synergy.StunValue = 1.0;
                break;
            case "Z3": // 마르코 제한
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.MovementSlow = 45;
                unit.Synergy.StunValue = 1.0;
                break;
            case "Z6": // 알비다 제한
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.ArmorBreakOverlap = 10;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "Z8": // 카타쿠리 제한
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.ArmorBreakOverlap = 6;
                unit.Synergy.StunValue = 0.5;
                break;
            case "Z9": // 크로커다일 제한
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.MovementSlow = 40;
                unit.Synergy.StunValue = 1.0;
                break;
            case "Z10": // 킹 제한
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.ArmorBreakOverlap = 30;
                unit.Synergy.MovementSlow = 45;
                break;

            // === 전설 (Legendary) ===
            case "L2": // 나미 전설
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 42;
                unit.Synergy.MagicArmorReduction = 1;
                break;
            case "L4": // 드래곤 전설
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.ArmorReduction = 10;
                unit.Synergy.MovementSlow = 10;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "L6": // 레이쥬 전설
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.MovementSlow = 50;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "L7": // 레일리 전설
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 20;
                unit.Synergy.ArmorBreakOverlap = 3;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "L13": // 바르톨로메오 전설
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.ArmorReduction = 12;
                break;
            case "L19": // 슈가 전설
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.HasPercentDamage = true;
                break;
            case "L21": // 시노부 전설
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 30;
                unit.Synergy.MovementSlow = 15;
                unit.Synergy.StunValue = 0.5;
                break;
            case "L24": // 에이스 전설
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 33;
                unit.Synergy.StunValue = 1.0;
                break;
            case "L34": // 킹 전설
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                unit.Synergy.MovementSlow = 55;
                break;
            case "L37": // 후지토라 전설
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.MovementSlow = 25;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.ArmorReduction = 12;
                break;

            // === 히든 (Hidden) ===
            case "H1": // 검호 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 25;
                break;
            case "H2": // 레드포스호 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 20;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "H3": // 레베카 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 18;
                break;
            case "H10": // 베르고 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 35;
                unit.Synergy.ArmorBreakOverlap = 20;
                unit.Synergy.StunValue = 1.0;
                break;
            case "H11": // 봉쿠레 히든
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.ArmorReduction = 11;
                unit.Synergy.StunValue = 0.5;
                break;
            case "H12": // 사보 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 20;
                unit.Synergy.MovementSlow = 25;
                break;
            case "H16": // 아오키지 히든
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.MovementSlow = 35;
                unit.Synergy.StunValue = 1.0;
                break;
            case "H18": // 이완코브 히든
                unit.PrimaryDamageType = DamageType.Support;
                unit.Synergy.ArmorReduction = 11;
                unit.Synergy.StunValue = 0.5;
                unit.Synergy.HasAttackBuff = true;
                break;
            case "H21": // 킬러 히든
                unit.PrimaryDamageType = DamageType.Physical;
                unit.Synergy.ArmorReduction = 12;
                unit.Synergy.HasBossKiller = true;
                break;

            // === 세라핌 (Seraphim) ===
            case "SR4": // 세라핌 S-베어 (쿠라핌)
                unit.PrimaryDamageType = DamageType.Magic;
                unit.Synergy.StunValue = 1.0;
                unit.Synergy.HasPercentDamage = true;
                unit.Synergy.MovementSlow = 30;
                break;
        }
    }

    private static void AddGoroseiUnits(List<OrdUnit> list)
    {
        if (list.Any(u => u.Id == "gorosei_warcury")) return;

        // 1. 오로성 워큐리 (법무무신): -15방깎, -15마방깎, 보스 체력 1500만
        list.Add(new OrdUnit
        {
            Id = "gorosei_warcury",
            Name = "오로성 워큐리",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy
            {
                ArmorReduction = 15,
                MagicArmorReduction = 15,
                HasBossKiller = true
            }
        });

        // 2. 오로성 나스쥬로 (재무무신): -15이감, -15공속, 라인 체력 1500만
        list.Add(new OrdUnit
        {
            Id = "gorosei_nusjuro",
            Name = "오로성 나스쥬로",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy
            {
                MovementSlow = 15
            }
        });

        // 3. 오로성 새턴 (과학방위무신): +10% 폭뎀증, 적 체젠 35만 억제, 아군 공증 30%
        list.Add(new OrdUnit
        {
            Id = "gorosei_saturn",
            Name = "오로성 새턴",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy
            {
                MagicArmorReduction = 10,  // 폭뎀증 10%
                HasPercentDamage = true,   // 적 체젠 35만 억제 및 끝딜 효과
                HasAttackBuff = true       // 공증 30%
            }
        });
    }

    private static List<OrdUnit> GetFallbackUnits()
    {
        var list = new List<OrdUnit>
        {
            new() { Id = "TR8", Name = "루피 (초월함)", Tier = UnitTier.Transcendence, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 10, ArmorBreakOverlap = 25, StunValue = 0.5 } },
            new() { Id = "TR20", Name = "조로 (초월함)", Tier = UnitTier.Transcendence, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 10, MovementSlow = 35 } },
            new() { Id = "TR22", Name = "징베 (초월함)", Tier = UnitTier.Transcendence, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 25, ArmorBreakOverlap = 25, StunValue = 1.0, HasBossKiller = true } },
            new() { Id = "Z10", Name = "킹 (제한됨)", Tier = UnitTier.Limited, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 30, ArmorBreakOverlap = 30, MovementSlow = 45 } },
            new() { Id = "L21", Name = "시노부 (전설)", Tier = UnitTier.Legendary, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 30, MovementSlow = 15 } },
            new() { Id = "H10", Name = "베르고 (히든)", Tier = UnitTier.Hidden, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 20, StunValue = 1.0 } },
            new() { Id = "L37", Name = "후지토라 (전설)", Tier = UnitTier.Legendary, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { MovementSlow = 25, StunValue = 1.0, ArmorReduction = 12 } },
            new() { Id = "L4", Name = "드래곤 (전설)", Tier = UnitTier.Legendary, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { StunValue = 1.0, ArmorReduction = 10, MovementSlow = 10, HasAttackBuff = true } },
            new() { Id = "gorosei_warcury", Name = "오로성 워큐리", Tier = UnitTier.Gorosei, PrimaryDamageType = DamageType.Physical, Synergy = new UnitSynergy { ArmorReduction = 15 } },
            new() { Id = "gorosei_nusjuro", Name = "오로성 나스쥬로", Tier = UnitTier.Gorosei, PrimaryDamageType = DamageType.Support, Synergy = new UnitSynergy { MovementSlow = 15 } },
            new() { Id = "gorosei_saturn", Name = "오로성 새턴", Tier = UnitTier.Gorosei, PrimaryDamageType = DamageType.Magic, Synergy = new UnitSynergy { MagicArmorReduction = 10, HasPercentDamage = true, HasAttackBuff = true } }
        };
        return list;
    }
}
