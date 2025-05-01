using AF.Models;
using AF.Services;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// 전투 시뮬레이션 서비스의 인터페이스
    /// 전투 진행 및 관리에 필요한 기본 기능을 정의합니다.
    /// </summary>
    public interface ICombatSimulatorService : IService
    {
        /// <summary>
        /// 현재 진행 중인 전투의 ID (없으면 null)
        /// </summary>
        string CurrentBattleId { get; }
        
        /// <summary>
        /// 전투가 진행 중인지 여부
        /// </summary>
        bool IsInCombat { get; }
        
        /// <summary>
        /// 현재 전투 턴 (라운드)
        /// </summary>
        int CurrentTurn { get; }
        
        /// <summary>
        /// 현재 턴(라운드) 내의 활성화 사이클 번호
        /// </summary>
        int CurrentCycle { get; }
        
        /// <summary>
        /// 현재 활성화된 유닛
        /// </summary>
        ArmoredFrame CurrentActiveUnit { get; }
        
        /// <summary>
        /// 전투 시작
        /// </summary>
        /// <param name="participants">전투 참가자들</param>
        /// <param name="battleName">전투 이름</param>
        /// <param name="autoProcess">자동 처리 여부</param>
        /// <returns>생성된 전투 ID</returns>
        string StartCombat(ArmoredFrame[] participants, string battleName, bool autoProcess = false);
        
        /// <summary>
        /// 현재 전투 종료
        /// </summary>
        /// <param name="forceResult">강제 결과 (null이면 자동 판정)</param>
        void EndCombat(CombatSessionEvents.CombatEndEvent.ResultType? forceResult = null);
        
        /// <summary>
        /// 다음 턴으로 진행
        /// </summary>
        /// <returns>턴이 성공적으로 진행되었는지 여부</returns>
        bool ProcessNextTurn();
        
        /// <summary>
        /// 유닛의 특정 행동을 수행합니다.
        /// </summary>
        /// <param name="actor">행동을 수행할 유닛</param>
        /// <param name="actionType">수행할 행동 유형</param>
        /// <param name="targetFrame">행동 대상 프레임 (nullable)</param>
        /// <param name="targetPosition">행동 목표 위치 (nullable)</param>
        /// <param name="weapon">사용할 무기 (nullable)</param>
        /// <returns>행동 성공 여부</returns>
        bool PerformAction(ArmoredFrame actor, CombatActionEvents.ActionType actionType, ArmoredFrame targetFrame, Vector3? targetPosition, Weapon weapon);
        
        /// <summary>
        /// 공격 수행
        /// </summary>
        /// <param name="attacker">공격자</param>
        /// <param name="target">대상</param>
        /// <param name="weapon">무기</param>
        /// <returns>공격 성공 여부</returns>
        bool PerformAttack(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon);
        
        /// <summary>
        /// 특정 유닛이 전투 불능 상태인지 확인합니다.
        /// </summary>
        /// <param name="unit">확인할 유닛</param>
        /// <returns>전투 불능 상태 여부</returns>
        bool IsUnitDefeated(ArmoredFrame unit);
        
        /// <summary>
        /// 전투 참가자 목록 가져오기
        /// </summary>
        /// <returns>전투 참가자 목록</returns>
        List<ArmoredFrame> GetParticipants();
        
        /// <summary>
        /// 아군 목록 가져오기
        /// </summary>
        /// <param name="forUnit">기준 유닛</param>
        /// <returns>아군 목록</returns>
        List<ArmoredFrame> GetAllies(ArmoredFrame forUnit);
        
        /// <summary>
        /// 적군 목록 가져오기
        /// </summary>
        /// <param name="forUnit">기준 유닛</param>
        /// <returns>적군 목록</returns>
        List<ArmoredFrame> GetEnemies(ArmoredFrame forUnit);
    }
} 