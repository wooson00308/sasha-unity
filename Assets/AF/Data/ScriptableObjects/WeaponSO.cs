using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq;
using AF.Models;
using System.Collections.Generic; 
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewWeaponSO", menuName = "AF/Data/Weapon SO")]
    public class WeaponSO : ScriptableObject
    {
        #if UNITY_EDITOR
        [BoxGroup("Preview")]
        [ShowInInspector, PreviewField(150), ReadOnly]
        private Sprite weaponPreview;
        #endif

        [OnValueChanged("UpdatePreview")]
        public string WeaponID;
        public string WeaponName;
        public WeaponType WeaponType; // Enum
        public DamageType DamageType; // Enum
        public float BaseDamage;
        public float Accuracy;
        public float Range;
        public float AttackSpeed;
        public float OverheatPerShot;
        public int AmmoCapacity;
        public float BaseAPCost;
        public float ReloadAPCost;
        public int ReloadTurns;
        public List<string> SpecialEffects; // List
        [TextArea]
        public string Notes;

        #if UNITY_EDITOR
        [Button("Update Preview"), BoxGroup("Preview")]
        private void UpdatePreview()
        {
            weaponPreview = LoadSpritePreview(WeaponID);
        }

        private Sprite LoadSpritePreview(string weaponID)
        {
            if (string.IsNullOrEmpty(weaponID))
            {
                return null;
            }
            string path = $"Assets/AF/Sprites/Weapons/{weaponID}.png";
             Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
             return loadedSprite;
        }

        private void OnEnable()
        {
             EditorApplication.delayCall += UpdatePreview;
        }

         private void OnDisable()
         {
             EditorApplication.delayCall -= UpdatePreview;
         }
        #endif

        public void Apply(WeaponData data)
        {
            WeaponID = data.WeaponID;
            WeaponName = data.WeaponName;

            if (Enum.TryParse<WeaponType>(data.WeaponType, true, out WeaponType parsedWpnType))
            {
                WeaponType = parsedWpnType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse WeaponType: {data.WeaponType} for WeaponID: {data.WeaponID}. Defaulting to MidRange.");
                WeaponType = global::AF.Models.WeaponType.MidRange;
            }

            if (Enum.TryParse<DamageType>(data.DamageType, true, out DamageType parsedDmgType))
            {
                DamageType = parsedDmgType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse DamageType: {data.DamageType} for WeaponID: {data.WeaponID}. Defaulting to Physical.");
                DamageType = global::AF.Models.DamageType.Physical;
            }

            BaseDamage = data.BaseDamage;
            Accuracy = data.Accuracy;
            Range = data.Range;
            AttackSpeed = data.AttackSpeed;
            OverheatPerShot = data.OverheatPerShot;
            AmmoCapacity = data.AmmoCapacity;
            BaseAPCost = data.BaseAPCost;
            ReloadAPCost = data.ReloadAPCost;
            ReloadTurns = data.ReloadTurns;
            SpecialEffects = data.SpecialEffects?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim())
                                             .ToList() ?? new List<string>();
            Notes = data.Notes;

            #if UNITY_EDITOR
            UpdatePreview();
            #endif
        }
    }
} 