using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq;
using AF.Models;
using System.Collections.Generic;

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewPilotSO", menuName = "AF/Data/Pilot SO")]
    public class PilotSO : ScriptableObject
    {
        public string PilotID;
        public string PilotName;
        public float Stat_AttackPower;
        public float Stat_Defense;
        public float Stat_Speed;
        public float Stat_Accuracy;
        public float Stat_Evasion;
        public float Stat_Durability;
        public float Stat_EnergyEff;
        public float Stat_MaxAP;
        public float Stat_APRecovery;
        public SpecializationType Specialization; // Enum
        public int InitialLevel;
        public List<string> InitialSkills; // List
        public string Notes;

        public void Apply(PilotData data)
        {
            PilotID = data.PilotID;
            PilotName = data.PilotName;
            Stat_AttackPower = data.Stat_AttackPower;
            Stat_Defense = data.Stat_Defense;
            Stat_Speed = data.Stat_Speed;
            Stat_Accuracy = data.Stat_Accuracy;
            Stat_Evasion = data.Stat_Evasion;
            Stat_Durability = data.Stat_Durability;
            Stat_EnergyEff = data.Stat_EnergyEff;
            Stat_MaxAP = data.Stat_MaxAP;
            Stat_APRecovery = data.Stat_APRecovery;

            if (Enum.TryParse<SpecializationType>(data.Specialization, true, out SpecializationType parsedSpec))
            {
                Specialization = parsedSpec;
            }
            else
            {
                Debug.LogWarning($"Failed to parse Specialization: {data.Specialization} for PilotID: {data.PilotID}. Defaulting to StandardCombat.");
                Specialization = SpecializationType.StandardCombat; // Default value changed to StandardCombat
            }

            InitialLevel = data.InitialLevel;
            InitialSkills = data.InitialSkills?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .ToList() ?? new List<string>();
            Notes = data.Notes;
        }
    }
} 