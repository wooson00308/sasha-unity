using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq; // For splitting strings
using AF.Models; // For Enums like FrameType
using Sirenix.OdinInspector; // Added

#if UNITY_EDITOR
using UnityEditor; // Added
#endif

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewFrameSO", menuName = "AF/Data/Frame SO")]
    public class FrameSO : ScriptableObject
    {
        #if UNITY_EDITOR
        [BoxGroup("Preview")]
        [ShowInInspector, PreviewField(150), ReadOnly] // Preview size 150
        private Sprite framePreview;
        #endif

        [OnValueChanged("UpdatePreview")] // Added
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
        [TextArea] // Added
        public string Notes;


        #if UNITY_EDITOR
        [Button("Update Preview"), BoxGroup("Preview")]
        private void UpdatePreview()
        {
            framePreview = LoadSpritePreview(FrameID);
        }

        private Sprite LoadSpritePreview(string frameID)
        {
            if (string.IsNullOrEmpty(frameID))
            {
                return null;
            }
            string path = $"Assets/AF/Sprites/Frames/{frameID}.png"; // Use Frames folder
             Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
             // Optional: Log if sprite not found
             // if (loadedSprite == null) Debug.LogWarning($"Preview sprite not found for frame ID: {frameID} at path: {path}");
             return loadedSprite;
        }

        private void OnEnable()
        {
             EditorApplication.delayCall += UpdatePreview; // Delay call to ensure AssetDatabase is ready
        }

         private void OnDisable()
         {
             EditorApplication.delayCall -= UpdatePreview;
         }
        #endif

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

            #if UNITY_EDITOR
            // Update preview after applying data from Excel
            UpdatePreview();
            #endif
        }
    }
} 