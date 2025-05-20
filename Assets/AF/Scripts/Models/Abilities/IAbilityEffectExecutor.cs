using AF.Combat;
using AF.Models;

namespace AF.Models.Abilities
{
    /// <summary>
    /// 런타임 어빌리티 효과를 실행하기 위한 공통 인터페이스.
    /// </summary>
    public interface IAbilityEffectExecutor
    {
        /// <summary>
        /// 어빌리티 효과를 실행합니다.
        /// </summary>
        /// <param name="ctx">현재 전투 컨텍스트</param>
        /// <param name="user">어빌리티를 발동한 기체</param>
        /// <param name="target">대상 기체 (없을 수 있음)</param>
        /// <param name="abilityData">런타임 어빌리티 정보</param>
        /// <returns>성공 여부</returns>
        bool Execute(CombatContext ctx, ArmoredFrame user, ArmoredFrame target, AbilityEffect abilityData);
    }
} 