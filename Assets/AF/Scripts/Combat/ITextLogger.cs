using AF.Combat;
using AF.Services;
using System.Collections.Generic;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 전투 과정을 텍스트 로그로 기록하는 로거 시스템의 인터페이스
    /// </summary>
    public interface ITextLogger : IService
    {
        /// <summary>
        /// 텍스트 로그 기록
        /// </summary>
        /// <param name="message">로그 메시지</param>
        /// <param name="level">로그 레벨</param>
        /// <param name="eventType">로그 이벤트 타입 (델타 로깅용)</param>
        /// <param name="contextUnit">로그와 관련된 유닛 (UI 업데이트용)</param>
        /// <param name="shouldUpdateTargetView">이 로그가 이벤트 대상 뷰 업데이트를 트리거해야 하는지 여부</param>
        /// <param name="turnStartStateSnapshot">턴 시작 시점의 유닛 상태 스냅샷 (턴 시작 로그용)</param>
        void Log(string message, LogLevel level = LogLevel.Info, LogEventType eventType = LogEventType.Unknown, ArmoredFrame contextUnit = null, bool shouldUpdateTargetView = false, Dictionary<string, ArmoredFrameSnapshot> turnStartStateSnapshot = null);

        /// <summary>
        /// 전투 이벤트 로깅
        /// </summary>
        /// <param name="combatEvent">로깅할 전투 이벤트</param>
        void LogEvent(ICombatEvent combatEvent);

        /// <summary>
        /// 저장된 모든 로그 가져오기
        /// </summary>
        /// <returns>로그 리스트</returns>
        List<string> GetLogs();

        /// <summary>
        /// 특정 로그 레벨의 로그만 필터링하여 가져오기
        /// </summary>
        /// <param name="level">필터링할 로그 레벨</param>
        /// <returns>필터링된 로그 리스트</returns>
        List<string> GetLogs(LogLevel level);

        /// <summary>
        /// 특정 검색어를 포함하는 로그만 필터링하여 가져오기
        /// </summary>
        /// <param name="searchTerm">검색어</param>
        /// <returns>필터링된 로그 리스트</returns>
        List<string> SearchLogs(string searchTerm);

        /// <summary>
        /// Filters and formats logs based on specified include/exclude flags.
        /// </summary>
        /// <param name="flagsToInclude">Array of LogLevelFlags to include. If null or empty, all levels are potentially included (unless excluded).</param>
        /// <param name="flagsToExclude">Array of LogLevelFlags to exclude. If null, no levels are excluded.</param>
        /// <returns>A list of formatted log strings matching the criteria.</returns>
        List<string> GetFormattedLogs(LogLevelFlags? flagsToInclude = null, LogLevelFlags? flagsToExclude = null);

        /// <summary>
        /// Gets the raw list of LogEntry objects.
        /// </summary>
        /// <returns>A list of LogEntry objects.</returns>
        List<TextLogger.LogEntry> GetLogEntries();

        /// <summary>
        /// 로그 초기화
        /// </summary>
        void Clear();

        /// <summary>
        /// 로그 파일로 저장
        /// </summary>
        /// <param name="filename">파일명</param>
        /// <returns>저장 성공 여부</returns>
        bool SaveToFile(string filename);

        /// <summary>
        /// 전투 요약 생성
        /// </summary>
        /// <returns>전투 요약 문자열</returns>
        string GenerateBattleSummary();
    }
} 