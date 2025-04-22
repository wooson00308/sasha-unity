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
            [HorizontalGroup("Layout", LabelWidth = 100)]
            [VerticalGroup("Layout/Left")]

            [LabelText("커스텀 조립")]
            public bool useCustomAssembly;

            [HideIf("useCustomAssembly")]
            [PreviewField(50, Alignment = ObjectFieldAlignment.Center), HideLabel]
            public AssemblySO assembly;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetFrameValues()")]
            public FrameSO customFrame;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetHeadPartValues()")]
            public PartSO customHead;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetBodyPartValues()")]
            public PartSO customBody;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetArmsPartValues()")]
            public PartSO customArms;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetLegsPartValues()")]
            public PartSO customLegs;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetWeaponValues()")]
            public WeaponSO customWeaponR1;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetWeaponValues()")]
            public WeaponSO customWeaponL1;

            [ShowIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetPilotValues()")]
            public PilotSO customPilot;

            [VerticalGroup("Layout/Right")]
            [LabelWidth(60)]
            public int teamId;

            [VerticalGroup("Layout/Right")]
            [LabelWidth(60)]
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
            // 공통 검증: 참가자 수, 팀 수
            if (battleParticipants.Count < 2)
            {
                Debug.LogWarning("전투 설정 검증: 최소 2명의 참가자가 필요합니다.");
            }
            else if (battleParticipants.Select(p => p.teamId).Distinct().Count() < 2)
            {
                Debug.LogWarning("전투 설정 검증: 최소 2개의 다른 팀이 필요합니다.");
            }

            // 참가자별 검증
            for (int i = 0; i < battleParticipants.Count; i++)
            {
                var setup = battleParticipants[i];
                if (setup.useCustomAssembly)
                {
                    // 커스텀 모드 검증: 필수 파츠 (Frame, Body) 확인
                    if (setup.customFrame == null || setup.customBody == null) // Head, Arms, Legs, Pilot은 필수는 아님
                    {
                         Debug.LogWarning($"전투 설정 검증 (참가자 {i+1}): 커스텀 모드에서 필수 파츠(Frame, Body)가 선택되지 않았습니다.");
                    }
                    // 추가 검증 가능 (예: 무기 장착 여부 등)
                }
                else
                {
                    // 기존 모드 검증: Assembly 확인
                    if (setup.assembly == null)
                    {
                        Debug.LogWarning($"전투 설정 검증 (참가자 {i+1}): 일반 모드에서 Assembly가 설정되지 않았습니다.");
                    }
                }
            }
        }

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("세부 로그 표시")]
        public bool logCombatDetails = true;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("로그 레벨 접두사 표시")]
        public bool showLogLevelPrefix = true;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("턴 넘버 접두사 표시")]
        public bool showTurnPrefix = true;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("로그 들여쓰기 사용")]
        public bool useLogIndentation = true;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("행동 요약 로그 표시 (성공/실패)")]
        public bool logActionSummaries = true;

        [VerticalGroup("Logging")]
        [LabelWidth(120)]
        public bool logAIDecisions = true; // AI 의사결정 로그 토글 추가

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
                Log("이미 전투가 진행 중입니다.", LogLevel.Warning);
                return;
            }

            ValidateSetup(); // 시작 전 검증

            if (battleParticipants.Count < 2 || battleParticipants.Select(p => p.teamId).Distinct().Count() < 2)
            {
                Log("전투 시작 조건 미충족: 참가자 수 또는 팀 수를 확인하세요.", LogLevel.Error);
                return;
            }

            // 서비스 로케이터에서 CombatSimulatorService 가져오기
            combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();

            if (combatSimulator == null || textLogger == null)
            {
                Log("필수 서비스(CombatSimulatorService or TextLoggerService)를 찾을 수 없습니다!", LogLevel.Critical);
                return;
            }
            
            // --- 로그 포맷 설정 --- 
            textLogger.SetShowLogLevel(showLogLevelPrefix);
            textLogger.SetShowTurnPrefix(showTurnPrefix);
            textLogger.SetUseIndentation(useLogIndentation);
            textLogger.SetLogActionSummaries(logActionSummaries);
            // ---------------------

            // 기존 로그 및 상태 초기화
            textLogger.ConcreteLogger?.Clear(); // TextLogger 직접 초기화
            isInCombat = true;
            currentTurn = 0;
            currentBattleId = Guid.NewGuid().ToString().Substring(0, 8); // 고유한 전투 ID 생성

            Log($"전투 시뮬레이션 시작 준비... (ID: {currentBattleId})", LogLevel.System);

            List<ArmoredFrame> participants = new List<ArmoredFrame>();
            for (int i = 0; i < battleParticipants.Count; i++)
            {
                var setup = battleParticipants[i];
                ArmoredFrame af = null;
                try
                {
                    if (setup.useCustomAssembly)
                    {
                        // 커스텀 AF 생성 로직 복구
                        af = CreateCustomArmoredFrame(setup, i + 1);
                        // Log($"참가자 {i+1}: 커스텀 AF 생성은 현재 지원되지 않습니다.", LogLevel.Warning);
                        // continue; // 커스텀 설정 건너뛰기 제거
                    }
                    else
                    {
                        // AssemblySO 기반 AF 생성
                        if (setup.assembly == null)
                        {
                            Log($"참가자 {i+1}: AssemblySO가 없습니다. 기본 설정으로 대체합니다.", LogLevel.Warning);
                            continue;
                        }
                        af = CreateTestArmoredFrame(setup.assembly.name, setup.teamId, setup.startPosition);
                    }

                    if (af != null)
                    {
                        participants.Add(af);
                        Log($"참가자 [{af.Name}] 생성 완료 (팀: {af.TeamId}, 위치: {af.Position})");
                    }
                }
                catch (Exception ex)
                {
                    Log($"참가자 {i+1} 생성 중 오류 발생: {ex.Message}", LogLevel.Error);
                }
            }

            if (participants.Count < 2)
            {
                Log("전투에 참여할 유효한 기체가 부족합니다.", LogLevel.Error);
                isInCombat = false;
                return;
            }

            Log("모든 참가자 생성 완료. 전투 시뮬레이션 시작.", LogLevel.System);

            // 전투 진행 로직 복구 (while 루프 사용)
            string battleName = $"Test Battle {DateTime.Now:HH:mm:ss}";
            currentBattleId = combatSimulator.StartCombat(participants.ToArray(), battleName, false);
            isInCombat = true;
            currentTurn = 1; // 턴 시작은 1부터

            try
            {
                bool combatEnded = false;
                int safetyBreak = 1000; // 무한 루프 방지
                int turnCounter = 0;

                while (!combatEnded && isInCombat && turnCounter < safetyBreak)
                {
                    // ProcessNextTurn() 반환값이 true면 전투 지속, false면 종료
                    combatEnded = !combatSimulator.ProcessNextTurn(); 
                    currentTurn = combatSimulator.CurrentTurn;
                    turnCounter++;

                    // UniTask.Yield 대신 Frame 단위 지연 등 다른 방식 고려 가능
                    await UniTask.Yield(PlayerLoopTiming.Update); 
                }

                if (turnCounter >= safetyBreak)
                {
                    Log($"안전 브레이크 발동! ({safetyBreak} 턴 초과)", LogLevel.Warning);
                    // 전투 강제 종료 (무승부 처리 등)
                    if (isInCombat) combatSimulator.EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Draw);
                }

                // 전투 결과 로그 (CombatSimulator 내부 또는 EndCombat에서 처리될 수 있음)
                // Log($"전투 종료. 결과: {combatSimulator.GetResult()}", LogLevel.System);
            }
            catch (Exception ex)
            {
                Log($"전투 시뮬레이션 중 오류 발생: {ex.Message} {ex.StackTrace}", LogLevel.Critical);
            }
            finally
            {
                // CombatSimulator 내부에서 EndCombat 호출 시 isInCombat=false 처리될 수 있음
                if (combatSimulator != null && combatSimulator.IsInCombat)
                {
                    combatSimulator.EndCombat(); // 확실하게 종료 호출
                }
                isInCombat = false; // 상태 확실히 업데이트
                currentTurn = combatSimulator?.CurrentTurn ?? currentTurn; // 최종 턴 저장
                Log("전투 프로세스 정리 완료.", LogLevel.System);

                // 로그 파일 자동 저장 (옵션)
                textLogger?.ConcreteLogger?.SaveToFile($"BattleLog_{currentBattleId}");
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
            // 공통 검증: 참가자 수, 팀 수
            if (isInCombat) return false;
            if (battleParticipants.Count < 2) return false;
            if (battleParticipants.Select(p => p.teamId).Distinct().Count() < 2) return false;

            // 참가자별 설정 유효성 검증
            foreach (var setup in battleParticipants)
            {
                if (setup.useCustomAssembly)
                {
                    // 커스텀 모드: 필수 파츠(Frame, Body) 확인
                    if (setup.customFrame == null || setup.customBody == null)
                    {
                        // Debug.LogWarning($"CanStartCombat: 커스텀 설정 오류 (팀 ID: {setup.teamId}) - 필수 파츠(Frame, Body) 누락."); // 버튼 비활성화 조건이므로 로그는 선택사항
                        return false; 
                    }
                     // 추가적인 유효성 검사 가능 (무게 제한, 에너지 등)
                }
                else
                {
                    // 기존 모드: Assembly 확인
                    if (setup.assembly == null) 
                    {
                         // Debug.LogWarning($"CanStartCombat: 일반 설정 오류 (팀 ID: {setup.teamId}) - Assembly 미설정."); // 버튼 비활성화 조건이므로 로그는 선택사항
                         return false;
                    }
                }
            }

            return true; // 모든 검증 통과
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

        /// <summary>
        /// 커스텀 설정값을 기반으로 ArmoredFrame 인스턴스를 생성합니다.
        /// </summary>
        private ArmoredFrame CreateCustomArmoredFrame(AFSetup setup, int participantIndex)
        {
            // 필수 파츠 확인 (프레임, 바디)
            if (setup.customFrame == null || setup.customBody == null)
            {
                Log($"커스텀 AF 생성 오류 (참가자 {participantIndex}): 필수 파츠(Frame 또는 Body)가 설정되지 않았습니다.", LogLevel.Error);
                return null;
            }

            // 1. 프레임 생성
            FrameSO frameData = setup.customFrame;
            Stats frameBaseStats = new Stats(
                 frameData.Stat_AttackPower, frameData.Stat_Defense, frameData.Stat_Speed, frameData.Stat_Accuracy,
                 frameData.Stat_Evasion, frameData.Stat_Durability, frameData.Stat_EnergyEff, frameData.Stat_MaxAP, frameData.Stat_APRecovery
            );
            Frame frame = null;
            switch (frameData.FrameType)
            {
                case FrameType.Standard: frame = new StandardFrame(frameData.FrameName ?? frameData.name, frameBaseStats, frameData.FrameWeight); break;
                case FrameType.Light: frame = new LightFrame(frameData.FrameName ?? frameData.name, frameBaseStats, frameData.FrameWeight); break;
                case FrameType.Heavy: frame = new HeavyFrame(frameData.FrameName ?? frameData.name, frameBaseStats, frameData.FrameWeight); break;
                default:
                    Log($"커스텀 AF 생성 오류 (참가자 {participantIndex}): 지원하지 않는 FrameType '{frameData.FrameType}' (프레임: {frameData.name}).", LogLevel.Error);
                    return null;
            }
            if (frame == null) { return null; } // 프레임 생성 실패

            // 2. ArmoredFrame 기본 인스턴스 생성
            string instanceName = $"Custom AF {participantIndex}";
            ArmoredFrame af = new ArmoredFrame(instanceName, frame, setup.startPosition, setup.teamId);

            // 3. 파일럿 할당
            if (setup.customPilot == null)
            {
                 Log($"참가자 {participantIndex}: 파일럿이 설정되지 않아 기본 파일럿을 할당합니다.", LogLevel.Warning);
                 af.AssignPilot(new Pilot("Default Pilot", new Stats(), SpecializationType.StandardCombat));
            }
            else
            {
                PilotSO pilotData = setup.customPilot;
                Stats pilotBaseStats = new Stats(
                    pilotData.Stat_AttackPower, pilotData.Stat_Defense, pilotData.Stat_Speed, pilotData.Stat_Accuracy,
                    pilotData.Stat_Evasion, pilotData.Stat_Durability, pilotData.Stat_EnergyEff, pilotData.Stat_MaxAP, pilotData.Stat_APRecovery
                );
                Pilot pilot = new Pilot(pilotData.PilotName ?? pilotData.name, pilotBaseStats, pilotData.Specialization);
                af.AssignPilot(pilot);
            }

            // 4. 파츠 부착 (헬퍼 메서드 사용 또는 직접 구현)
            AttachCustomPart(af, setup.customHead, frameData.Slot_Head);
            AttachCustomPart(af, setup.customBody, frameData.Slot_Body); // Body는 필수이므로 위에서 null 체크됨
            AttachCustomPart(af, setup.customArms, frameData.Slot_Arm_L); // 임시로 왼쪽 팔 슬롯 사용
            AttachCustomPart(af, setup.customArms, frameData.Slot_Arm_R); // 임시로 오른쪽 팔 슬롯 사용 (개선 필요)
            AttachCustomPart(af, setup.customLegs, frameData.Slot_Legs);
            // Backpack 등 다른 슬롯 추가 가능

            // 5. 무기 부착 (헬퍼 메서드 사용 또는 직접 구현)
            AttachCustomWeapon(af, setup.customWeaponR1);
            AttachCustomWeapon(af, setup.customWeaponL1);

            Log($"--- 커스텀 AF 생성 완료: [{af.Name}] (팀: {af.TeamId}) ---", LogLevel.System);

            return af;
        }

        /// <summary>
        /// 커스텀 설정된 PartSO를 ArmoredFrame에 부착하는 헬퍼 메서드
        /// </summary>
        private void AttachCustomPart(ArmoredFrame af, PartSO partSO, string slotId)
        {
            if (partSO == null || string.IsNullOrEmpty(slotId)) { return; } // 파츠 SO나 슬롯 ID 없으면 스킵

            Stats partStats = new Stats(
                partSO.Stat_AttackPower, partSO.Stat_Defense, partSO.Stat_Speed, partSO.Stat_Accuracy,
                partSO.Stat_Evasion, partSO.Stat_Durability, partSO.Stat_EnergyEff, partSO.Stat_MaxAP, partSO.Stat_APRecovery
            );
            Part runtimePart = null;
            try
            {
                // PartSO의 PartType에 따라 적절한 Part 클래스 인스턴스 생성
                switch (partSO.PartType)
                {
                    case PartType.Head: runtimePart = new HeadPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    case PartType.Body: runtimePart = new BodyPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    case PartType.Arm: runtimePart = new ArmsPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    case PartType.Legs: runtimePart = new LegsPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    // Backpack 등 다른 파츠 타입 추가 가능
                    default: Debug.LogWarning($"Unhandled PartType '{partSO.PartType}' for PartSO '{partSO.name}'."); break;
                }

                if (runtimePart != null)
                {
                    // 프레임 호환성 체크 후 부착
                    if (af.FrameBase.CanEquipPart(runtimePart, slotId))
                    {
                        af.AttachPart(runtimePart, slotId);
                    }
                    else
                    {
                        Log($"파츠 호환성 문제: {partSO.name} ({partSO.PartType}) 파츠를 슬롯 {slotId}에 장착할 수 없습니다 (프레임: {af.FrameBase.Name}).", LogLevel.Warning);
                    }
                }
            }
            catch (Exception e) { Log($"커스텀 파츠 생성/부착 오류 ({partSO.name}): {e.Message}", LogLevel.Error); }
        }

        /// <summary>
        /// 커스텀 설정된 WeaponSO를 ArmoredFrame에 부착하는 헬퍼 메서드
        /// </summary>
        private void AttachCustomWeapon(ArmoredFrame af, WeaponSO weaponSO)
        {
            if (weaponSO == null) { return; } // 무기 SO 없으면 스킵

            try {
                 Weapon runtimeWeapon = new Weapon(
                     weaponSO.WeaponName ?? weaponSO.name,
                     weaponSO.WeaponType, 
                     weaponSO.DamageType,
                     weaponSO.BaseDamage, 
                     weaponSO.Accuracy, 
                     weaponSO.Range,
                     weaponSO.AttackSpeed, 
                     weaponSO.OverheatPerShot,
                     weaponSO.BaseAPCost // AP Cost 추가
                 );
                af.AttachWeapon(runtimeWeapon);
            } catch (Exception e) { Log($"커스텀 무기 생성/부착 오류 ({weaponSO.name}): {e.Message}", LogLevel.Error); }
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

        // --- ValueDropdown용 데이터 소스 메소드들 ---

        // 인스펙터용: 직접 Resources에서 로드
        private static IEnumerable<ValueDropdownItem<FrameSO>> GetFrameValues()
        {
            // var runnerInstance = FindFirstObjectByType<CombatTestRunner>();
            // if (runnerInstance == null || runnerInstance.frameDatabase == null) return Enumerable.Empty<ValueDropdownItem<FrameSO>>();
            // return runnerInstance.frameDatabase.Values...

            return Resources.LoadAll<FrameSO>("Frames")
                .Select(f => new ValueDropdownItem<FrameSO>(f.FrameName ?? f.name, f));
        }

        // 인스펙터용: 직접 Resources에서 로드 후 필터링
        private static IEnumerable<ValueDropdownItem<PartSO>> GetPartValuesFiltered(PartType type)
        {
            // var runnerInstance = FindFirstObjectByType<CombatTestRunner>();
            // if (runnerInstance == null || runnerInstance.partDatabase == null) return Enumerable.Empty<ValueDropdownItem<PartSO>>();
            // return runnerInstance.partDatabase.Values...

             return Resources.LoadAll<PartSO>("Parts")
                 .Where(p => p != null && p.PartType == type)
                 .Select(p => new ValueDropdownItem<PartSO>($"{p.PartName ?? p.name} ({p.PartType})", p));
        }

        private static IEnumerable<ValueDropdownItem<PartSO>> GetHeadPartValues() => GetPartValuesFiltered(PartType.Head);
        private static IEnumerable<ValueDropdownItem<PartSO>> GetBodyPartValues() => GetPartValuesFiltered(PartType.Body);
        private static IEnumerable<ValueDropdownItem<PartSO>> GetArmsPartValues() => GetPartValuesFiltered(PartType.Arm); // Arms가 아닌 Arm Enum 값 확인 필요
        private static IEnumerable<ValueDropdownItem<PartSO>> GetLegsPartValues() => GetPartValuesFiltered(PartType.Legs);

        // 인스펙터용: 직접 Resources에서 로드
        private static IEnumerable<ValueDropdownItem<WeaponSO>> GetWeaponValues()
        {
            //  var runnerInstance = FindFirstObjectByType<CombatTestRunner>();
            //  if (runnerInstance == null || runnerInstance.weaponDatabase == null) return Enumerable.Empty<ValueDropdownItem<WeaponSO>>();
            // return runnerInstance.weaponDatabase.Values...

            return Resources.LoadAll<WeaponSO>("Weapons")
                .Select(w => new ValueDropdownItem<WeaponSO>(w.WeaponName ?? w.name, w));
        }

        // 인스펙터용: 직접 Resources에서 로드
        private static IEnumerable<ValueDropdownItem<PilotSO>> GetPilotValues()
        {
            //  var runnerInstance = FindFirstObjectByType<CombatTestRunner>();
            //  if (runnerInstance == null || runnerInstance.pilotDatabase == null) return Enumerable.Empty<ValueDropdownItem<PilotSO>>();
            // return runnerInstance.pilotDatabase.Values...

            return Resources.LoadAll<PilotSO>("Pilots")
                .Select(p => new ValueDropdownItem<PilotSO>(p.PilotName ?? p.name, p));
        }

        // --- End ValueDropdown용 데이터 소스 메소드들 ---
    }
} 
