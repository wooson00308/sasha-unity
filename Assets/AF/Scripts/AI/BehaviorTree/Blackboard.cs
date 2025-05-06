using AF.Combat;
using AF.Models;
using UnityEngine;
using System.Collections.Generic;

namespace AF.AI.BehaviorTree
{
    /// <summary>
    /// 행동 트리 노드 간 데이터 공유 및 최종 행동 결정을 저장하는 클래스.
    /// 각 ArmoredFrame 에이전트는 자신만의 Blackboard 인스턴스를 가집니다.
    /// </summary>
    public class Blackboard
    {
        // 일반적인 데이터 저장을 위한 딕셔너리
        private Dictionary<string, object> _data = new Dictionary<string, object>();

        // 자주 사용될 것으로 예상되는 데이터에 대한 명시적 속성
        public ArmoredFrame CurrentTarget { get; set; }
        public Vector3? IntendedMovePosition { get; set; }
        public CombatActionEvents.ActionType? DecidedActionType { get; set; }
        public Weapon SelectedWeapon { get; set; }

        // 향후 AI가 특정 상태나 플래그를 기억해야 할 때 사용할 수 있는 일반적인 값들
        public bool HasReachedTarget { get; set; } = false;
        public bool IsAlerted { get; set; } = false;


        // 제네릭 메서드를 사용한 데이터 설정
        public void SetData<T>(string key, T value)
        {
            _data[key] = value;
        }

        // 제네릭 메서드를 사용한 데이터 검색 (기본값 반환 기능 포함)
        public T GetData<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out object value))
            {
                if (value is T typedValue)
                {
                    return typedValue;
                }
            }
            return defaultValue;
        }

        public bool HasData(string key)
        {
            return _data.ContainsKey(key);
        }

        public void ClearData(string key)
        {
            _data.Remove(key);
        }

        public void ClearAllData()
        {
            _data.Clear();
            CurrentTarget = null;
            IntendedMovePosition = null;
            DecidedActionType = null;
            SelectedWeapon = null;
            HasReachedTarget = false;
            IsAlerted = false;
        }
    }
} 