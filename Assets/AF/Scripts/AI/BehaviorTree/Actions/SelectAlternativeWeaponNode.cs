using System.Collections.Generic;
using System.Linq;
using AF.Combat;
using AF.Models;
using UnityEngine; // Debug.Log 용
using AF.Services;

namespace AF.AI.BehaviorTree.Actions
{
    /// <summary>
    /// 블랙보드의 SelectedWeapon이 사용 불가능할 경우, 다른 사용 가능한 무기를 찾아 선택하는 액션 노드입니다.
    /// </summary>
    public class SelectAlternativeWeaponNode : ActionNode
    {
        public override NodeStatus Tick(ArmoredFrame agent, Blackboard blackboard, CombatContext context)
        {
            // 현재 블랙보드에 선택된 무기 가져오기
            Weapon currentSelectedWeapon = blackboard.SelectedWeapon;
            // 현재 선택된 무기가 없거나 (SelectTargetNode 실패 등) 사용 불가능 상태인지 확인
            // UseWeaponNode 같은 곳에서 AP 부족 등으로 실패한 후 이 노드로 넘어왔을 수도 있으므로,
            // 여기서 다시 한번 현재 선택된 무기가 유효한지, 사용 가능한지 체크하는 것이 안전합니다.
            // 특히 장전 중인지 여부를 다시 확인합니다.
            bool isCurrentWeaponUsable = (currentSelectedWeapon != null && 
                                            currentSelectedWeapon.IsOperational && 
                                            currentSelectedWeapon.HasAmmo() && 
                                           !currentSelectedWeapon.IsReloading);
            
            // 현재 무기가 유효하고 사용 가능하다면, 다른 무기를 찾을 필요 없이 성공 반환
            if (isCurrentWeaponUsable)
            {
                 var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                 textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Current SelectedWeapon ({currentSelectedWeapon.Name}) is usable. Success.", LogLevel.Debug);
                return NodeStatus.Success;
            }

            // 현재 무기가 사용 불가능하다면, 다른 사용 가능한 무기를 찾아봅니다.
            var usableAlternativeWeapons = agent.EquippedWeapons
                .Where(w => w != null && 
                            w.IsOperational && 
                            w.HasAmmo() && 
                           !w.IsReloading &&
                            w != currentSelectedWeapon) // 현재 무기 제외
                .ToList();

            if (usableAlternativeWeapons.Any())
            {
                // 사용할 다른 무기가 있다면, 가장 적합한 무기 선택 (SelectTargetNode와 유사한 기준 사용)
                Weapon alternativeWeaponToUse = usableAlternativeWeapons
                    .OrderByDescending(w => w == agent.GetPrimaryWeapon()) // 주무기 우선
                    .ThenByDescending(w => w.Damage) // 같은 우선순위면 공격력 높은 무기
                    .FirstOrDefault();

                if (alternativeWeaponToUse != null)
                {
                    blackboard.SelectedWeapon = alternativeWeaponToUse;
                    var textLogger = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
                    textLogger?.Log($"[{this.GetType().Name}] {agent.Name}: Current weapon ({currentSelectedWeapon?.Name ?? "None"}) is unusable. Selected alternative weapon: {alternativeWeaponToUse.Name}. Success.", LogLevel.Debug);
                    return NodeStatus.Success;
                }
            }
            
            // 사용할 다른 무기가 없다면 실패
            var textLoggerWarning = ServiceLocator.Instance?.GetService<TextLoggerService>()?.TextLogger;
            textLoggerWarning?.Log($"[{this.GetType().Name}] {agent.Name}: No usable alternative weapon found. Current SelectedWeapon ({currentSelectedWeapon?.Name ?? "None"}). Failure.", LogLevel.Debug);
            blackboard.SelectedWeapon = null; // 명확히 null 설정
            return NodeStatus.Failure;
        }
    }
} 