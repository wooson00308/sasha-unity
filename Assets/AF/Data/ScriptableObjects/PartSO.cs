using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq;
using AF.Models;
using System.Collections.Generic; // For List

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewPartSO", menuName = "AF/Data/Part SO")]
    public class PartSO : ScriptableObject
    {
        public string PartID;
        public string PartName;
        public PartType PartType; // Enum
        public float Stat_AttackPower;
        public float Stat_Defense;
        public float Stat_Speed;
        public float Stat_Accuracy;
        public float Stat_Evasion;
        public float Stat_Durability;
        public float Stat_EnergyEff;
        public float Stat_MaxAP;
        public float Stat_APRecovery;
        public float MaxDurability;
        public float PartWeight;
        public List<string> Abilities; // Store as list
        public string Notes;

        public void Apply(PartData data)
        {
            PartID = data.PartID;
            PartName = data.PartName;

            if (Enum.TryParse<PartType>(data.PartType, true, out PartType parsedType))
            {
                PartType = parsedType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse PartType: {data.PartType} for PartID: {data.PartID}. Defaulting to Body.");
                PartType = global::AF.Models.PartType.Body; // Default value or handle error
            }

            Stat_AttackPower = data.Stat_AttackPower;
            Stat_Defense = data.Stat_Defense;
            Stat_Speed = data.Stat_Speed;
            Stat_Accuracy = data.Stat_Accuracy;
            Stat_Evasion = data.Stat_Evasion;
            Stat_Durability = data.Stat_Durability;
            Stat_EnergyEff = data.Stat_EnergyEff;
            Stat_MaxAP = data.Stat_MaxAP;
            Stat_APRecovery = data.Stat_APRecovery;
            MaxDurability = data.MaxDurability;
            PartWeight = data.PartWeight;
            
            // Split comma-separated string into List<string>
            Abilities = data.Abilities?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(s => s.Trim())
                                     .ToList() ?? new List<string>();
            Notes = data.Notes;
        }
    }
} 