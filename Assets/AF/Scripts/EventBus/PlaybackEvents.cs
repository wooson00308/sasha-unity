using AF.Combat;
using AF.Models;
using System.Collections.Generic;
using UnityEngine;

namespace AF.EventBus
{
    // <<< Add IEvent interface definition if it's not globally accessible (assuming it is) >>>
    // public interface IEvent { }

    public static class PlaybackEvents
    {
        /// <summary>
        /// 플레이백 시작 시 또는 턴 시작 시 전체 상태 스냅샷 업데이트 알림
        /// </summary>
        public struct PlaybackSnapshotUpdateEvent : IEvent // <<< Implement IEvent
        {
            public Dictionary<string, ArmoredFrameSnapshot> SnapshotDict { get; }
            public ArmoredFrameSnapshot ActiveUnitSnapshot { get; } // 레이더 중심 계산용

            public PlaybackSnapshotUpdateEvent(Dictionary<string, ArmoredFrameSnapshot> snapshotDict, ArmoredFrameSnapshot activeUnitSnapshot)
            {
                SnapshotDict = snapshotDict;
                ActiveUnitSnapshot = activeUnitSnapshot;
            }
        }

        /// <summary>
        /// 유닛 이동 완료 알림
        /// </summary>
        public struct PlaybackUnitMoveEvent : IEvent // <<< Implement IEvent
        {
            public string UnitName { get; }
            public Vector3 NewPosition { get; }

            public PlaybackUnitMoveEvent(string unitName, Vector3 newPosition)
            {
                UnitName = unitName;
                NewPosition = newPosition;
            }
        }

        /// <summary>
        /// 유닛에게 데미지 적용 알림
        /// </summary>
        public struct PlaybackDamageEvent : IEvent // <<< Implement IEvent
        {
            public string TargetUnitName { get; }
            // Optional: Add partSlot, damageAmount if radar needs more specific visual cues
            // public string DamagedPartSlot { get; }
            // public float DamageAmount { get; }

            public PlaybackDamageEvent(string targetUnitName) // Simplified for basic flash
            {
                TargetUnitName = targetUnitName;
            }
        }

        /// <summary>
        /// 유닛 파츠 파괴 알림
        /// </summary>
        public struct PlaybackPartDestroyedEvent : IEvent // <<< Implement IEvent
        {
            public string OwnerName { get; }
            // Optional: Add partSlot if needed
            // public string DestroyedPartSlot { get; }

            public PlaybackPartDestroyedEvent(string ownerName) // Simplified
            {
                OwnerName = ownerName;
            }
        }

        /// <summary>
        /// 유닛에게 수리 적용 알림
        /// </summary>
        public struct PlaybackRepairEvent : IEvent // <<< Implement IEvent
        {
            public string TargetName { get; }

            public PlaybackRepairEvent(string targetName)
            {
                TargetName = targetName;
            }
        }

        /// <summary>
        /// 상태 이상 적용 알림 (간단한 펄스 효과용)
        /// </summary>
        public struct PlaybackStatusEffectEvent : IEvent // <<< Implement IEvent
        {
            public string TargetName { get; }

            public PlaybackStatusEffectEvent(string targetName)
            {
                TargetName = targetName;
            }
        }

        /// <summary>
        /// 이동 외 일반 액션 완료 알림 (간단한 펄스 효과용)
        /// </summary>
        public struct PlaybackGenericActionEvent : IEvent // <<< Implement IEvent
        {
            public string ActorName { get; }

            public PlaybackGenericActionEvent(string actorName)
            {
                ActorName = actorName;
            }
        }

         /// <summary>
        /// 플레이백 완료 알림
        /// </summary>
        public struct PlaybackCompleteEvent : IEvent {} // <<< Implement IEvent
    }
} 