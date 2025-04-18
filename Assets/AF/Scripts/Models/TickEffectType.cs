namespace AF.Models
{
    /// <summary>
    /// 턴마다 발생하는 효과의 종류
    /// </summary>
    public enum TickEffectType
    {
        None,
        DamageOverTime, // 턴당 데미지
        HealOverTime    // 턴당 회복
        // 필요에 따라 추가 (예: AP 회복/소모, 특정 효과 확률 증가/감소 등)
    }
} 
