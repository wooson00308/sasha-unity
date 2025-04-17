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

        /// <summary>
        /// ArmoredFrame의 현재 위치
        /// </summary>
        [SerializeField] private Vector3 _position;

        /// <summary>
        /// 이 ArmoredFrame을 조종하는 파일럿
        /// </summary>
        private Pilot _pilot;

        /// <summary>
        /// 장착된 무기 목록
        /// </summary>
        private List<Weapon> _equippedWeapons;

        // 공개 프로퍼티
        public string Name => _name;
        public Frame FrameBase => _frameBase;
        public Stats CombinedStats => _combinedStats;
        public bool IsOperational => _isOperational;
        public Vector3 Position { get => _position; set => _position = value; }
        public Pilot Pilot => _pilot;
        public IReadOnlyList<Weapon> EquippedWeapons => _equippedWeapons; // Read-only access

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public ArmoredFrame()
        {
            _name = "Default ArmoredFrame";
            _frameBase = new Frame();
            _parts = new Dictionary<PartType, Part>();
            _equippedWeapons = new List<Weapon>(); // Initialize weapon list
            _combinedStats = _frameBase.BaseStats;
            _isOperational = true;
            _position = Vector3.zero; // Initialize position
            _pilot = null; // Initialize pilot
        }

        /// <summary>
        /// 이름과 프레임을 지정하는 생성자
        /// </summary>
        public ArmoredFrame(string name, Frame frameBase)
        {
            _name = name;
            _frameBase = frameBase;
            _parts = new Dictionary<PartType, Part>();
            _equippedWeapons = new List<Weapon>(); // Initialize weapon list
            _combinedStats = _frameBase.BaseStats;
            _isOperational = true;
            _position = Vector3.zero; // Initialize position
            _pilot = null; // Initialize pilot
        }

        /// <summary>
        /// 이름, 프레임, 위치를 지정하는 생성자 (추가)
        /// </summary>
        public ArmoredFrame(string name, Frame frameBase, Vector3 initialPosition)
        {
            _name = name;
            _frameBase = frameBase;
            _parts = new Dictionary<PartType, Part>();
            _equippedWeapons = new List<Weapon>(); // Initialize weapon list
            _combinedStats = _frameBase.BaseStats;
            _isOperational = true;
            _position = initialPosition; // Initialize with provided position
            _pilot = null; // Initialize pilot
        }

        /// <summary>
        /// 파일럿을 ArmoredFrame에 할당합니다.
        /// </summary>
        public void AssignPilot(Pilot pilot)
        {
            _pilot = pilot;
            // TODO: 파일럿 스탯을 CombinedStats에 반영하는 로직 필요 (RecalculateStats 수정)
        }

        /// <summary>
        /// 무기를 장착합니다. (단순히 리스트에 추가)
        /// </summary>
        public void AttachWeapon(Weapon weapon)
        {
            if (weapon != null)
            {
                _equippedWeapons.Add(weapon);
                // TODO: 무기 스탯이나 특수 능력을 고려한 스탯 재계산 로직 필요 시 추가
            }
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
        /// 장착된 모든 작동 가능한 무기 목록을 반환합니다.
        /// </summary>
        public List<Weapon> GetEquippedWeapons()
        {
            // 작동 가능한 무기만 필터링하여 반환
            return _equippedWeapons.FindAll(w => w.IsOperational);
        }
    }
} 