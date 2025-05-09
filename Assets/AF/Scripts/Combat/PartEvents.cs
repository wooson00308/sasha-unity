using UnityEngine;
using AF.Models;

namespace AF.Combat
{
    /// <summary>
    /// 부품 상태 및 파괴와 관련된 이벤트
    /// </summary>
    public class PartEvents
    {
        /// <summary>
        /// 부품 파괴 이벤트
        /// </summary>
        public class PartDestroyedEvent : ICombatEvent
        {
            public ArmoredFrame Frame { get; private set; }
            public PartType DestroyedPartType { get; private set; }
            public string DestroyedSlotId { get; private set; }
            public ArmoredFrame Destroyer { get; private set; }
            public string[] Effects { get; private set; }
            public bool FrameWasDestroyed { get; private set; }

            public PartDestroyedEvent(ArmoredFrame frame, PartType destroyedPartType,
                                     string destroyedSlotId, ArmoredFrame destroyer, 
                                     bool frameWasDestroyed, params string[] effects)
            {
                Frame = frame;
                DestroyedPartType = destroyedPartType;
                DestroyedSlotId = destroyedSlotId;
                Destroyer = destroyer;
                FrameWasDestroyed = frameWasDestroyed;
                Effects = effects;
            }
        }

        /// <summary>
        /// 부품 상태 변경 이벤트
        /// </summary>
        public class PartStatusChangedEvent : ICombatEvent
        {
            public enum StatusChangeType { Damaged, Overheated, Malfunctioning, Disabled, Repaired }

            public ArmoredFrame Frame { get; private set; }
            public PartType PartType { get; private set; }
            public StatusChangeType ChangeType { get; private set; }
            public float Severity { get; private set; } // 0.0f ~ 1.0f
            public string Description { get; private set; }

            public PartStatusChangedEvent(ArmoredFrame frame, PartType partType,
                                         StatusChangeType changeType, float severity,
                                         string description)
            {
                Frame = frame;
                PartType = partType;
                ChangeType = changeType;
                Severity = severity;
                Description = description;
            }
        }

        /// <summary>
        /// 시스템 치명적 오류 발생 이벤트
        /// </summary>
        public class SystemCriticalFailureEvent : ICombatEvent
        {
            public ArmoredFrame Frame { get; private set; }
            public string SystemName { get; private set; }
            public string FailureDescription { get; private set; }
            public int TurnDuration { get; private set; }
            public bool IsPermanent { get; private set; }

            public SystemCriticalFailureEvent(ArmoredFrame frame, string systemName,
                                             string failureDescription, int turnDuration,
                                             bool isPermanent)
            {
                Frame = frame;
                SystemName = systemName;
                FailureDescription = failureDescription;
                TurnDuration = turnDuration;
                IsPermanent = isPermanent;
            }
        }
    }
} 