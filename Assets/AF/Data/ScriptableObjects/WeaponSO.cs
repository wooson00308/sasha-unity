using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq;
using AF.Models;
using System.Collections.Generic; 

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewWeaponSO", menuName = "AF/Data/Weapon SO")]
    public class WeaponSO : ScriptableObject
    {
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
        public List<string> SpecialEffects; // List
        public string Notes;

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
            SpecialEffects = data.SpecialEffects?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim())
                                             .ToList() ?? new List<string>();
            Notes = data.Notes;
        }
    }
} 