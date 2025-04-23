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
    [CreateAssetMenu(fileName = "NewPilotSO", menuName = "AF/Data/Pilot SO")]
    public class PilotSO : ScriptableObject
    {
        #if UNITY_EDITOR
        [BoxGroup("Preview")]
        [ShowInInspector, PreviewField(150), ReadOnly]
        private Sprite pilotPreview;
        #endif

        [OnValueChanged("UpdatePreview")]
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
        [TextArea]
        public string Notes;

        #if UNITY_EDITOR
        [Button("Update Preview"), BoxGroup("Preview")]
        private void UpdatePreview()
        {
            pilotPreview = LoadSpritePreview(PilotID);
        }

        private Sprite LoadSpritePreview(string pilotID)
        {
            if (string.IsNullOrEmpty(pilotID))
            {
                return null;
            }
            string path = $"Assets/AF/Sprites/Pilots/{pilotID}.png";
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
                Specialization = SpecializationType.StandardCombat;
            }

            InitialLevel = data.InitialLevel;
            InitialSkills = data.InitialSkills?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Select(s => s.Trim())
                                           .ToList() ?? new List<string>();
            Notes = data.Notes;

            #if UNITY_EDITOR
            UpdatePreview();
            #endif
        }
    }
} 