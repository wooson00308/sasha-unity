using System.Collections.Generic;
using AF.Combat;

namespace AF.Models.Abilities
{
    /// <summary>
    /// AbilityID ↔ 실행 로직 매핑을 관리합니다.
    /// </summary>
    public static class AbilityEffectRegistry
    {
        private static readonly Dictionary<string, IAbilityEffectExecutor> _executors =
            new Dictionary<string, IAbilityEffectExecutor>
            {
                { ZoomAbilityExecutor.ABILITY_ID,     new ZoomAbilityExecutor()     },
                { AnalyzeAbilityExecutor.ABILITY_ID,  new AnalyzeAbilityExecutor()  },
                { SelfRepairAbilityExecutor.ABILITY_ID, new SelfRepairAbilityExecutor() },
                { EvasiveAbilityExecutor.ABILITY_ID, new EvasiveAbilityExecutor() },
                { HoverAbilityExecutor.ABILITY_ID, new HoverAbilityExecutor() },
                { APBoostAbilityExecutor.ABILITY_ID, new APBoostAbilityExecutor() },
                { EnergyShieldAbilityExecutor.ABILITY_ID, new EnergyShieldAbilityExecutor() },
                { RepairUnitAbilityExecutor.ABILITY_ID, new RepairUnitAbilityExecutor() },
                { RepairKitAbilityExecutor.ABILITY_ID, new RepairKitAbilityExecutor() },
                // TODO: 다른 어빌리티 추가
            };

        /// <summary>
        /// 주어진 AbilityID에 대한 실행기를 반환합니다.
        /// </summary>
        public static bool TryGetExecutor(string abilityId, out IAbilityEffectExecutor executor)
        {
            return _executors.TryGetValue(abilityId, out executor);
        }
    }
} 