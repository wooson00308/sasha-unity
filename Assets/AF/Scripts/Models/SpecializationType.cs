namespace AF.Models
{
    /// <summary>
    /// 파일럿의 전문화 유형을 정의합니다.
    /// </summary>
    public enum SpecializationType
    {
        //Combat, // 기존 Combat -> StandardCombat으로 변경 또는 역할 통합
        StandardCombat, // 표준 전투: 상황에 맞춰 근/중/원거리 교전 시도
        MeleeCombat,    // 근접 전투: 적에게 접근하여 근접 공격 선호
        RangedCombat,   // 원거리 전투: 적정 사거리 유지하며 원거리 공격 선호
        Defense,        // 방어 전문화
        Support,        // 지원 전문화
        Engineering,    // 기계 전문화
        Evasion         // 회피 전문화
    }
} 