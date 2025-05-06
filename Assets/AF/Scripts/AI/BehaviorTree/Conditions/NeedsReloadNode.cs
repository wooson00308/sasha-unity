using AF.Combat;
using AF.Models;
using UnityEngine;
using AF.Services; // TextLoggerService 사용

namespace AF.AI.BehaviorTree
{
    // 재장전 조건 정의
    public enum ReloadCondition
    {
        OutOfAmmo, // 탄약 없음 (CurrentAmmo <= 0)
        LowAmmo    // 낮은 탄약 (0 < CurrentAmmo <= Threshold)
    }

    /// <summary>
    /// 유닛의 무기가 특정 재장전 조건(탄약 없음 또는 낮은 탄약)을 만족하는지 검사하는 조건 노드입니다.
    /// </summary>
    public class NeedsReloadNode : ConditionNode
    {
        private readonly ReloadCondition _condition;
        private readonly float _thresholdValue; // int -> float, 파라미터 이름 변경 (절대값 또는 비율)

        /// <summary>
        /// NeedsReloadNode를 생성합니다.
        /// </summary>
        /// <param name="condition">검사할 재장전 조건</param>
        /// <param name="thresholdValue">LowAmmo 조건일 때 사용할 기준값. 0.0 초과 1.0 미만이면 비율, 1.0 이상이면 절대값 (기본값: 0.1f = 10%)</param>
        public NeedsReloadNode(ReloadCondition condition, float thresholdValue = 0.1f)
        {
            _condition = condition;
            // 기준값 유효성 검사 (0보다 커야 함)
            _thresholdValue = Mathf.Max(0.0001f, thresholdValue); // 0 또는 음수 방지
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // 검사할 무기 결정: Blackboard에 SelectedWeapon이 있으면 그것을 사용, 없으면 agent의 주무기 사용
            Weapon weaponToCheck = blackboard.SelectedWeapon ?? agent.GetPrimaryWeapon();
            var logger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;

            // 무기가 없거나, 작동 불능이거나, 무한 탄창이거나, 이미 재장전 중이면 재장전 불필요/불가
            if (weaponToCheck == null || !weaponToCheck.IsOperational || weaponToCheck.MaxAmmo <= 0 || weaponToCheck.IsReloading)
            {
                string reason = weaponToCheck == null ? "No weapon" : 
                                !weaponToCheck.IsOperational ? "Not operational" : 
                                weaponToCheck.MaxAmmo <= 0 ? "Infinite ammo" : "Already reloading";
                // 무한 탄창이거나 재장전 중이면 LowAmmo/OutOfAmmo 검사 자체가 무의미하므로 로그 레벨 변경 가능 (예: Verbose)
                logger?.Log($"[{GetType().Name}({_condition})] {agent.Name}: Weapon='{weaponToCheck?.Name ?? "N/A"}' cannot reload ({reason}). Failure.", LogLevel.Debug);
                return NodeStatus.Failure;
            }

            bool checkResult = false;
            int actualThreshold = 1; // 기본값 (비율 계산 실패 대비)
            string conditionDesc = "Unknown";

            switch (_condition)
            {
                case ReloadCondition.OutOfAmmo:
                    checkResult = weaponToCheck.CurrentAmmo <= 0;
                    conditionDesc = "OutOfAmmo";
                    break;
                case ReloadCondition.LowAmmo:
                    if (_thresholdValue > 0f && _thresholdValue < 1f) // 비율 기준
                    {
                        // MaxAmmo가 0보다 클 때만 비율 계산 의미 있음
                        if (weaponToCheck.MaxAmmo > 0)
                        {   
                            // 올림 계산으로 최소 1발 이상 기준값 보장 (예: 10발 탄창 10%면 1발, 15발 탄창 10%면 2발)
                            actualThreshold = Mathf.CeilToInt(weaponToCheck.MaxAmmo * _thresholdValue);
                             // 비율 계산 결과가 0이 되는 극단적인 경우 방지 (예: MaxAmmo=5, Threshold=0.1 -> 0.5 -> CeilToInt=1)
                            actualThreshold = Mathf.Max(1, actualThreshold); 
                            conditionDesc = $"LowAmmo (<= {_thresholdValue * 100:F0}% ≈ {actualThreshold} rounds)";
                        }
                        else 
                        { 
                            // MaxAmmo가 0인데 비율 기준 LowAmmo 체크는 모순. 실패 처리.
                             logger?.Log($"[{GetType().Name}(LowAmmo)] {agent.Name}: Weapon='{weaponToCheck.Name}' has MaxAmmo=0, cannot use percentage threshold. Failure.", LogLevel.Debug);
                             return NodeStatus.Failure; 
                        }
                    }
                    else // 절대값 기준 (1.0 이상)
                    {
                        actualThreshold = (int)_thresholdValue;
                        conditionDesc = $"LowAmmo (<= {actualThreshold} rounds)";
                    }
                    checkResult = weaponToCheck.CurrentAmmo > 0 && weaponToCheck.CurrentAmmo <= actualThreshold; 
                    break;
            }
            
            logger?.Log(
                $"[{GetType().Name}({conditionDesc})] {agent.Name}: Weapon='{weaponToCheck.Name}', Ammo={weaponToCheck.CurrentAmmo}/{weaponToCheck.MaxAmmo}. CheckResult={checkResult}. " +
                $"Result: {(checkResult ? NodeStatus.Success : NodeStatus.Failure)}",
                LogLevel.Debug
            );

            return checkResult ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
} 