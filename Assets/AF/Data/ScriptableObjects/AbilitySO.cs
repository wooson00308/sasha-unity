using UnityEngine;
using ExcelToSO.DataModels;
using AF.Models; // For Enums
using System;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AF.Data
{
    [CreateAssetMenu(fileName = "ABSO_", menuName = "AF/Data/Ability SO")]
    public class AbilitySO : ScriptableObject
    {
        [BoxGroup("Base Info")]
        [OnValueChanged("UpdateEditorTitle")]
        public string AbilityID;
        [BoxGroup("Base Info")]
        public string AbilityName;
        [BoxGroup("Base Info")]
        [TextArea(3,5)]
        public string Description;

        [BoxGroup("Behavior")]
        public AbilityType AbilityType;
        [BoxGroup("Behavior")]
        public AbilityTargetType TargetType;
        [BoxGroup("Behavior")]
        public AbilityEffectType EffectType;
        [BoxGroup("Behavior")]
        [TextArea(2,5)]
        [Tooltip("예: Stat:AttackPower,ModType:Additive,Value:10,Duration:3;StatusID:Burn,Chance:0.5")]
        public string EffectParametersRaw; // Excel에서 읽어온 원본 문자열
        
        [BoxGroup("Costs & Conditions")]
        public float APCost;
        [BoxGroup("Costs & Conditions")]
        public int CooldownTurns;
        [BoxGroup("Costs & Conditions")]
        [Tooltip("예: OnHit, OnKill, HPLessThan:30%, TurnStart, Always")]
        public string ActivationConditionRaw; // Excel에서 읽어온 원본 문자열

        [BoxGroup("Visuals & Audio")]
        public string VisualEffectKey;
        [BoxGroup("Visuals & Audio")]
        public string SoundEffectKey;
        [BoxGroup("Visuals & Audio")]
        [OnValueChanged("UpdateIconPreview")]
        [PreviewField(50, ObjectFieldAlignment.Left)]
        public Sprite Icon; // 아이콘 직접 참조 방식으로 변경

        [BoxGroup("Meta")]
        [TextArea(2,5)]
        public string Notes;

#if UNITY_EDITOR
        private void UpdateEditorTitle()
        {
            if (!string.IsNullOrEmpty(AbilityID))
            {
                string currentPath = AssetDatabase.GetAssetPath(this);
                if (!string.IsNullOrEmpty(currentPath))
                {
                    string newName = $"ABSO_{AbilityID}";
                    EditorUtility.SetDirty(this); // 변경 사항을 저장하도록 표시
                    AssetDatabase.RenameAsset(currentPath, newName);
                    AssetDatabase.SaveAssets();
                }
            }
        }
        private void UpdateIconPreview()
        {
            // 아이콘은 직접 Sprite 필드에 할당하므로 별도 로직 불필요
        }

        // SO 파일이 선택될 때 아이콘 프리뷰 업데이트 (선택 사항)
        private void OnEnable()
        {
            //UpdateIconPreview(); // Icon 필드가 직접 Sprite를 가지므로 OnEnable에서 특별히 할 일 없음
        }
#endif

        public void Apply(AbilityData data)
        {
            AbilityID = data.AbilityID;
            AbilityName = data.AbilityName;
            Description = data.Description;

            if (Enum.TryParse<AbilityType>(data.AbilityType, true, out AbilityType parsedAbilityType))
            {
                AbilityType = parsedAbilityType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse AbilityType: {data.AbilityType} for AbilityID: {data.AbilityID}. Defaulting to Passive.");
                AbilityType = Models.AbilityType.Passive;
            }

            if (Enum.TryParse<AbilityTargetType>(data.TargetType, true, out AbilityTargetType parsedTargetType))
            {
                TargetType = parsedTargetType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse TargetType: {data.TargetType} for AbilityID: {data.AbilityID}. Defaulting to None.");
                TargetType = Models.AbilityTargetType.None;
            }

            if (Enum.TryParse<AbilityEffectType>(data.EffectType, true, out AbilityEffectType parsedEffectType))
            {
                EffectType = parsedEffectType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse EffectType: {data.EffectType} for AbilityID: {data.AbilityID}. Defaulting to None.");
                EffectType = Models.AbilityEffectType.None;
            }

            EffectParametersRaw = data.EffectParameters;
            APCost = data.APCost;
            CooldownTurns = data.CooldownTurns;
            ActivationConditionRaw = data.ActivationCondition;
            VisualEffectKey = data.VisualEffectKey;
            SoundEffectKey = data.SoundEffectKey;
            Notes = data.Notes;

#if UNITY_EDITOR
            // 아이콘 ID를 기반으로 에디터에서 아이콘 로드 (IconID 필드가 AbilityData에 있다고 가정)
            if (!string.IsNullOrEmpty(data.IconID))
            {
                Icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/AF/Sprites/Icons/Abilities/{data.IconID}.png");
                if (Icon == null)
                {
                    Debug.LogWarning($"Icon sprite not found for IconID: {data.IconID} at Assets/AF/Sprites/Icons/Abilities/{data.IconID}.png");
                }
            }
            else
            {
                Icon = null;
            }
            EditorUtility.SetDirty(this);
#endif
        }
    }
} 