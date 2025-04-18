using UnityEngine;
using AF.Models;
using AF.Combat;
using AF.Services;
using System.Collections.Generic;
using System;
using System.Text;
using AF.Data; // Add this for SO classes
using System.Linq;
using System.Collections.Generic;

namespace AF.Tests
{
    /// <summary>
    /// 전투 시스템 테스트를 위한 시나리오 실행 및 데이터 생성 클래스
    /// </summary>
    public class CombatTestRunner : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool _runTestOnStart = true;
        [SerializeField] private bool _logCombatDetails = true;

        private ICombatSimulatorService _combatSimulator;
        private TextLoggerService _textLogger;

        // Dictionaries to hold loaded ScriptableObjects
        private Dictionary<string, FrameSO> _frameDatabase = new Dictionary<string, FrameSO>();
        private Dictionary<string, PartSO> _partDatabase = new Dictionary<string, PartSO>();
        private Dictionary<string, WeaponSO> _weaponDatabase = new Dictionary<string, WeaponSO>();
        private Dictionary<string, PilotSO> _pilotDatabase = new Dictionary<string, PilotSO>();
        private Dictionary<string, AssemblySO> _assemblyDatabase = new Dictionary<string, AssemblySO>();

        // Helper struct to define participants for a test scenario
        private struct ParticipantConfig
        {
            public string AssemblyId; // ID of the AssemblySO
            public int TeamId;        // Team identifier
            public Vector3 Position;   // Starting position

            public ParticipantConfig(string assemblyId, int teamId, Vector3 position)
            {
                AssemblyId = assemblyId;
                TeamId = teamId;
                Position = position;
            }
        }

        private void Awake() // Use Awake to load data before Start
        {
            LoadAllScriptableObjects();
        }

        private void LoadAllScriptableObjects()
        {
            LoadResource<FrameSO>(_frameDatabase, "Frames");
            LoadResource<PartSO>(_partDatabase, "Parts");
            LoadResource<WeaponSO>(_weaponDatabase, "Weapons");
            LoadResource<PilotSO>(_pilotDatabase, "Pilots");
            LoadResource<AssemblySO>(_assemblyDatabase, "Assemblies");
            Debug.Log("Loaded all ScriptableObject data.");
        }

        private void LoadResource<T>(Dictionary<string, T> database, string subfolder) where T : ScriptableObject
        {
            database.Clear();
            var resources = Resources.LoadAll<T>(subfolder);
            if (resources == null || resources.Length == 0)
            {
                 Debug.LogWarning($"No resources of type {typeof(T).Name} found in Resources/{subfolder}");
                 return;
            }

            foreach (var resource in resources)
            {
                // Use the asset's file name (without extension) as the ID key
                string id = resource.name; // Example: "AF_TEST_LIGHT_01"

                if (!string.IsNullOrEmpty(id))
                {
                    if (!database.ContainsKey(id))
                    {
                        database.Add(id, resource);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate key (resource name) '{id}' found for resource type {typeof(T).Name} in {subfolder}. Resource: {resource.name}");
                    }
                }
                else
                {
                     Debug.LogWarning($"Resource in {subfolder} has an empty name. Skipping.");
                }
            }
            Debug.Log($"Loaded {database.Count} {typeof(T).Name} resources from Resources/{subfolder}");
        }

