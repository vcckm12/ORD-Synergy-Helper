namespace ORD.Core.Models;

public enum DamageType
{
    None = 0,
    Physical,   // 물딜 (방깎, 암브, 이감, 공버프 위주)
    Magic,      // 마딜 (단일, 끝딜, 마방깎, 스턴 위주)
    Hybrid,     // 하이브리드
    Support     // 순수 유틸/서포터 (스턴, 이감 등)
}

public enum UnitTier
{
    Common = 1,         // 흔함
    Uncommon = 2,       // 안흔함
    Special = 3,        // 특별함
    Rare = 4,           // 희귀함
    Hidden = 5,         // 히든
    Change = 6,         // 변화/변이
    Legendary = 7,      // 전설
    Limited = 8,        // 제한됨
    Transcendence = 9,  // 초월함
    Immortal = 10,      // 불멸함
    Eternal = 11,       // 영원함
    RandomExclusive = 12, // 랜디/기타
    Gorosei = 13        // 오로성
}
