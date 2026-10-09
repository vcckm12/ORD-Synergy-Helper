using ORD.Core.Models;

namespace ORD.Core.Database;

public class UnitRepository
{
    private readonly Dictionary<string, OrdUnit> _units = new();

    public UnitRepository()
    {
        InitializeDefaultUnits();
    }

    public OrdUnit? GetById(string id) => _units.TryGetValue(id, out var u) ? u : null;
    public IEnumerable<OrdUnit> GetAll() => _units.Values;

    private void Add(OrdUnit unit) => _units[unit.Id] = unit;

    private void InitializeDefaultUnits()
    {
        // ==========================================
        // [최상위 유닛 - 초월함 / 불멸함 / 영원함]
        // ==========================================
        
        // 1. 루피 초월 (물딜 메인 캐리)
        Add(new OrdUnit
        {
            Id = "luffy_trans",
            Name = "루피 초월 (해적왕)",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 10, ArmorBreakOverlap = 25, MovementSlow = 0, StunValue = 0.5 }
        });

        // 2. 조로 초월 (물딜 메인 캐리)
        Add(new OrdUnit
        {
            Id = "zoro_trans",
            Name = "조로 초월",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 10, MovementSlow = 0, StunValue = 0.0 }
        });

        // 3. 시키 불멸 (물딜 메인 캐리 / 스턴)
        Add(new OrdUnit
        {
            Id = "shiki_immo",
            Name = "시키 불멸",
            Tier = UnitTier.Immortal,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 40, MovementSlow = 0, StunValue = 1.0 }
        });

        // 4. 징베 초월 (2.323 1티어 물딜 클트키 / 스턴 / 마나스킬)
        Add(new OrdUnit
        {
            Id = "jinbe_trans",
            Name = "징베 초월",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 20, ArmorBreakOverlap = 20, StunValue = 1.0, HasBossKiller = true }
        });

        // 5. 킹 제한 (2.323 징초+킹제 클트키 / 110깎 완성)
        Add(new OrdUnit
        {
            Id = "king_limit",
            Name = "킹 제한 (킹제)",
            Tier = UnitTier.Limited,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 30, ArmorBreakOverlap = 30, MovementSlow = 15 }
        });

        // 6. 카이도 제한 (2.323 악몽 유카0 1티어)
        Add(new OrdUnit
        {
            Id = "kaido_limit",
            Name = "카이도 제한 (카제)",
            Tier = UnitTier.Limited,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 20, StunValue = 0.5 }
        });

        // 7. 바질 호킨스 초월 (2.323 그블 스턴퉁)
        Add(new OrdUnit
        {
            Id = "hawkins_trans",
            Name = "바질 호킨스 초월 (바질초)",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 25, MovementSlow = 15, StunValue = 1.0 }
        });

        // 8. 우타 초월 (2.323 우초)
        Add(new OrdUnit
        {
            Id = "uta_trans",
            Name = "우타 초월 (우초)",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 20, StunValue = 0.5, HasAttackBuff = true }
        });

        // 9. 베가펑크 제한 (2.323 마딜 신규)
        Add(new OrdUnit
        {
            Id = "vegapunk_limit",
            Name = "베가펑크 제한 (베펑)",
            Tier = UnitTier.Limited,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { MagicArmorReduction = 20, MovementSlow = 20, StunValue = 0.5 }
        });

        // 10. 세라핌 쿠라핌 (2.323 마딜 4천왕)
        Add(new OrdUnit
        {
            Id = "seraphim_kuma",
            Name = "세라핌 S-베어 (쿠라핌)",
            Tier = UnitTier.Limited,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { StunValue = 1.0, HasPercentDamage = true }
        });

        // 11. 아오키지 초월 (마딜 메인 광역 / 이감)
        Add(new OrdUnit
        {
            Id = "aokiji_trans",
            Name = "아오키지 초월",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { MovementSlow = 70, StunValue = 1.0, MagicArmorReduction = 0 }
        });

        // 12. 시라호시 초월 (마딜 메인 광역 / 스턴)
        Add(new OrdUnit
        {
            Id = "shirahoshi_trans",
            Name = "시라호시 초월",
            Tier = UnitTier.Transcendence,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { MovementSlow = 0, StunValue = 1.5, HasAttackBuff = true }
        });

        // ==========================================
        // [핵심 서포트 - 전설 / 히든 / 변이]
        // ==========================================

        // [물딜 핵심 방깎 / 이감]
        Add(new OrdUnit
        {
            Id = "king_leg",
            Name = "킹 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 15, MovementSlow = 20 }
        });

        Add(new OrdUnit
        {
            Id = "shinobu_leg",
            Name = "시노부 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 30, MovementSlow = 15 }
        });

        Add(new OrdUnit
        {
            Id = "vergo_hid",
            Name = "베르고 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 35, ArmorBreakOverlap = 20 }
        });

        Add(new OrdUnit
        {
            Id = "ace_chg",
            Name = "에이스 변이",
            Tier = UnitTier.Change,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 30 }
        });

        // [스턴 / 이감 핵심]
        Add(new OrdUnit
        {
            Id = "fujitora_leg",
            Name = "후지토라 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { MovementSlow = 30, StunValue = 1.0, ArmorReduction = 12 }
        });

        Add(new OrdUnit
        {
            Id = "dragon_leg",
            Name = "드래곤 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { StunValue = 1.0, ArmorReduction = 10, HasAttackBuff = true }
        });

        Add(new OrdUnit
        {
            Id = "bartolomeo_leg",
            Name = "바르톨로메오 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { StunValue = 1.0, ArmorReduction = 12 }
        });

        // [마딜 핵심 끝딜 / 단일 / 보잡]
        Add(new OrdUnit
        {
            Id = "reiju_leg",
            Name = "레이쥬 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { HasPercentDamage = true, MovementSlow = 40 }
        });

        Add(new OrdUnit
        {
            Id = "lucci_leg",
            Name = "로브 루치 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { HasSingleTargetDamage = true, HasPercentDamage = true }
        });

        Add(new OrdUnit
        {
            Id = "killer_hid",
            Name = "킬러 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Hybrid,
            Synergy = new UnitSynergy { HasBossKiller = true, ArmorReduction = 12, MovementSlow = 0 }
        });

        Add(new OrdUnit
        {
            Id = "ivankov_hid",
            Name = "이완코브 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { StunValue = 1.0, ArmorReduction = 11, HasAttackBuff = true }
        });

        Add(new OrdUnit
        {
            Id = "bonkure_hid",
            Name = "봉쿠레 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { StunValue = 0.5, ArmorReduction = 11 }
        });

        Add(new OrdUnit
        {
            Id = "rebecca_hid",
            Name = "레베카 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 15, HasBossKiller = true }
        });

        Add(new OrdUnit
        {
            Id = "perona_hid",
            Name = "페로나 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { MovementSlow = 35, MagicArmorReduction = 10 }
        });

        Add(new OrdUnit
        {
            Id = "smoker_leg",
            Name = "스모커 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { MovementSlow = 25, ArmorReduction = 10 }
        });

        Add(new OrdUnit
        {
            Id = "koala_hid",
            Name = "코알라 히든",
            Tier = UnitTier.Hidden,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { ArmorBreakOverlap = 12, HasManaSkillSynergy = true }
        });

        Add(new OrdUnit
        {
            Id = "moria_leg",
            Name = "모리아 전설",
            Tier = UnitTier.Legendary,
            PrimaryDamageType = DamageType.Magic,
            Synergy = new UnitSynergy { MagicArmorReduction = 15, StunValue = 0.5 }
        });

        // ==========================================
        // [오로성 - 특수 서포터]
        // ==========================================
        Add(new OrdUnit
        {
            Id = "gorosei_warcury",
            Name = "오로성 워큐리",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Physical,
            Synergy = new UnitSynergy { ArmorReduction = 15 }
        });

        Add(new OrdUnit
        {
            Id = "gorosei_nusjuro",
            Name = "오로성 나스쥬로",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { MovementSlow = 15 }
        });

        Add(new OrdUnit
        {
            Id = "gorosei_saturn",
            Name = "오로성 새턴",
            Tier = UnitTier.Gorosei,
            PrimaryDamageType = DamageType.Support,
            Synergy = new UnitSynergy { StunValue = 0.5 }
        });
    }
}
