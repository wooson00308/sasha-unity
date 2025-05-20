using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AF.Data;

namespace AF.Models.Abilities
{
    /// <summary>
    /// 런타임에서 AbilityID → AbilitySO 매핑을 빠르게 조회하기 위한 간단한 DB.
    /// Resources 폴더에 있는 AbilitySO를 모두 로드해 캐싱한다.
    /// </summary>
    public static class AbilityDatabase
    {
        private static readonly Dictionary<string, AbilitySO> _abilityMap;

        static AbilityDatabase()
        {
            // Resources 경로 전체에서 AbilitySO 전부 검색
            var all = Resources.LoadAll<AbilitySO>("Abilities");
            _abilityMap = all.ToDictionary(a => a.AbilityID, a => a);
        }

        public static bool TryGetAbility(string abilityId, out AbilitySO so)
        {
            return _abilityMap.TryGetValue(abilityId, out so);
        }
    }
} 