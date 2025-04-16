using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// ArmoredFrame의 기본 골격을 담당하는 Frame 클래스입니다.
    /// </summary>
    [Serializable]
    public class Frame
    {
        /// <summary>
        /// 프레임의 이름
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// 프레임의 타입 (경량, 범용, 중장갑)
        /// </summary>
        [SerializeField] private FrameType _type;

        /// <summary>
        /// 프레임의 기본 스탯
        /// </summary>
        [SerializeField] private Stats _baseStats;

        /// <summary>
        /// 프레임과 파츠의 호환성 정보를 담는 딕셔너리
        /// key: 파츠 타입, value: 호환성 계수 (1.0이 정상, 1.0 미만이면 성능 저하)
        /// </summary>
        private Dictionary<PartType, float> _partCompatibility;

        // 공개 프로퍼티
        public string Name => _name;
        public FrameType Type => _type;
        public Stats BaseStats => _baseStats;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Frame()
        {
            _name = "Default Frame";
            _type = FrameType.Standard;
            _baseStats = new Stats();
            _partCompatibility = new Dictionary<PartType, float>();
            InitializeDefaultCompatibility();
        }

        /// <summary>
        /// 이름과 타입을 지정하는 생성자
        /// </summary>
        public Frame(string name, FrameType type, Stats baseStats)
        {
            _name = name;
            _type = type;
            _baseStats = baseStats;
            _partCompatibility = new Dictionary<PartType, float>();
            InitializeDefaultCompatibility();
            AdjustCompatibilityByType();
        }

        /// <summary>
        /// 기본 호환성 초기화 - 모든 파츠 타입에 대해 1.0으로 설정
        /// </summary>
        private void InitializeDefaultCompatibility()
        {
            foreach (PartType partType in Enum.GetValues(typeof(PartType)))
            {
                if (partType != PartType.Frame) // 프레임 자신은 제외
                {
                    _partCompatibility[partType] = 1.0f;
                }
            }
        }

        /// <summary>
        /// 프레임 타입에 따라 호환성 조정
        /// </summary>
        private void AdjustCompatibilityByType()
        {
            switch (_type)
            {
                case FrameType.Light:
                    // 경량 프레임은 무거운 파츠와 호환성이 낮음
                    _partCompatibility[PartType.Body] = 0.8f;
                    _partCompatibility[PartType.Arm] = 0.9f;
                    break;
                case FrameType.Heavy:
                    // 중장갑 프레임은 경량 파츠와 호환성이 낮음
                    _partCompatibility[PartType.Legs] = 0.8f;
                    _partCompatibility[PartType.Head] = 0.9f;
                    break;
                case FrameType.Standard:
                    // 범용 프레임은 모든 파츠와 호환성이 좋음
                    break;
            }
        }

        /// <summary>
        /// 특정 파츠 타입에 대한 호환성 계수를 반환합니다.
        /// </summary>
        public float GetCompatibilityFactor(PartType partType)
        {
            if (_partCompatibility.TryGetValue(partType, out float factor))
            {
                return factor;
            }
            return 1.0f; // 기본값
        }

        /// <summary>
        /// 특정 파츠 타입에 대한 호환성 계수를 설정합니다.
        /// </summary>
        public void SetCompatibilityFactor(PartType partType, float factor)
        {
            if (partType != PartType.Frame) // 프레임 자신은 제외
            {
                _partCompatibility[partType] = Mathf.Clamp(factor, 0.1f, 1.5f);
            }
        }
    }
} 