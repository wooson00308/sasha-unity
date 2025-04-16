using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 게임의 주요 유닛인 ArmoredFrame을 구현한 클래스입니다.
    /// 다양한 파츠를 조합하여 하나의 완전한 메카닉 유닛을 구성합니다.
    /// </summary>
    [Serializable]
    public class ArmoredFrame
    {
        /// <summary>
        /// ArmoredFrame의 이름
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// 기본 프레임
        /// </summary>
        [SerializeField] private Frame _frameBase;

        /// <summary>
        /// 파츠 참조 (현재는 Part 클래스의 하위 클래스들이 구현되지 않았으므로 Part로 선언)
        /// </summary>
        private Dictionary<PartType, Part> _parts;

        /// <summary>
        /// 통합 스탯 (모든 파츠의 스탯 합산)
        /// </summary>
        private Stats _combinedStats;

        /// <summary>
        /// ArmoredFrame의 현재 상태
        /// </summary>
        private bool _isOperational;

        // 공개 프로퍼티
        public string Name => _name;
        public Frame FrameBase => _frameBase;
        public Stats CombinedStats => _combinedStats;
        public bool IsOperational => _isOperational;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ArmoredFrame()
        {
            _name = "Default ArmoredFrame";
            _frameBase = new Frame();
            _parts = new Dictionary<PartType, Part>();
            _combinedStats = _frameBase.BaseStats;
            _isOperational = true;
        }

        /// <summary>
        /// 이름과 프레임을 지정하는 생성자
        /// </summary>
        public ArmoredFrame(string name, Frame frameBase)
        {
            _name = name;
            _frameBase = frameBase;
            _parts = new Dictionary<PartType, Part>();
            _combinedStats = _frameBase.BaseStats;
            _isOperational = true;
        }

        /// <summary>
        /// 파츠를 장착합니다.
        /// </summary>
        public void AttachPart(Part part)
        {
            if (part != null)
            {
                _parts[part.Type] = part;
                RecalculateStats();
            }
        }

        /// <summary>
        /// 파츠를 제거합니다.
        /// </summary>
        public Part DetachPart(PartType partType)
        {
            if (_parts.TryGetValue(partType, out Part part))
            {
                _parts.Remove(partType);
                RecalculateStats();
                return part;
            }
            return null;
        }

        /// <summary>
        /// 특정 타입의 파츠를 반환합니다.
        /// </summary>
        public Part GetPart(PartType partType)
        {
            if (_parts.TryGetValue(partType, out Part part))
            {
                return part;
            }
            return null;
        }

        /// <summary>
        /// 모든 파츠를 고려하여 통합 스탯을 다시 계산합니다.
        /// </summary>
        private void RecalculateStats()
        {
            // 기본 프레임 스탯으로 시작
            _combinedStats = _frameBase.BaseStats;

            // 모든 파츠의 스탯 추가
            foreach (var part in _parts.Values)
            {
                if (part.IsOperational)
                {
                    float compatibilityFactor = _frameBase.GetCompatibilityFactor(part.Type);
                    // 호환성 계수를 적용한 파츠 스탯 추가
                    _combinedStats += part.PartStats * compatibilityFactor;
                }
            }

            // 작동 상태 확인
            CheckOperationalStatus();
        }

        /// <summary>
        /// ArmoredFrame이 작동 가능한 상태인지 확인합니다.
        /// Frame 또는 Body가 파괴되면 ArmoredFrame은 작동 불능 상태가 됩니다.
        /// </summary>
        private void CheckOperationalStatus()
        {
            // 현재는 간단하게 구현. 나중에 Body 클래스가 구현되면 더 자세히 구현 예정
            _isOperational = true;
            
            // Body 파츠 확인
            if (_parts.TryGetValue(PartType.Body, out Part bodyPart))
            {
                if (!bodyPart.IsOperational)
                {
                    _isOperational = false;
                }
            }
        }

        /// <summary>
        /// 데미지를 특정 파츠에 적용합니다.
        /// </summary>
        public bool ApplyDamage(PartType targetPart, float damageAmount)
        {
            if (_parts.TryGetValue(targetPart, out Part part))
            {
                bool isDestroyed = part.ApplyDamage(damageAmount);
                if (isDestroyed)
                {
                    // 파츠가 파괴되면 OnDestroyed 효과 적용 및 스탯 재계산
                    part.OnDestroyed(this);
                    RecalculateStats();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 모든 작동 가능한 무기 목록을 반환합니다.
        /// </summary>
        public List<Weapon> GetAllWeapons()
        {
            // 현재는 간단하게 빈 리스트 반환, 나중에 무기 시스템이 구현되면 업데이트 예정
            return new List<Weapon>();
        }
    }
} 