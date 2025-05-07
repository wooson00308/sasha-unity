using System;

namespace AF.Combat
{
    [Flags]
    public enum LogLevelFlags
    {
        Nothing = 0,
        Debug = 1 << 0,
        Info = 1 << 1,
        Success = 1 << 2,
        Warning = 1 << 3,
        Error = 1 << 4,
        Danger = 1 << 5,
        Critical = 1 << 6,
        System = 1 << 7,
        Everything = ~0 // 모든 비트 켬
    }
} 