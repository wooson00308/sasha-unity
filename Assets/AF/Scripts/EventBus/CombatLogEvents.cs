using System.Collections.Generic;
using AF.Models;
using AF.Combat; // For LogEntry

namespace AF.EventBus
{
    /// <summary>
    /// Event published by CombatTextUIService during log playback
    /// to synchronize other UI elements like CombatRadarUIService.
    /// </summary>
    public class CombatLogPlaybackUpdateEvent : IEvent
    {
        /// <summary>
        /// The current snapshot of all unit states relevant to this log entry.
        /// Key is the unit name (string).
        /// </summary>
        public Dictionary<string, ArmoredFrameSnapshot> CurrentSnapshot { get; private set; }

        /// <summary>
        /// The name of the unit that should be highlighted or is the context of the current log entry.
        /// Can be null if there's no specific context unit (e.g., system messages).
        /// </summary>
        public string ActiveUnitName { get; private set; }

        /// <summary>
        /// The specific log entry that triggered this update.
        /// Can be used by subscribers to determine what specific action/change occurred.
        /// </summary>
        public TextLogger.LogEntry CurrentLogEntry { get; private set; }

        public CombatLogPlaybackUpdateEvent(
            Dictionary<string, ArmoredFrameSnapshot> currentSnapshot,
            string activeUnitName,
            TextLogger.LogEntry currentLogEntry)
        {
            CurrentSnapshot = currentSnapshot;
            ActiveUnitName = activeUnitName;
            CurrentLogEntry = currentLogEntry;
        }
    }
} 