        private void Start()
        {
            // 서비스 로케이터를 통해 필요한 서비스 가져오기
            _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            _textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();

            if (_combatSimulator == null || _textLogger == null)
            {
                Debug.LogError("CombatTestRunner: 필요한 서비스(CombatSimulator 또는 TextLogger)를 찾을 수 없습니다.");
                return;
            }

            if (_runTestOnStart)
            {
                 RunConfigurableCombatTest(); // Run the unified test method
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            // Ensure textLogger is available
             if (_textLogger?.TextLogger != null)
        {
            _textLogger.TextLogger.Log(message, level);
             }
             else
             {
                 // Fallback or error if logger isn't ready
                 Debug.LogWarning($"TextLogger not available! Log message: [{level}] {message}");
             }
        }

        /// <summary>
        /// Runs a combat test scenario defined by a list of participant configurations.
        /// </summary>
        private void RunConfigurableCombatTest()
        {
            // --- Define the Test Scenario Here --- 
            List<ParticipantConfig> scenarioConfig = new List<ParticipantConfig>
            {
                // Team 0 (Example: 3 units)
                new ParticipantConfig("AF_BLUE_LEADER", 0, new Vector3(-15, 0, 0)),
                new ParticipantConfig("AF_BLUE_ASSAULT", 0, new Vector3(-10, 0, 5)),
                //new ParticipantConfig("AF_BLUE_SCOUT", 0, new Vector3(-5, 0, -5)),
                // Team 1 (Example: 3 units)
                new ParticipantConfig("AF_RED_TANK", 1, new Vector3(40, 0, 0)),
                //new ParticipantConfig("AF_RED_SNIPER", 1, new Vector3(45, 0, -5)),
                //new ParticipantConfig("AF_RED_SUPPORT", 1, new Vector3(45, 0, 5))
            };
            // --- End of Scenario Definition ---

            // Ensure services and data are ready
             if (_combatSimulator == null || _textLogger == null)
            {
                _combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                _textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();
                if (_combatSimulator == null || _textLogger == null)
                {
                    Debug.LogError("CombatTestRunner: Services not ready for configurable test.");
                    return;
                }
            }
             if (_assemblyDatabase.Count == 0) LoadAllScriptableObjects();
             if (_assemblyDatabase.Count == 0)
             { 
                  Debug.LogError("CombatTestRunner: Data not loaded for configurable test."); 
                  return; 
             }

            Debug.Log("=== Combat Test Start ==="); // Generic log
            Log("=== Combat Test Start ===", LogLevel.System); // Generic log
            _textLogger.TextLogger.Clear();

            // 1. Create ArmoredFrames based on the scenario configuration
            // Store both the AF instance and its original config for later use (like team ID)
            List<Tuple<ArmoredFrame, ParticipantConfig>> participantData = new List<Tuple<ArmoredFrame, ParticipantConfig>>();
            foreach (var config in scenarioConfig)
            {
                ArmoredFrame af = CreateTestArmoredFrame(config.AssemblyId, config.TeamId, config.Position);
                if (af != null) 
                { 
                    participantData.Add(new Tuple<ArmoredFrame, ParticipantConfig>(af, config)); 
                }
                else
                {
                     Debug.LogWarning($"Failed to create AF for Assembly ID: {config.AssemblyId}. It will be excluded from the test.");
                     Log($"테스트 AF 생성 실패: ID {config.AssemblyId}", LogLevel.Warning);
                }
            }
            
            // Extract just the ArmoredFrame instances for the simulator
            List<ArmoredFrame> participantsList = participantData.Select(pd => pd.Item1).ToList();

            // Check if enough participants were created for a meaningful combat
            if (participantsList.Count < 2) 
            {
                Debug.LogError("전투 테스트에 필요한 최소 AF 수(2)를 생성하지 못했습니다!");
                Log("전투 테스트 AF 생성 실패! (참가자 부족)", LogLevel.Critical);
                return;
            }
            
            // Optional: Check for weapons 
            foreach(var af in participantsList)
            {
                if (af.GetAllWeapons().Count == 0)
                {
                    // Find the original config to get the Assembly ID for the warning
                    var originalConfig = participantData.FirstOrDefault(pd => pd.Item1 == af)?.Item2;
                    string assemblyIdForWarning = originalConfig?.AssemblyId ?? "Unknown Assembly";
                    Debug.LogWarning($"Test AF ({af.Name}, Assembly: {assemblyIdForWarning}) has no weapons!"); // Fixed: Use AssemblyId from config
                }
            }

            // 2. Start Combat
            ArmoredFrame[] participants = participantsList.ToArray();
            // Get team counts from the participantData list which stores the original config
            int team0Count = participantData.Count(pd => pd.Item2.TeamId == 0);
            int team1Count = participantData.Count(pd => pd.Item2.TeamId == 1); // Assuming only teams 0 and 1 for naming
            string battleName = $"{team0Count} vs {team1Count} Battle SO"; // Fixed: Use config TeamId
            string battleId = _combatSimulator.StartCombat(participants, battleName);
            Debug.Log($"Combat ({battleName}) Started with ID: {battleId}"); // Generic log
            Log($"Combat ({battleName}) Started with ID: {battleId}", LogLevel.System); // Generic log

            // 3. Combat Loop (remains the same)
            int maxTurns = 500;
            while (_combatSimulator.IsInCombat && _combatSimulator.CurrentTurn < maxTurns)
            {
                 Debug.Log($"--- Turn {_combatSimulator.CurrentTurn + 1} Processing --- ");
                Log($"--- Turn {_combatSimulator.CurrentTurn + 1} Processing --- ", LogLevel.System);
                bool turnProcessed = _combatSimulator.ProcessNextTurn();
                 if (!turnProcessed) break;
            }

            // 4. Combat End Check (remains the same)
            if (_combatSimulator.IsInCombat)
            {
                 Debug.LogWarning($"최대 턴({maxTurns}) 경과 후에도 전투 미종료. 강제 종료.");
                 Log($"최대 턴({maxTurns}) 경과 후에도 전투 미종료. 강제 종료.", LogLevel.Warning);
                _combatSimulator.EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Draw);
            }

            // 5. Final Log Output
            Debug.Log("=== Combat Test End ==="); // Generic log
            Log("=== Combat Test End ===", LogLevel.System); // Generic log
            Debug.Log("--- Final Combat Log ---"); // Generic log
            _textLogger.TextLogger.SaveToFile("CombatTest_SO"); // Generic log file name
        }

