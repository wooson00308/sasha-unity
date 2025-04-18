namespace AF.Models
{
    /// <summary>
    /// 스탯 변경 방식
    /// </summary>
    public enum ModificationType
    {
        None,
        Additive,       // 값 덧셈 (Value 만큼 더함)
        Multiplicative  // 값 곱셈 (Value 만큼 곱함)
    }
} 