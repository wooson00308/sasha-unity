using UnityEngine;
using ExcelToSO.DataModels;
using System;
using System.Linq;
using AF.Models;
using System.Collections.Generic; // For List
using Sirenix.OdinInspector; // Added

#if UNITY_EDITOR
using UnityEditor; // Added
// using AF.EditorUtils; // Removed using statement
#endif

namespace AF.Data
{
    [CreateAssetMenu(fileName = "NewPartSO", menuName = "AF/Data/Part SO")]
    public class PartSO : ScriptableObject
    {
        #if UNITY_EDITOR
        [BoxGroup("Preview")]
        [ShowInInspector, PreviewField(150), ReadOnly] // Preview size 150
        private Sprite partPreview;
        #endif

        [OnValueChanged("UpdatePreview")] // Added
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
        [TextArea] // Added for consistency
        public string Notes;

        #if UNITY_EDITOR
        [Button("Update Preview"), BoxGroup("Preview")]
        private void UpdatePreview()
        {
            // Reverted to use local method
            partPreview = LoadSpritePreview(PartID);
        }

        // Re-added the LoadSpritePreview method
        private Sprite LoadSpritePreview(string partID)
        {
            if (string.IsNullOrEmpty(partID))
            {
                return null;
            }
            // Assuming sprites are named exactly like PartIDs and are in the specified folder
            string path = $"Assets/AF/Sprites/Parts/{partID}.png";
             Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
             // Optional: Log if sprite not found
             // if (loadedSprite == null) Debug.LogWarning($"Preview sprite not found for part ID: {partID} at path: {path}");
             return loadedSprite;
        }

        // Call UpdatePreview when the SO is enabled in the editor or selection changes
        private void OnEnable()
        {
             EditorApplication.delayCall += UpdatePreview; // Delay call to ensure AssetDatabase is ready
        }

         private void OnDisable()
         {
             EditorApplication.delayCall -= UpdatePreview;
         }
        #endif

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

            #if UNITY_EDITOR
            // Update preview after applying data from Excel
            UpdatePreview();
            #endif
        }
    }
} 