        /// <summary>
        /// Creates an ArmoredFrame instance based on data loaded from ScriptableObjects.
        /// </summary>
        /// <param name="assemblyId">The ID of the AssemblySO to use.</param>
        /// <param name="teamId">The team ID for the ArmoredFrame.</param>
        /// <param name="position">The starting position.</param>
        /// <returns>The created ArmoredFrame, or null if creation failed.</returns>
        private ArmoredFrame CreateTestArmoredFrame(string assemblyId, int teamId, Vector3 position)
        {
            // 1. Find the AssemblySO
            if (!_assemblyDatabase.TryGetValue(assemblyId, out AssemblySO assemblySO))
            {
                Debug.LogError($"AssemblySO with ID '{assemblyId}' not found in database.");
                Log($"AssemblySO 로드 실패: ID '{assemblyId}'", LogLevel.Critical);
                return null;
            }

            // <<< Start Log Changed >>>
            Log($"--- Creating: {assemblySO.AFName} (Team {teamId}) from {assemblyId} at {position} ---", LogLevel.Info); 
            
            // 2. Load referenced SOs
            if (!_frameDatabase.TryGetValue(assemblySO.FrameID, out FrameSO frameSO))
            {
                 Debug.LogError($"Required FrameSO '{assemblySO.FrameID}' not found for Assembly '{assemblyId}'.");
                 Log($"  프레임 SO 로드 실패: ID '{assemblySO.FrameID}'", LogLevel.Critical);
                 return null;
            }

            if (!_pilotDatabase.TryGetValue(assemblySO.PilotID, out PilotSO pilotSO))
            {
                Debug.LogError($"Required PilotSO '{assemblySO.PilotID}' not found for Assembly '{assemblyId}'.");
                Log($"  파일럿 SO 로드 실패: ID '{assemblySO.PilotID}'", LogLevel.Critical);
                return null;
            }

            // --- Create runtime objects from SO data ---\

            // 3. Create Frame Stats and Frame Instance
            Stats frameBaseStats = new Stats(
                frameSO.Stat_AttackPower, frameSO.Stat_Defense, frameSO.Stat_Speed, frameSO.Stat_Accuracy,
                frameSO.Stat_Evasion, frameSO.Stat_Durability, frameSO.Stat_EnergyEff, frameSO.Stat_MaxAP, frameSO.Stat_APRecovery
            );

            Frame runtimeFrame;
            try
            {
                 runtimeFrame = new StandardFrame(frameSO.FrameName, frameBaseStats, frameSO.FrameWeight);
            }
            catch (Exception e)
            {
                 Debug.LogError($"Failed to instantiate Frame ({frameSO.FrameName}) using StandardFrame: {e.Message}\n{e.StackTrace}");
                Log($"  런타임 프레임 생성 실패!", LogLevel.Critical);
                return null;
            }

            // 4. Create Pilot Stats and Pilot Instance
            Stats pilotBaseStats = new Stats(
                 pilotSO.Stat_AttackPower, pilotSO.Stat_Defense, pilotSO.Stat_Speed, pilotSO.Stat_Accuracy,
                 pilotSO.Stat_Evasion, pilotSO.Stat_Durability, pilotSO.Stat_EnergyEff, pilotSO.Stat_MaxAP, pilotSO.Stat_APRecovery
            );

            Pilot runtimePilot = new Pilot(pilotSO.PilotName, pilotBaseStats, pilotSO.Specialization);

            // <<< Frame and Pilot Summary Log >>>
            Log($"  > Frame: {runtimeFrame.Name}, Pilot: {runtimePilot.Name} ({runtimePilot.Specialization})", LogLevel.Info);

            // 5. Create ArmoredFrame instance
            ArmoredFrame af = new ArmoredFrame(assemblySO.AFName, runtimeFrame, position, teamId); 
            af.AssignPilot(runtimePilot); 

            // 6. Attach Parts using helper method
            AttachPartFromSO(af, assemblySO.HeadPartID, frameSO.Slot_Head);
            AttachPartFromSO(af, assemblySO.BodyPartID, frameSO.Slot_Body);
            AttachPartFromSO(af, assemblySO.LeftArmPartID, frameSO.Slot_Arm_L);
            AttachPartFromSO(af, assemblySO.RightArmPartID, frameSO.Slot_Arm_R);
            AttachPartFromSO(af, assemblySO.LegsPartID, frameSO.Slot_Legs);
            AttachPartFromSO(af, assemblySO.BackpackPartID, frameSO.Slot_Backpack);

            // 7. Attach Weapons using helper method
            AttachWeaponFromSO(af, assemblySO.Weapon1ID);
            AttachWeaponFromSO(af, assemblySO.Weapon2ID);

            // <<< Part Summary Log (Corrected) >>>
            StringBuilder partSummary = new StringBuilder();
            foreach (var kvp in af.Parts.OrderBy(p => p.Key)) 
            {
                if (kvp.Value != null)
                {
                    partSummary.Append($"{kvp.Key}({kvp.Value.Name}), ");
                }
            }
            if (partSummary.Length > 0) 
            {
                partSummary.Length -= 2; 
                Log($"  > Parts: {partSummary}", LogLevel.Info);
            } else {
                Log("  > Parts: None", LogLevel.Info);
            }

            // <<< Weapon Summary Log (Corrected) >>>
            StringBuilder weaponSummary = new StringBuilder();
            foreach (var weapon in af.GetAllWeapons())
            {
                 if (weapon != null)
                 {
                     weaponSummary.Append($"{weapon.Name}, ");
                 }
            }
             if (weaponSummary.Length > 0) 
             {
                 weaponSummary.Length -= 2; 
                 Log($"  > Weapons: {weaponSummary}", LogLevel.Info);
             } else {
                 Log("  > Weapons: None", LogLevel.Info);
             }

            // <<< End Log Changed >>>
            Log($"--- Finished Creating: {af.Name} ---", LogLevel.Info);

            return af;
        }

