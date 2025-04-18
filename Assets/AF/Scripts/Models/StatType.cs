namespace AF.Models
{
    /// <summary>
    /// 상태 효과가 영향을 미치는 스탯의 종류
    /// </summary>
    public enum StatType
    {
        None,
        AttackPower,
        Defense,
        Speed,
        Accuracy,
        Evasion,
        Durability, // 최대 내구도 자체를 변경할 수도 있음 (예: 임시 강화)
        EnergyEfficiency,
        MaxAP, // 최대 AP
        APRecovery // 턴당 AP 회복량
        // 필요에 따라 추가 (예: 받는 데미지 감소율, 특정 무기 데미지 등)
    }
} 