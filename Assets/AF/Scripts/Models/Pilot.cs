using System;
using System.Collections.Generic;
using UnityEngine;

namespace AF.Models
{
    /// <summary>
    /// 파일럿 전문화 타입
    /// </summary>
    public enum SpecializationType
    {
        /// <summary>
        /// 공격 전문화: 데미지와 명중률 향상
        /// </summary>
        Combat,

        /// <summary>
        /// 방어 전문화: 내구도와 회피율 향상
        /// </summary>
        Defense,

        /// <summary>
        /// 지원 전문화: 특수 능력 효율 향상
        /// </summary>
        Support,

        /// <summary>
        /// 기계 전문화: 프레임-파츠 호환성 향상
        /// </summary>
        Engineering
    }

    /// <summary>
    /// ArmoredFrame을 조종하는 파일럿 클래스입니다.
    /// </summary>
    [Serializable]
    public class Pilot
    {
        /// <summary>
        /// 파일럿의 이름
        /// </summary>
        [SerializeField] private string _name;

        /// <summary>
        /// 파일럿의 기본 스탯
        /// </summary>
        [SerializeField] private Stats _baseStats;

        /// <summary>
        /// 파일럿의 레벨
        /// </summary>
        [SerializeField] private int _level;

        /// <summary>
        /// 현재 경험치
        /// </summary>
        [SerializeField] private int _experience;

        /// <summary>
        /// 레벨업에 필요한 경험치
        /// </summary>
        [SerializeField] private int _experienceToNextLevel;

        /// <summary>
        /// 파일럿의 전문화 타입
        /// </summary>
        [SerializeField] private SpecializationType _specialization;

        /// <summary>
        /// 파일럿의 스킬 목록
        /// </summary>
        private List<string> _skills;

        /// <summary>
        /// 파일럿의 스탯 보정치 (전문화에 따라 달라짐)
        /// </summary>
        private Stats _specializationBonus;

        // 공개 프로퍼티
        public string Name => _name;
        public Stats BaseStats => _baseStats;
        public int Level => _level;
        public int Experience => _experience;
        public int ExperienceToNextLevel => _experienceToNextLevel;
        public SpecializationType Specialization => _specialization;
        public IReadOnlyList<string> Skills => _skills;
        public Stats SpecializationBonus => _specializationBonus;

        /// <summary>
        /// 기본 생성자
        /// </summary>
        public Pilot()
        {
            _name = "Default Pilot";
            _baseStats = new Stats();
            _level = 1;
            _experience = 0;
            _experienceToNextLevel = 100;
            _specialization = SpecializationType.Combat;
            _skills = new List<string>();
            _specializationBonus = CalculateSpecializationBonus();
        }

        /// <summary>
        /// 상세 정보를 지정하는 생성자
        /// </summary>
        public Pilot(string name, Stats baseStats, SpecializationType specialization)
        {
            _name = name;
            _baseStats = baseStats;
            _level = 1;
            _experience = 0;
            _experienceToNextLevel = 100;
            _specialization = specialization;
            _skills = new List<string>();
            _specializationBonus = CalculateSpecializationBonus();
        }

        /// <summary>
        /// 경험치를 획득합니다. 레벨업 조건을 만족하면 레벨업을 수행합니다.
        /// </summary>
        /// <param name="amount">획득한 경험치</param>
        /// <returns>레벨업 여부</returns>
        public bool GainExperience(int amount)
        {
            _experience += amount;
            
            if (_experience >= _experienceToNextLevel)
            {
                LevelUp();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 레벨을 올립니다.
        /// </summary>
        private void LevelUp()
        {
            _level++;
            _experience -= _experienceToNextLevel;
            _experienceToNextLevel = CalculateNextLevelExperience();
            
            // 스탯 증가
            _baseStats = _baseStats * 1.1f; // 레벨업 시 모든 스탯 10% 증가
            
            // 전문화 보너스 재계산
            _specializationBonus = CalculateSpecializationBonus();
            
            // 레벨업 시 새 스킬 획득 로직 (간단히 구현)
            if (_level % 3 == 0) // 3레벨마다 새 스킬
            {
                AddRandomSkill();
            }
        }

        /// <summary>
        /// 다음 레벨업에 필요한 경험치를 계산합니다.
        /// </summary>
        private int CalculateNextLevelExperience()
        {
            return (int)(_experienceToNextLevel * 1.5f);
        }

        /// <summary>
        /// 전문화에 따른 스탯 보너스를 계산합니다.
        /// </summary>
        private Stats CalculateSpecializationBonus()
        {
            Stats bonus = new Stats();

            switch (_specialization)
            {
                case SpecializationType.Combat:
                    // 공격 전문화: 공격력과 정확도 보너스
                    return new Stats(0.2f, 0, 0, 0.15f, 0, 0, 0);
                
                case SpecializationType.Defense:
                    // 방어 전문화: 방어력과 내구도 보너스
                    return new Stats(0, 0.2f, 0, 0, 0.15f, 20.0f, 0);
                
                case SpecializationType.Support:
                    // 지원 전문화: 속도와 에너지 효율 보너스
                    return new Stats(0, 0, 0.15f, 0, 0, 0, 0.2f);
                
                case SpecializationType.Engineering:
                    // 기계 전문화: 균형 잡힌 보너스
                    return new Stats(0.1f, 0.1f, 0.1f, 0.05f, 0.05f, 10.0f, 0.1f);
                
                default:
                    return bonus;
            }
        }

        /// <summary>
        /// 스킬을 추가합니다.
        /// </summary>
        public void AddSkill(string skill)
        {
            if (!_skills.Contains(skill))
            {
                _skills.Add(skill);
            }
        }

        /// <summary>
        /// 랜덤 스킬을 추가합니다. (현재는 간단히 구현)
        /// </summary>
        private void AddRandomSkill()
        {
            // 전문화에 따른 기본 스킬 추가
            switch (_specialization)
            {
                case SpecializationType.Combat:
                    AddSkill($"Combat Skill Lv{_level / 3}");
                    break;
                case SpecializationType.Defense:
                    AddSkill($"Defense Skill Lv{_level / 3}");
                    break;
                case SpecializationType.Support:
                    AddSkill($"Support Skill Lv{_level / 3}");
                    break;
                case SpecializationType.Engineering:
                    AddSkill($"Engineering Skill Lv{_level / 3}");
                    break;
            }
        }

        /// <summary>
        /// 파일럿의 현재 총 스탯을 계산합니다.
        /// </summary>
        public Stats GetTotalStats()
        {
            return _baseStats + _specializationBonus;
        }
    }
} 