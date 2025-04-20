using Sirenix.OdinInspector;
using UnityEngine;
using AF.Models;
using AF.Combat;
using AF.Services;
using System.Collections.Generic;
using System;
using AF.Data;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace AF.Tests
{
    /// <summary>
    /// 전투 시스템 테스트를 위한 시나리오 실행 및 데이터 생성 클래스
    /// </summary>
    public class CombatTestRunner : MonoBehaviour
    {
        [TitleGroup("전투 테스트 설정")]
        [InfoBox("전투 테스트에 참여할 기체와 팀 설정을 구성합니다.")]
        [Serializable]
        public class AFSetup
        {
            [PreviewField(50, Alignment = ObjectFieldAlignment.Center), HorizontalGroup("AF", Width = 50), HideLabel]
            public AssemblySO assembly;

            [VerticalGroup("AF/Right"), LabelWidth(60)]
            public int teamId;

            [VerticalGroup("AF/Right"), LabelWidth(60)]
            public Vector3 startPosition;
        }

        [FoldoutGroup("참가자 설정", expanded: true)]
        [OnValueChanged("ValidateSetup", IncludeChildren = true)]
        [ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true, DraggableItems = true, NumberOfItemsPerPage = 5)]
        [PropertySpace(10)]
        [Searchable]
        public List<AFSetup> battleParticipants = new List<AFSetup>();

        [FoldoutGroup("참가자 설정", expanded: true)]
        [PropertySpace(5), HideInPlayMode]
        [Button(ButtonSizes.Large)]
        public void AddParticipant()
        {
            battleParticipants.Add(new AFSetup { teamId = battleParticipants.Count > 0 ? battleParticipants.Max(p => p.teamId) + 1 : 0 });
        }

        private void ValidateSetup()
        {
            if (battleParticipants.Count < 2 || battleParticipants.Any(p => p.assembly == null))
            {
                 Debug.LogWarning("전투 설정 검증...");
            }
            else if (battleParticipants.Select(p => p.teamId).Distinct().Count() < 2)
            {
                 Debug.LogWarning("전투 설정 검증...");
            }
        }

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("세부 로그 표시")]
        public bool logCombatDetails = true;

        private ICombatSimulatorService combatSimulator;
        private TextLoggerService textLogger;

        [TabGroup("데이터베이스", "프레임")]
        private Dictionary<string, FrameSO> frameDatabase = new Dictionary<string, FrameSO>();
        [TabGroup("데이터베이스", "파츠")]
        private Dictionary<string, PartSO> partDatabase = new Dictionary<string, PartSO>();
        [TabGroup("데이터베이스", "무기")]
        private Dictionary<string, WeaponSO> weaponDatabase = new Dictionary<string, WeaponSO>();
        [TabGroup("데이터베이스", "파일럿")]
        private Dictionary<string, PilotSO> pilotDatabase = new Dictionary<string, PilotSO>();
        [TabGroup("데이터베이스", "어셈블리")]
        private Dictionary<string, AssemblySO> assemblyDatabase = new Dictionary<string, AssemblySO>();
        
        [FoldoutGroup("현재 전투 상태"), ReadOnly, LabelText("전투 ID")]
        public string currentBattleId;
        [FoldoutGroup("현재 전투 상태"), ReadOnly, LabelText("현재 턴")]
        public int currentTurn;
        [FoldoutGroup("현재 전투 상태"), ReadOnly, LabelText("전투 진행 중")]
        public bool isInCombat;

        private void Awake()
        {
            LoadAllScriptableObjects();
        }

        private void LoadAllScriptableObjects()
        {
            LoadResource<FrameSO>(frameDatabase, "Frames");
            LoadResource<PartSO>(partDatabase, "Parts");
            LoadResource<WeaponSO>(weaponDatabase, "Weapons");
            LoadResource<PilotSO>(pilotDatabase, "Pilots");
            LoadResource<AssemblySO>(assemblyDatabase, "Assemblies");
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
                string id = resource.name;
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

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
             if (textLogger?.TextLogger != null) { textLogger.TextLogger.Log(message, level); }
             else { Debug.LogWarning($"TextLogger not available! Log message: [{level}] {message}"); }
        }

        [BoxGroup("전투 제어", centerLabel: true)]
        [HorizontalGroup("전투 제어/Buttons")]
        [Button(ButtonSizes.Large), GUIColor(0.3f, 0.8f, 0.3f), EnableIf("CanStartCombat")]
        public async UniTaskVoid StartCombatTestAsync()
        {
            if (isInCombat)
            {
                Debug.LogWarning("현재 전투가 진행 중입니다. 먼저 종료해주세요.");
                return;
            }

            combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();
            if (combatSimulator == null || textLogger == null)
            {
                Debug.LogError("CombatTestRunner: 필요한 서비스(CombatSimulator 또는 TextLogger)를 찾을 수 없습니다.");
                return;
            }
            if (assemblyDatabase.Count == 0) LoadAllScriptableObjects();
            if (assemblyDatabase.Count == 0)
            {
                 Debug.LogError("CombatTestRunner: 데이터가 로드되지 않았습니다.");
                 return;
            }

            Debug.Log("=== 전투 테스트 시작 (UniTask 비동기 처리) ===");
            Log("=== 전투 테스트 시작 ===", LogLevel.System);

            List<ArmoredFrame> participants = new List<ArmoredFrame>();
            foreach (var setup in battleParticipants)
            {
                if (setup.assembly == null)
                {
                    Debug.LogWarning("참가자 설정 오류: Assembly가 설정되지 않은 항목이 있습니다.");
                    continue;
                }
                ArmoredFrame af = CreateTestArmoredFrame(setup.assembly.name, setup.teamId, setup.startPosition);
                if (af != null) participants.Add(af);
            }

            if (participants.Count < 2)
            {
                Debug.LogError("전투 테스트에 필요한 최소 AF 수(2)를 생성하지 못했습니다!");
                Log("전투 테스트 AF 생성 실패! (참가자 부족)", LogLevel.Critical);
                return;
            }
            if (participants.Select(p => p.TeamId).Distinct().Count() < 2)
            {
                 Debug.LogError("전투 테스트에는 최소 2개의 다른 팀이 필요합니다!");
                 Log("전투 테스트 팀 설정 오류!", LogLevel.Critical);
                 return;
            }

            string battleName = $"테스트 전투 {DateTime.Now:HH:mm:ss}";
            currentBattleId = combatSimulator.StartCombat(participants.ToArray(), battleName, false);
            isInCombat = true;
            currentTurn = 1;

            try
            {
                bool combatEnded = false;
                int safetyBreak = 1000;
                int turnCounter = 0;

                while (!combatEnded && isInCombat && turnCounter < safetyBreak)
                {
                    combatEnded = !combatSimulator.ProcessNextTurn();
                    currentTurn = combatSimulator.CurrentTurn;
                    turnCounter++;

                    await UniTask.Yield(PlayerLoopTiming.Update);
                }

                if (turnCounter >= safetyBreak)
                {
                    Debug.LogWarning($"안전 브레이크 발동! ({safetyBreak} 턴 초과)");
                    Log($"안전 브레이크 발동!", LogLevel.Warning);
                    if (isInCombat && combatSimulator != null) combatSimulator.EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Draw);
                }

                Log("=== 전투 테스트 완료 (비동기 처리) ===", LogLevel.System);
                Debug.Log($"전투 완료! Battle ID: {currentBattleId}. 로그를 확인하세요.");

                string logFileName = $"CombatLog_{currentBattleId}_{DateTime.Now:yyyyMMdd_HHmmss}";
                textLogger.TextLogger.SaveToFile(logFileName);
                Debug.Log($"전투 로그 저장됨: {logFileName}.txt");

            }
            catch (Exception ex)
            {
                Debug.LogError($"전투 시뮬레이션 중 오류 발생: {ex.Message}\n{ex.StackTrace}");
                Log($"전투 시뮬레이션 오류: {ex.Message}", LogLevel.Critical);
            }
            finally
            {
                 if (combatSimulator != null && combatSimulator.IsInCombat) {
                      combatSimulator.EndCombat();
                 }
                isInCombat = false;
            }
        }
        
        public void EndCombatTest()
        {
            if (combatSimulator != null && isInCombat)
            {
                combatSimulator.EndCombat();
                isInCombat = false;
                Log("=== 전투 테스트 중단됨 ===", LogLevel.System);
                Debug.Log("전투가 수동으로 중단되었습니다.");
            }
        }

        private bool CanStartCombat()
        {
            return !isInCombat &&
                   battleParticipants.Count >= 2 &&
                   battleParticipants.All(p => p.assembly != null) &&
                   battleParticipants.Select(p => p.teamId).Distinct().Count() >= 2;
        }

        private ArmoredFrame CreateTestArmoredFrame(string assemblyId, int teamId, Vector3 position)
        {
            if (!assemblyDatabase.TryGetValue(assemblyId, out AssemblySO assemblyData))
            {
                Debug.LogError($"CreateTestArmoredFrame Error: AssemblySO with ID '{assemblyId}' not found in database.");
                return null;
            }

            string instanceName = assemblyData.AFName;
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = assemblyId;
                Debug.LogWarning($"AssemblySO '{assemblyId}' has no specified name. Using ID as instance name.");
            }

            if (!frameDatabase.TryGetValue(assemblyData.FrameID, out FrameSO frameData))
            {
                Debug.LogError($"CreateTestArmoredFrame Error: FrameSO with ID '{assemblyData.FrameID}' not found for Assembly '{assemblyId}'.");
                return null;
            }
            Stats frameBaseStats = new Stats(
                 frameData.Stat_AttackPower, frameData.Stat_Defense, frameData.Stat_Speed, frameData.Stat_Accuracy,
                 frameData.Stat_Evasion, frameData.Stat_Durability, frameData.Stat_EnergyEff, frameData.Stat_MaxAP, frameData.Stat_APRecovery
            );
            Frame frame = null;
            switch (frameData.FrameType)
            {
                case FrameType.Standard: frame = new StandardFrame(frameData.FrameName, frameBaseStats, frameData.FrameWeight); break;
                case FrameType.Light: frame = new LightFrame(frameData.FrameName, frameBaseStats, frameData.FrameWeight); break;
                case FrameType.Heavy: frame = new HeavyFrame(frameData.FrameName, frameBaseStats, frameData.FrameWeight); break;
                default:
                    Debug.LogError($"Unsupported FrameType '{frameData.FrameType}' found for FrameID '{frameData.FrameID}'. Cannot create Frame instance.");
                    return null;
            }
            if (frame == null) { return null; }

            ArmoredFrame af = new ArmoredFrame(instanceName, frame, position, teamId);

            if (!pilotDatabase.TryGetValue(assemblyData.PilotID, out PilotSO pilotData))
            {
                 Debug.LogWarning($"PilotSO with ID '{assemblyData.PilotID}' not found for Assembly '{assemblyId}'. Using default Pilot.");
                 af.AssignPilot(new Pilot("Default Pilot", new Stats(), SpecializationType.StandardCombat));
            }
            else
            {
                Stats pilotBaseStats = new Stats(
                    pilotData.Stat_AttackPower, pilotData.Stat_Defense, pilotData.Stat_Speed, pilotData.Stat_Accuracy,
                    pilotData.Stat_Evasion, pilotData.Stat_Durability, pilotData.Stat_EnergyEff, pilotData.Stat_MaxAP, pilotData.Stat_APRecovery
                );
                Pilot pilot = new Pilot(pilotData.PilotName, pilotBaseStats, pilotData.Specialization);
                af.AssignPilot(pilot);
            }

            AttachPartFromSO(af, assemblyData.HeadPartID, frameData.Slot_Head);
            AttachPartFromSO(af, assemblyData.BodyPartID, frameData.Slot_Body);
            AttachPartFromSO(af, assemblyData.LeftArmPartID, frameData.Slot_Arm_L);
            AttachPartFromSO(af, assemblyData.RightArmPartID, frameData.Slot_Arm_R);
            AttachPartFromSO(af, assemblyData.LegsPartID, frameData.Slot_Legs);
            AttachPartFromSO(af, assemblyData.BackpackPartID, frameData.Slot_Backpack);

            if (string.IsNullOrEmpty(assemblyData.BodyPartID))
            {
                 Debug.LogError($"CreateTestArmoredFrame Error: Assembly '{assemblyId}' does not specify a Body part ID! ArmoredFrame might be non-operational.");
            }

            AttachWeaponFromSO(af, assemblyData.Weapon1ID);
            AttachWeaponFromSO(af, assemblyData.Weapon2ID);

            Log($"--- AF 생성 완료: [{af.Name}] ({assemblyId}) ---", LogLevel.System);

            return af;
        }

        private void AttachPartFromSO(ArmoredFrame af, string partId, string slotId)
        {
            if (string.IsNullOrEmpty(partId) || string.IsNullOrEmpty(slotId)) { return; }
            if (partDatabase.TryGetValue(partId, out PartSO partSO))
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
                        default: Debug.LogWarning($"Unhandled PartType '{partSO.PartType}' for ID '{partId}'."); break;
                    }
                     if (runtimePart != null)
                     {
                         if (af.FrameBase.CanEquipPart(runtimePart, slotId)) { af.AttachPart(runtimePart, slotId); }
                         else { Debug.LogWarning($"Part compatibility issue: Cannot equip {partId} ({partSO.PartType}) to slot {slotId} on frame {af.FrameBase.Name}."); }
                     }
                }
                catch (Exception e) { Debug.LogError($"Part instantiation/attach error for {partId}: {e.Message}"); }
            }
            else { Debug.LogWarning($"PartSO ID '{partId}' not found in database."); }
        }

        private void AttachWeaponFromSO(ArmoredFrame af, string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) { return; }
            if (weaponDatabase.TryGetValue(weaponId, out WeaponSO weaponSO))
            {
                try {
                     Weapon runtimeWeapon = new Weapon(
                         weaponSO.WeaponName, weaponSO.WeaponType, weaponSO.DamageType,
                         weaponSO.BaseDamage, weaponSO.Accuracy, weaponSO.Range,
                         weaponSO.AttackSpeed, weaponSO.OverheatPerShot
                     );
                    af.AttachWeapon(runtimeWeapon);
                } catch (Exception e) { Debug.LogError($"Weapon instantiation/attach error for {weaponId}: {e.Message}"); }
            }
            else { Debug.LogWarning($"WeaponSO ID '{weaponId}' not found in database."); }
        }
    }
} 
