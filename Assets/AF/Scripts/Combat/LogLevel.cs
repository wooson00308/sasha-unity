namespace AF.Combat
{
    /// <summary>
    /// 로그 레벨 열거형
    /// </summary>
    public enum LogLevel
    {
        Debug,      // 상세 디버깅 정보 (추가됨)
        Info,       // 일반 정보
        Success,    // 성공/긍정적 결과
        Warning,    // 주의/경고
        Error,      // 오류 발생 (추가됨)
        Danger,     // 위험/부정적 결과
        Critical,   // 치명적/중요 이벤트
        System      // 시스템 메시지
    }
} 