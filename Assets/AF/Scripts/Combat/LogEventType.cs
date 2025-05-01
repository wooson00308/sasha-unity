namespace AF.Combat
{
    /// <summary>
    /// 로그 항목의 이벤트 유형을 나타내는 열거형입니다.
    /// </summary>
    public enum LogEventType
    {
        Unknown = 0,
        SystemMessage,      // 시스템 메시지 (초기화, 종료 등)
        CombatStart,
        CombatEnd,
        RoundStart,         // === 전체 스냅샷 저장 시점 ===
        RoundEnd,
        UnitActivationStart,// === 전체 스냅샷 저장 시점 ===
        UnitActivationEnd,
        ActionStart,        // (델타 정보 저장 대상)
        ActionCompleted,    // (델타 정보 저장 대상)
        WeaponFired,        // (델타 정보 저장 대상)
        DamageApplied,      // (델타 정보 저장 대상)
        DamageAvoided,      // (델타 정보 저장 대상)
        PartDestroyed,      // (델타 정보 저장 대상)
        StatusEffectApplied,// (델타 정보 저장 대상)
        StatusEffectExpired,// (델타 정보 저장 대상)
        StatusEffectTicked, // (델타 정보 저장 대상)
        RepairApplied       // (델타 정보 저장 대상)
    }
} 