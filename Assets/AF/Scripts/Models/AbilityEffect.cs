using System.Collections.Generic;
using AF.Data; // For AbilitySO

namespace AF.Models
{
    /// <summary>
    /// 런타임에 사용될 어빌리티의 구체적인 데이터와 효과 실행을 위한 정보를 담는 클래스입니다.
    /// 행동 트리 노드에서 생성되어 Blackboard를 통해 CombatActionExecutor로 전달될 수 있습니다.
    /// </summary>
    public class AbilityEffect // 이전 RuntimeAbilityData 역할
    {
        public string AbilityID { get; }
        public AbilityType Type { get; }
        public AbilityTargetType TargetType { get; }
        public AbilityEffectType EffectType { get; }
        public Dictionary<string, object> EffectParameters { get; } // 파싱된 효과 파라미터
        public float APCost { get; }
        public Part SourcePart { get; } // (선택적) 어떤 파츠의 어빌리티인지
        public AbilitySO SourceSO { get; } // (필수) 원본 SO 참조

        public AbilityEffect(
            AbilitySO sourceSO,
            Dictionary<string, object> effectParameters = null, 
            Part sourcePart = null) 
        {
            if (sourceSO == null)
            {
                throw new System.ArgumentNullException(nameof(sourceSO), "Source AbilitySO cannot be null.");
            }

            SourceSO = sourceSO;
            AbilityID = sourceSO.AbilityID;
            Type = sourceSO.AbilityType;
            TargetType = sourceSO.TargetType;
            EffectType = sourceSO.EffectType;
            APCost = sourceSO.APCost;
            
            // EffectParameters는 외부에서 파싱해서 주입하거나, 이 생성자 내에서 sourceSO.EffectParametersRaw를 파싱할 수 있음
            // 여기서는 일단 외부 주입으로 가정
            EffectParameters = effectParameters ?? new Dictionary<string, object>();
            SourcePart = sourcePart;
        }

        // +++ 추가 헬퍼 메서드 (예시) +++

        /// <summary>
        /// EffectParameters에서 특정 키의 값을 가져옵니다.
        /// </summary>
        public T GetParameter<T>(string key, T defaultValue = default)
        {
            if (EffectParameters.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public override string ToString()
        {
            return $"{AbilityName} ({AbilityID}) - Type: {Type}, Target: {TargetType}, Effect: {EffectType}, AP: {APCost}";
        }

        public string AbilityName => SourceSO?.AbilityName ?? "Unknown Ability";
    }
} 