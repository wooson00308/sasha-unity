using System;
using System.Collections.Generic;
using System.Linq;
using AF.EventBus;
using AF.Models;
using AF.Services;
using AF.Combat.Behaviors;  // 파일럿 전략
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// 전투 시뮬레이션 서비스 (인터페이스 구현 + 오케스트레이션 전담)
    /// </summary>
    public sealed class CombatSimulatorService : ICombatSimulatorService
    {
        // ──────────────────────────────────────────────────────────────
        // 외부 서브-서비스 (DI 대신 직접 new)
        // ──────────────────────────────────────────────────────────────
        private ICombatActionExecutor  _actionExecutor;
        private IStatusEffectProcessor _statusProcessor;
        private IBattleResultEvaluator _resultEvaluator;

        // ──────────────────────────────────────────────────────────────
        // Unity/서비스 참조
        // ──────────────────────────────────────────────────────────────
        private EventBus.EventBus _eventBus;
        private TextLoggerService _textLogger;

        // ──────────────────────────────────────────────────────────────
        // 전투 상태
        // ──────────────────────────────────────────────────────────────
        private string _currentBattleId;
        private string _battleName;
        private bool   _isInCombat;
        private int    _currentTurn;
        private float  _combatStartTime;

        private List<ArmoredFrame>                    _participants;
        private Dictionary<ArmoredFrame,int>          _teamAssignments;
        private ArmoredFrame                          _currentActiveUnit;
        private HashSet<ArmoredFrame>                 _defendedThisTurn;

        // 파일럿 전문화 → 전략
        private Dictionary<SpecializationType, IPilotBehaviorStrategy> _behaviorStrategies;

        // ──────────────────────────────────────────────────────────────
        // 인터페이스 프로퍼티
        // ──────────────────────────────────────────────────────────────
        public string       CurrentBattleId   => _currentBattleId;
        public bool         IsInCombat        => _isInCombat;
        public int          CurrentTurn       => _currentTurn;
        public ArmoredFrame CurrentActiveUnit => _currentActiveUnit;

        // ──────────────────────────────────────────────────────────────
        // IService 기본
        // ──────────────────────────────────────────────────────────────
        public void Initialize()
        {
            // 서비스 참조
            _eventBus   = ServiceLocator.Instance.GetService<EventBusService>().Bus;
            _textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();

            // 서브-서비스
            _actionExecutor  = new CombatActionExecutor();
            _statusProcessor = new StatusEffectProcessor();
            _resultEvaluator = new BattleResultEvaluator();

            // 전략 매핑
            _behaviorStrategies = new Dictionary<SpecializationType, IPilotBehaviorStrategy>
            {
                { SpecializationType.MeleeCombat,    new MeleeCombatBehaviorStrategy()    },
                { SpecializationType.RangedCombat,   new RangedCombatBehaviorStrategy()   },
                { SpecializationType.Defense,        new DefenseCombatBehaviorStrategy()  },
                { SpecializationType.Support,        new SupportCombatBehaviorStrategy()  },
                { SpecializationType.StandardCombat, new StandardCombatBehaviorStrategy() }
            };

            // 상태 초기화
            _participants      = new List<ArmoredFrame>();
            _teamAssignments   = new Dictionary<ArmoredFrame,int>();
            _defendedThisTurn  = new HashSet<ArmoredFrame>();
            _isInCombat        = false;
            _currentTurn       = 0;
            _currentBattleId   = null;
        }

        public void Shutdown()
        {
            if (_isInCombat) EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Aborted);

            _eventBus         = null;
            _textLogger       = null;
            _participants     = null;
            _teamAssignments  = null;
            _defendedThisTurn = null;
        }

        // ──────────────────────────────────────────────────────────────
        // 전투 시작/종료
        // ──────────────────────────────────────────────────────────────
        public string StartCombat(ArmoredFrame[] participants, string battleName, bool autoProcess = false)
        {
            if (_isInCombat) EndCombat();

            _isInCombat       = true;
            _currentTurn      = 0;
            _battleName       = battleName;
            _currentBattleId  = $"BATTLE_{DateTime.Now:yyyyMMdd_HHmmss}_{UnityEngine.Random.Range(1000,9999)}";
            _combatStartTime  = Time.time;

            _participants     = participants.ToList();
            AssignTeams(participants);
            _defendedThisTurn.Clear();

            _eventBus.Publish(new CombatSessionEvents.CombatStartEvent(
                participants, _currentBattleId, battleName, Vector3.zero));

            if (autoProcess) ProcessNextTurn();
            return _currentBattleId;
        }

        public void EndCombat(CombatSessionEvents.CombatEndEvent.ResultType? forceResult = null)
        {
            if (!_isInCombat) return;

            var result = forceResult ?? _resultEvaluator.Evaluate(_participants, _teamAssignments);
            float dur  = Time.time - _combatStartTime;
            var survivors = _participants.Where(p => p.IsOperational).ToArray();

            _eventBus.Publish(new CombatSessionEvents.CombatEndEvent(
                survivors, result, _currentBattleId, dur));

            // 상태 초기화
            _isInCombat        = false;
            _currentActiveUnit = null;
            _participants.Clear();
            _teamAssignments.Clear();
            _defendedThisTurn.Clear();

            _currentBattleId = null;
            _battleName      = null;
            _currentTurn     = 0;
        }

        // ──────────────────────────────────────────────────────────────
        // 턴 진행
        // ──────────────────────────────────────────────────────────────
        public bool ProcessNextTurn()
        {
            if (!_isInCombat) return false;

            _defendedThisTurn.Clear();

            if (_currentTurn > 0 && _currentActiveUnit != null)
                _eventBus.Publish(new CombatSessionEvents.TurnEndEvent(
                    _currentTurn, _currentActiveUnit, _currentBattleId));

            _currentTurn++;
            _currentActiveUnit = GetNextActiveUnit();
            if (_currentActiveUnit == null) { EndCombat(); return false; }

            _eventBus.Publish(new CombatSessionEvents.TurnStartEvent(
                _currentTurn, _currentActiveUnit, _currentBattleId));

            // 재장전 완료 체크
            foreach (var unit in _participants.Where(u=>u.IsOperational))
            {
                foreach (var w in unit.GetAllWeapons())
                {
                    if (w.IsReloading && w.CheckReloadCompletion(_currentTurn)) w.FinishReload();
                }
            }

            // AP 회복 + 상태효과
            _currentActiveUnit.RecoverAPOnTurnStart();
            _statusProcessor.Tick(MakeCtx(), _currentActiveUnit);

            // AI 루프
            int actions=0, maxActions=30;
            while (_currentActiveUnit.IsOperational && actions<maxActions)
            {
                var (act,tp,pos,wep) = DetermineActionForUnit(_currentActiveUnit);
                if (act==default) break;

                bool ok = _actionExecutor.Execute(
                    MakeCtx(), _currentActiveUnit, act, tp, pos, wep);

                actions++;
                if (act==CombatActionEvents.ActionType.Reload && ok) break;
                if (!ok || !_currentActiveUnit.IsOperational) break;
            }

            CheckBattleEndCondition();
            return true;
        }

        // ──────────────────────────────────────────────────────────────
        // 인터페이스 요구: PerformAction / PerformAttack  (래퍼)
        // ──────────────────────────────────────────────────────────────
        public bool PerformAction(ArmoredFrame actor,
                                  CombatActionEvents.ActionType actionType,
                                  ArmoredFrame targetFrame,
                                  Vector3? targetPosition,
                                  Weapon weapon)
        {
            // 시뮬레이터 외부 호출 시 최소 검증
            if (!_isInCombat || actor != _currentActiveUnit) return false;
            return _actionExecutor.Execute(
                MakeCtx(), actor, actionType, targetFrame, targetPosition, weapon);
        }

        public bool PerformAttack(ArmoredFrame attacker, ArmoredFrame target, Weapon weapon)
        {
            if (!_isInCombat || attacker != _currentActiveUnit) return false;
            // ActionType.Attack 래퍼 호출
            return _actionExecutor.Execute(
                MakeCtx(), attacker, CombatActionEvents.ActionType.Attack,
                target, null, weapon);
        }

        // ──────────────────────────────────────────────────────────────
        // 참가자 / 팀 / 유틸
        // ──────────────────────────────────────────────────────────────
        public bool IsUnitDefeated(ArmoredFrame unit)
            => unit == null || !unit.IsOperational || !_participants.Contains(unit);

        public List<ArmoredFrame> GetParticipants() => new List<ArmoredFrame>(_participants);

        public List<ArmoredFrame> GetAllies(ArmoredFrame unit)
        {
            if (!_teamAssignments.TryGetValue(unit,out var team)) return new();
            return _participants.Where(p=>p!=unit && _teamAssignments.TryGetValue(p,out var t) && t==team).ToList();
        }

        public List<ArmoredFrame> GetEnemies(ArmoredFrame unit)
        {
            if (!_teamAssignments.TryGetValue(unit,out var team)) return new();
            return _participants.Where(p=>_teamAssignments.TryGetValue(p,out var t)&&t!=team).ToList();
        }

        public bool HasUnitDefendedThisTurn(ArmoredFrame unit)
            => _defendedThisTurn.Contains(unit);

        // ──────────────────────────────────────────────────────────────
        // 내부 헬퍼
        // ──────────────────────────────────────────────────────────────
        private CombatContext MakeCtx() => new CombatContext(
            _eventBus, _textLogger, _currentBattleId, _currentTurn,
            _defendedThisTurn, _participants, _teamAssignments);

        private void AssignTeams(IEnumerable<ArmoredFrame> parts)
        {
            _teamAssignments.Clear();
            foreach (var p in parts) _teamAssignments[p] = p.TeamId;
        }

        private ArmoredFrame GetNextActiveUnit()
        {
            int idx = _currentActiveUnit!=null ? _participants.IndexOf(_currentActiveUnit) : -1;
            for (int i=1;i<=_participants.Count;i++)
            {
                var u = _participants[(idx+i)%_participants.Count];
                if (u.IsOperational) return u;
            }
            return null;
        }

        private (CombatActionEvents.ActionType,ArmoredFrame,Vector3?,Weapon)
            DetermineActionForUnit(ArmoredFrame unit)
        {
            var spec = unit.Pilot?.Specialization ?? SpecializationType.StandardCombat;
            if (!_behaviorStrategies.TryGetValue(spec,out var strat))
                strat = _behaviorStrategies[SpecializationType.StandardCombat];
            return strat.DetermineAction(unit, this);
        }

        private void CheckBattleEndCondition()
        {
            var res = _resultEvaluator.Evaluate(_participants,_teamAssignments);
            if (res!=CombatSessionEvents.CombatEndEvent.ResultType.Draw) EndCombat(res);
        }
    }
}