using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AF.Models
{
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
        private List<Skill> _skills;

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
        public IReadOnlyList<Skill> Skills => _skills;
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
            _specialization = SpecializationType.StandardCombat;
            _skills = new List<Skill>();
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
            _skills = new List<Skill>();
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
                case SpecializationType.StandardCombat:
                    // 공격 전문화: 공격력과 정확도 보너스
                    return new Stats(0.2f, 0, 0, 0.15f, 0, 0, 1.0f, 0, 0);
                
                case SpecializationType.MeleeCombat:
                    // 근접 전투 전문화: 공격력, 방어력, 내구도 보너스
                    return new Stats(0.25f, 0.15f, 0.05f, 0f, 0f, 15.0f, 1.0f, 0f, 0f);
                
                case SpecializationType.RangedCombat:
                    // 원거리 전투 전문화: 정확도, 속도, 약간의 공격력/에너지 효율 보너스
                    return new Stats(0.1f, 0f, 0.15f, 0.25f, 0f, 0f, 1.05f, 0f, 0f);
                
                case SpecializationType.Defense:
                    // 방어 전문화: 방어력과 내구도 보너스
                    return new Stats(0, 0.2f, 0, 0, 0.15f, 20.0f, 1.0f, 0, 0);
                
                case SpecializationType.Support:
                    // 지원 전문화: 속도와 에너지 효율 보너스
                    return new Stats(0, 0, 0.15f, 0, 0, 0, 1.2f, 0, 0);
                
                case SpecializationType.Engineering:
                    // 기계 전문화: 균형 잡힌 보너스
                    return new Stats(0.1f, 0.1f, 0.1f, 0.05f, 0.05f, 10.0f, 1.1f, 0, 0);
                
                default:
                    return bonus;
            }
        }

        /// <summary>
        /// 스킬을 추가합니다. 동일한 이름의 스킬이 이미 있다면 추가하지 않습니다.
        /// </summary>
        public void AddSkill(Skill skill)
        {
            if (skill != null && !_skills.Any(s => s.Name == skill.Name))
            {
                _skills.Add(skill);
                Debug.Log($"Pilot ({Name}): 스킬 '{skill.Name}' 추가됨.");
            }
            else if (skill != null)
            {
                Debug.LogWarning($"Pilot ({Name}): 스킬 '{skill.Name}'은(는) 이미 보유하고 있습니다.");
            }
        }

        /// <summary>
        /// 랜덤 스킬을 추가합니다. (현재는 간단히 구현 - Skill 객체 생성 필요)
        /// </summary>
        private void AddRandomSkill()
        {
            // TODO: 실제 스킬 데이터를 로드하거나 정의하여 Skill 객체를 생성해야 함
            // 아래는 임시 예시입니다. 실제 구현에서는 SkillDatabase 등에서 가져와야 합니다.
            Skill newSkill = null;
            string skillNameBase = "";

            switch (_specialization)
            {
                case SpecializationType.StandardCombat:
                    skillNameBase = "Combat"; break;
                case SpecializationType.Defense:
                    skillNameBase = "Defense"; break;
                case SpecializationType.Support:
                    skillNameBase = "Support"; break;
                case SpecializationType.Engineering:
                    skillNameBase = "Engineering"; break;
                default: return; // 해당 전문화 타입에 맞는 스킬 없으면 추가 안함
            }

            // 임시 Skill 객체 생성 (실제로는 데이터 기반으로 생성해야 함)
            newSkill = new Skill(
                name: $"{skillNameBase} Skill Lv{_level / 3}",
                description: $"Level {_level / 3} {skillNameBase} specialization skill.",
                apCost: 2.0f + (_level / 3), // 레벨 따라 AP 증가 (예시)
                cooldownTurns: 3 + (_level / 3), // 레벨 따라 쿨다운 증가 (예시)
                icon: null // 아이콘은 별도 로드 필요
            );

            AddSkill(newSkill);
        }

        /// <summary>
        /// 턴 시작 시 모든 스킬의 쿨다운을 감소시킵니다.
        /// </summary>
        public void TickSkillCooldowns()
        {
            if (_skills == null) return;
            foreach(var skill in _skills)
            {
                skill.TickCooldown();
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