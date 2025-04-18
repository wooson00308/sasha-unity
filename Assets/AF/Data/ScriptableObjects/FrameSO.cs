using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq; // For splitting strings
using AF.Models; // For Enums like FrameType

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewFrameSO", menuName = "AF/Data/Frame SO")]
    public class FrameSO : ScriptableObject
    {
        // Corresponding fields for FrameData properties
        public string FrameID;
        public string FrameName;
        public FrameType FrameType; // Use the actual Enum
        public float Stat_AttackPower;
        public float Stat_Defense;
        public float Stat_Speed;
        public float Stat_Accuracy;
        public float Stat_Evasion;
        public float Stat_Durability;
        public float Stat_EnergyEff;
        public float Stat_MaxAP;
        public float Stat_APRecovery;
        public float FrameWeight;
        public string Slot_Head;
        public string Slot_Body;
        public string Slot_Arm_L;
        public string Slot_Arm_R;
        public string Slot_Legs;
        public string Slot_Backpack;
        public string Slot_Weapon_1;
        public string Slot_Weapon_2;
        public string Notes;

        // Apply method to populate fields from FrameData
        public void Apply(FrameData data)
        {
            FrameID = data.FrameID;
            FrameName = data.FrameName;
            
            // Safely parse FrameType enum
            if (Enum.TryParse<FrameType>(data.FrameType, true, out FrameType parsedType))
            {
                FrameType = parsedType;
            }
            else
            {
                Debug.LogWarning($"Failed to parse FrameType: {data.FrameType} for FrameID: {data.FrameID}. Defaulting to Standard.");
                FrameType = global::AF.Models.FrameType.Standard; // Default value
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
            FrameWeight = data.FrameWeight;
            Slot_Head = data.Slot_Head;
            Slot_Body = data.Slot_Body;
            Slot_Arm_L = data.Slot_Arm_L;
            Slot_Arm_R = data.Slot_Arm_R;
            Slot_Legs = data.Slot_Legs;
            Slot_Backpack = data.Slot_Backpack;
            Slot_Weapon_1 = data.Slot_Weapon_1;
            Slot_Weapon_2 = data.Slot_Weapon_2;
            Notes = data.Notes;
        }
    }
} 