        // Helper method to find, instantiate, and attach a Part from its SO
        private void AttachPartFromSO(ArmoredFrame af, string partId, string slotId)
        {
            if (string.IsNullOrEmpty(partId) || string.IsNullOrEmpty(slotId))
            {
                 return;
            }

            if (_partDatabase.TryGetValue(partId, out PartSO partSO))
            {
                Stats partStats = new Stats(
                    partSO.Stat_AttackPower, partSO.Stat_Defense, partSO.Stat_Speed, partSO.Stat_Accuracy,
                    partSO.Stat_Evasion, partSO.Stat_Durability, partSO.Stat_EnergyEff, partSO.Stat_MaxAP, partSO.Stat_APRecovery
                );
                
                Part runtimePart = null;
                try
                {
                    switch (partSO.PartType)
                    {
                        case PartType.Head: runtimePart = new HeadPart(partSO.PartName, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                        case PartType.Body: runtimePart = new BodyPart(partSO.PartName, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                        case PartType.Arm: runtimePart = new ArmsPart(partSO.PartName, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                        case PartType.Legs: runtimePart = new LegsPart(partSO.PartName, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                        default: Debug.LogWarning($"Unhandled PartType '{partSO.PartType}'..."); Log($"미지원 파츠 타입: {partSO.PartType}", LogLevel.Warning); break;
                    }

                     if (runtimePart != null)
                     {
                         if (af.FrameBase.CanEquipPart(runtimePart, slotId))
                         {
                             af.AttachPart(runtimePart, slotId);
                         }
                         else
                         {
                             Debug.LogWarning($"Part compatibility issue..."); Log($"호환성 문제로 부착 실패...", LogLevel.Warning);
                         }
                     }
                }
                catch (Exception e) { Debug.LogError($"Part instantiation/attach error: {e.Message}"); Log($"런타임 파츠 오류...", LogLevel.Critical); }
            }
            else { Debug.LogWarning($"PartSO ID '{partId}' not found..."); Log($"파츠 SO 로드 실패: ID '{partId}'", LogLevel.Warning); }
        }

        // Helper method to find, instantiate, and attach a Weapon from its SO
        private void AttachWeaponFromSO(ArmoredFrame af, string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) { return; }

            if (_weaponDatabase.TryGetValue(weaponId, out WeaponSO weaponSO))
            {
                // Corrected: Use the 8-argument constructor as defined in api.mdc
                // Corrected: Use weaponSO.WeaponType instead of weaponSO.Type
                Weapon runtimeWeapon = new Weapon(
                     weaponSO.WeaponName,
                     weaponSO.WeaponType, // Corrected from weaponSO.Type
                     weaponSO.DamageType,
                     weaponSO.BaseDamage,
                     weaponSO.Accuracy,
                     weaponSO.Range,
                     weaponSO.AttackSpeed,
                     weaponSO.OverheatPerShot
                     // AmmoCapacity and BaseAPCost are likely properties or handled elsewhere, not in this constructor
                );
                
                // TODO: Set AmmoCapacity and BaseAPCost as properties if they exist on Weapon class
                // runtimeWeapon.AmmoCapacity = weaponSO.AmmoCapacity; 
                // runtimeWeapon.BaseAPCost = weaponSO.BaseAPCost; 
                
                af.AttachWeapon(runtimeWeapon);
            }
            else 
            { 
                Debug.LogWarning($"WeaponSO ID '{weaponId}' not found..."); 
                Log($"무기 SO 로드 실패: ID '{weaponId}'", LogLevel.Warning); 
            }
        } // Corrected: Ensure only one closing brace for the method
    }
} 
