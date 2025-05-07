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
            _thresholdValue = Mathf.Max(0.0001f, thresholdValue); 
        }

        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            var actualLogger = context?.Logger?.TextLogger; 
            Weapon weaponToReloadCandidate = null;
            bool foundOutOfAmmoWeapon = false;

            // 모든 장착된 무기를 순회하며 재장전 대상 탐색
            foreach (var weapon in agent.EquippedWeapons)
            {
                if (weapon == null || !weapon.IsOperational || weapon.MaxAmmo <= 0 || weapon.IsReloading)
                {
                    continue; // 유효하지 않거나, 무한 탄창이거나, 이미 재장전 중인 무기는 건너뜀
                }

                // 1. 탄약이 완전히 없는 무기 (OutOfAmmo)
                if (weapon.CurrentAmmo <= 0)
                {
                    weaponToReloadCandidate = weapon; // 가장 시급한 케이스
                    foundOutOfAmmoWeapon = true;
                    break; // OutOfAmmo 무기를 찾으면 더 이상 탐색할 필요 없음
                }

                // 2. 낮은 탄약 상태의 무기 (LowAmmo) - OutOfAmmo가 아닌 경우에만 고려
                if (_condition == ReloadCondition.LowAmmo && !foundOutOfAmmoWeapon)
                {
                    int lowAmmoLimit = 1;
                    if (_thresholdValue > 0f && _thresholdValue < 1f) // 비율 기준
                    {
                        lowAmmoLimit = Mathf.CeilToInt(weapon.MaxAmmo * _thresholdValue);
                        lowAmmoLimit = Mathf.Max(1, lowAmmoLimit);
                    }
                    else // 절대값 기준
                    {
                        lowAmmoLimit = (int)_thresholdValue;
                    }

                    if (weapon.CurrentAmmo > 0 && weapon.CurrentAmmo <= lowAmmoLimit)
                    {
                        // 아직 후보가 없거나, 현재 후보보다 더 적합하면 (예: 더 낮은 탄약 비율) 교체 가능
                        // 여기서는 일단 처음 발견된 LowAmmo 무기를 후보로 지정
                        if (weaponToReloadCandidate == null) 
                        {
                            weaponToReloadCandidate = weapon;
                        }
                    }
                }
            }

            // 최종 결정된 재장전 대상이 있는지 확인
            if (weaponToReloadCandidate != null)
            {
                // OutOfAmmo 조건인데 찾은 게 LowAmmo 상태면 안됨 (위 로직에서 이미 처리됨)
                // LowAmmo 조건일 때는 OutOfAmmo를 우선했으므로 괜찮음.
                bool conditionMet = false;
                if (foundOutOfAmmoWeapon) // 탄약 없는 무기를 찾았다면 어떤 조건이든 OK
                { 
                    conditionMet = true;
                }
                else if (_condition == ReloadCondition.LowAmmo && weaponToReloadCandidate != null) // 낮은 탄약 조건이고, 후보가 있을 때 (OutOfAmmo는 아니었음)
                { 
                    conditionMet = true; // 위에서 LowAmmo 조건 만족하는 후보를 찾았음
                }
                else if (_condition == ReloadCondition.OutOfAmmo && !foundOutOfAmmoWeapon)
                { 
                     // OutOfAmmo 조건인데, 탄약 없는 무기를 못 찾음 (모두 탄약이 있거나 LowAmmo 상태임)
                    conditionMet = false;
                }


                if (conditionMet)
                {
                    actualLogger?.Log($"[{GetType().Name}({_condition})] {agent.Name}: Needs reload for '{weaponToReloadCandidate.Name}' (Ammo: {weaponToReloadCandidate.CurrentAmmo}/{weaponToReloadCandidate.MaxAmmo}). Success.", LogLevel.Debug);
                    blackboard.WeaponToReload = weaponToReloadCandidate;
                    blackboard.SelectedWeapon = null; // 재장전 시에는 선택된 공격 무기 없음
                    return NodeStatus.Success;
                }
            }
            
            // 여기까지 왔다면 재장전할 무기를 찾지 못했거나 조건에 맞지 않음
            actualLogger?.Log($"[{GetType().Name}({_condition})] {agent.Name}: No weapon needs reload under current condition. Failure.", LogLevel.Debug);
            blackboard.WeaponToReload = null; // 재장전할 무기 없음 확실히 명시
            return NodeStatus.Failure;
        }
    }
} 