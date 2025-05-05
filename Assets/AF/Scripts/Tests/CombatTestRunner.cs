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

#if UNITY_EDITOR
using UnityEditor; // Needed for AssetDatabase
using Sirenix.Utilities.Editor; // For InspectorMessageBox
#endif

namespace AF.Tests
{
    /// <summary>
    /// 전투 시스템 테스트를 위한 시나리오 실행 및 데이터 생성 클래스
    /// </summary>
    public class CombatTestRunner : MonoBehaviour
    {
        // --- Re-add Color Palette for Team IDs ---
        private static readonly List<Color> teamColorPalette = new List<Color>
        {
            new Color(0.9f, 0.2f, 0.2f), // Red
            new Color(0.2f, 0.4f, 0.9f), // Blue
            new Color(0.2f, 0.8f, 0.3f), // Green
            new Color(0.9f, 0.8f, 0.2f), // Yellow
            new Color(0.8f, 0.3f, 0.9f), // Magenta
            new Color(0.3f, 0.8f, 0.9f), // Cyan
            new Color(0.9f, 0.5f, 0.2f), // Orange
            new Color(0.6f, 0.3f, 0.9f), // Purple
            new Color(0.5f, 0.9f, 0.2f), // Lime
            new Color(0.2f, 0.7f, 0.7f), // Teal
            Color.grey // Default/fallback
        };
        private Dictionary<int, Color> _currentTeamColors = new Dictionary<int, Color>();
        // --- End Re-add Color Palette ---
        public static CombatTestRunner Instance { get; private set; }

        // +++ Player Squad State Storage +++
        private Dictionary<string, ArmoredFrame> _persistentPlayerFrames = new Dictionary<string, ArmoredFrame>();
        // +++ End Player Squad State Storage +++

        [TitleGroup("전투 테스트 설정")]
        [InfoBox("전투 테스트에 참여할 기체와 팀 설정을 구성합니다.")]
        [Serializable]
        public class AFSetup
        {
            // --- Mode Selection & Core Fields ---
            [HorizontalGroup("TopConfig", LabelWidth = 100)]
            [VerticalGroup("TopConfig/Left")]
            [LabelText("커스텀 조립")]
            [OnValueChanged("UpdatePreviews")]
            public bool useCustomAssembly;

            [VerticalGroup("TopConfig/Left")]
            [Required("유닛 이름을 입력해야 합니다.")]
            [ValidateInput("IsNameUnique", "유닛 이름이 중복됩니다!", InfoMessageType.Error)]
            [OnValueChanged("OnNameChanged")]
            [LabelText("이름 (콜사인)")]
            [InfoBox("다른 유닛과 겹치지 않는 고유한 이름을 입력하세요.", InfoMessageType.None, VisibleIf = "@useCustomAssembly")]
            [EnableIf("useCustomAssembly")]
            [PropertyOrder(-1)]
            public string customAFName;

            [VerticalGroup("TopConfig/Left")]
            [EnableIf("@!useCustomAssembly")]
            [OnValueChanged("UpdatePreviews")]
            [PreviewField(Alignment = Sirenix.OdinInspector.ObjectFieldAlignment.Left), HideLabel]
            public AssemblySO assembly;

            // --- Right Side Grouping ---
            [VerticalGroup("TopConfig/Right")]
            [HorizontalGroup("TopConfig/Right/TeamInfo", LabelWidth = 60)] // Team ID + Color Row
            [OnValueChanged("@AF.Tests.CombatTestRunner.Instance?.ValidateSetup()")] // Update color on change
            public int teamId;

            [HorizontalGroup("TopConfig/Right/TeamInfo", Width = 30)] // Color Box
            [HideLabel, ReadOnly]
            public Color teamColorPreview; // Re-add color preview field

            [PropertySpace(5)] // Space below Team info
            [LabelWidth(60)]
            public Vector3 startPosition;
            // --- End Right Side Grouping ---

            // --- Unified Parts Configuration & Preview --- 
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Frame", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/Frame/SO")]
            [EnableIf("useCustomAssembly")] // SO Field enabled only in custom mode
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetFrameValues()")]
            [OnValueChanged("UpdatePreviews")]
            public FrameSO customFrame;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Frame/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel] // Preview always visible
            private Sprite customFramePreview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Head", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/Head/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetHeadPartValues()")]
            [OnValueChanged("UpdatePreviews")]
            public PartSO customHead;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Head/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customHeadPreview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Body", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/Body/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetBodyPartValues()")]
            [OnValueChanged("UpdatePreviews")]
            public PartSO customBody;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Body/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customBodyPreview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Arms", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/Arms/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetArmsPartValues()")]
            [OnValueChanged("UpdatePreviews")]
            public PartSO customArms; // Assuming single Arms SO field
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Arms/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customArmsPreview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Legs", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/Legs/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetLegsPartValues()")]
            [OnValueChanged("UpdatePreviews")]
            public PartSO customLegs;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Legs/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customLegsPreview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/WeaponR1", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/WeaponR1/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetWeaponValues()")]
            [OnValueChanged("UpdatePreviews")]
            public WeaponSO customWeaponR1;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/WeaponR1/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customWeaponR1Preview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/WeaponL1", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/WeaponL1/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetWeaponValues()")]
            [OnValueChanged("UpdatePreviews")]
            public WeaponSO customWeaponL1;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/WeaponL1/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customWeaponL1Preview;

            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Pilot", LabelWidth = 100)]
            [VerticalGroup("파츠 구성/Pilot/SO")]
            [EnableIf("useCustomAssembly")]
            [ValueDropdown("@AF.Tests.CombatTestRunner.GetPilotValues()")]
            [OnValueChanged("UpdatePreviews")]
            public PilotSO customPilot;
            [FoldoutGroup("파츠 구성")]
            [HorizontalGroup("파츠 구성/Pilot/Preview")]
            [ShowInInspector, PreviewField(80), ReadOnly, HideLabel]
            private Sprite customPilotPreview;

            // +++ Add Unique ID for Persistence Tracking +++
            [FoldoutGroup("식별자 (상태 유지용)")]
            [Tooltip("플레이어 스쿼드 유닛의 상태를 추적하기 위한 고유 ID입니다. '이름 (콜사인)' 필드를 기반으로 자동 설정됩니다.")]
            [ShowIf("IsPlayerSquadMember")]
            [ReadOnly]
            public string persistentId;
            // +++ End Add Unique ID +++

            #if UNITY_EDITOR
            [Button("Update Previews (Manual)")]
            private void UpdatePreviews()
            {
                // Always clear previews first
                customFramePreview = null;
                customPilotPreview = null;
                customHeadPreview = null;
                customBodyPreview = null;
                customArmsPreview = null;
                customLegsPreview = null;
                customWeaponR1Preview = null;
                customWeaponL1Preview = null;

                if (useCustomAssembly)
                {
                    // Custom Assembly Mode
                    // Update previews based on custom SO fields
                    customFramePreview = LoadSpritePreview(customFrame?.FrameID, "Frames");
                    customPilotPreview = LoadSpritePreview(customPilot?.PilotID, "Pilots");
                    customHeadPreview = LoadSpritePreview(customHead?.PartID, "Parts");
                    customBodyPreview = LoadSpritePreview(customBody?.PartID, "Parts");
                    customArmsPreview = LoadSpritePreview(customArms?.PartID, "Parts");
                    customLegsPreview = LoadSpritePreview(customLegs?.PartID, "Parts");
                    customWeaponR1Preview = LoadSpritePreview(customWeaponR1?.WeaponID, "Weapons");
                    customWeaponL1Preview = LoadSpritePreview(customWeaponL1?.WeaponID, "Weapons");

                    // persistentId is now derived from customAFName in OnNameChanged
                    // Ensure customAFName is editable (handled by EnableIf)
                }
                else // Assembly Mode
                {
                    if (assembly != null)
                    {
                        // Load SOs from Resources based on the selected AssemblySO
                        FrameSO frameSO = FindResource<FrameSO>("Frames", assembly.FrameID);
                        PartSO headSO = FindResource<PartSO>("Parts", assembly.HeadPartID);
                        PartSO bodySO = FindResource<PartSO>("Parts", assembly.BodyPartID);
                        PartSO armsSO = FindResource<PartSO>("Parts", assembly.LeftArmPartID);
                        PartSO legsSO = FindResource<PartSO>("Parts", assembly.LegsPartID);
                        WeaponSO weapon1SO = FindResource<WeaponSO>("Weapons", assembly.Weapon1ID);
                        WeaponSO weapon2SO = FindResource<WeaponSO>("Weapons", assembly.Weapon2ID);
                        PilotSO pilotSO = FindResource<PilotSO>("Pilots", assembly.PilotID);

                        // Populate the disabled custom fields (including name)
                        customFrame = frameSO;
                        customHead = headSO;
                        customBody = bodySO;
                        customArms = armsSO;
                        customLegs = legsSO;
                        customWeaponR1 = weapon1SO;
                        customWeaponL1 = weapon2SO;
                        customPilot = pilotSO;
                        customAFName = assembly.AFName ?? assembly.name; // Populate name from AssemblySO

                        // Update previews based on the loaded SOs
                        customFramePreview = LoadSpritePreview(frameSO?.FrameID, "Frames");
                        customPilotPreview = LoadSpritePreview(pilotSO?.PilotID, "Pilots");
                        customHeadPreview = LoadSpritePreview(headSO?.PartID, "Parts");
                        customBodyPreview = LoadSpritePreview(bodySO?.PartID, "Parts");
                        customArmsPreview = LoadSpritePreview(armsSO?.PartID, "Parts");
                        customLegsPreview = LoadSpritePreview(legsSO?.PartID, "Parts");
                        customWeaponR1Preview = LoadSpritePreview(weapon1SO?.WeaponID, "Weapons");
                        customWeaponL1Preview = LoadSpritePreview(weapon2SO?.WeaponID, "Weapons");

                        // Populate customAFName from assembly name
                        this.customAFName = assembly.AFName ?? assembly.name;
                        
                        // Update persistentId based on the newly set customAFName
                        OnNameChanged(); // Trigger persistentId update
                    }
                    else // Assembly is null in Assembly Mode
                    {
                        // Clear the custom fields and persistent ID
                        customFrame = null;
                        customHead = null;
                        customBody = null;
                        customArms = null;
                        customLegs = null;
                        customWeaponR1 = null;
                        customWeaponL1 = null;
                        customPilot = null;
                        customAFName = null; // Clear name field
                        this.persistentId = null;
                    }
                }
                // Instance?.ValidateSetup() 호출하여 팀 색상 등 업데이트
                Instance?.ValidateSetup();
            }

            private void OnNameChanged()
            {
                if (IsPlayerSquadMember()) // Only update persistentId for player squad members
                {
                    this.persistentId = this.customAFName;
                }
                // Trigger overall validation in CombatTestRunner
                Instance?.ValidateSetup();
            }

            private bool IsNameUnique(string value)
            {
                if (Instance == null || string.IsNullOrEmpty(value))
                {
                    return true; // Allow empty during initial setup or if instance is null
                }

                int count = 0;
                // Check against player squad
                if (Instance.playerSquadSetups != null)
                {
                    count += Instance.playerSquadSetups.Count(setup => setup != this && setup.customAFName == value);
                }
                // Check against scenario units
                if (Instance.afSetups != null)
                {
                    count += Instance.afSetups.Count(setup => setup != this && setup.customAFName == value);
                }

                return count == 0; // True if no other setup has the same name
            }

            private bool IsCallsignReadOnly() => !useCustomAssembly;

            private T FindResource<T>(string subfolder, string resourceId) where T : ScriptableObject
            {
                if (string.IsNullOrEmpty(resourceId)) return null;
                string path = $"{subfolder}/{resourceId}";
                return Resources.Load<T>(path);
            }

            private Sprite LoadSpritePreview(string id, string folderName)
            {
                if (string.IsNullOrEmpty(id)) return null;
#if UNITY_EDITOR
                string[] guids = AssetDatabase.FindAssets($"{id} t:sprite", new[] { $"Assets/AF/Sprites/{folderName}" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
                }
                Debug.LogWarning($"Preview sprite not found for ID: {id} in folder: {folderName}");
#endif
                return null; // Return null if not in editor or not found
            }

            // +++ Helper to check if this setup is in the player squad list +++
            // Note: This requires the AFSetup instance to know which list it belongs to,
            // which is tricky. A simpler approach might be needed in StartCombatTestAsync.
            // For now, this ShowIf condition won't work as intended without more complex setup.
            // Consider making persistentId always visible or finding another way.
            private bool IsPlayerSquadMember()
            {
                // Placeholder - this logic needs refinement or removal
                 return Instance != null && Instance.playerSquadSetups.Contains(this);
                 //return true; // Temporarily make it always visible for testing
            }

            private void OnEnable()
            {
                // Update previews when the object is enabled (e.g., added to list)
                 UpdatePreviews();
            }

            private void OnDisable()
            {
                // Optional: Cleanup if needed when removed from list
            }
            #endif
        }

        // +++ Player Squad Setup +++
        [TitleGroup("플레이어 스쿼드 (상태 유지)")]
        [ListDrawerSettings(NumberOfItemsPerPage = 3, Expanded = true)]
        [InfoBox("이 리스트의 유닛들은 전투 후 상태(파츠 내구도 등)가 유지됩니다. 각 항목에 고유한 'Persistent ID'를 설정해야 합니다.")]
        public List<AFSetup> playerSquadSetups = new List<AFSetup>();
        // +++ End Player Squad Setup +++

        [TitleGroup("시나리오 참가자 (일회성)")] // Renamed TitleGroup for clarity
        [ListDrawerSettings(NumberOfItemsPerPage = 5, Expanded = true)]
        [InfoBox("이 리스트의 유닛들은 매 전투마다 새로 생성됩니다 (상태 유지 안됨). 적군 또는 임시 아군 설정에 사용하세요.")]
        public List<AFSetup> afSetups = new List<AFSetup>(); // Kept original name 'afSetups'

        // +++ 시나리오 로드 기능 추가 +++
        [TitleGroup("시나리오 로드", Order = -10)] // 맨 위나 적절한 위치에 표시되도록 Order 조정
        [InfoBox("선택한 시나리오 SO의 유닛 구성을 '시나리오 참가자' 리스트에 로드합니다. 기존 내용은 지워집니다.")]
        public ScenarioSO scenarioToLoad;

        [TitleGroup("시나리오 로드")]
        [Button("선택한 시나리오 로드", ButtonSizes.Large), GUIColor(0.5f, 0.7f, 1f)]
        [EnableIf("scenarioToLoad")] // ScenarioSO가 할당되어 있을 때만 버튼 활성화
        public void LoadScenarioUnits()
        {
            if (scenarioToLoad == null)
            {
                Debug.LogWarning("로드할 ScenarioSO 에셋이 선택되지 않았습니다.");
                return;
            }

            if (assemblyDatabase == null || assemblyDatabase.Count == 0)
            {
                // 데이터베이스가 로드되지 않았을 수 있으니 다시 로드 시도
                LoadAllScriptableObjects();
                if (assemblyDatabase == null || assemblyDatabase.Count == 0)
                {
                    Debug.LogError("Assembly 데이터베이스가 로드되지 않아 시나리오를 로드할 수 없습니다.");
                    return;
                }
            }

            Debug.Log($"Loading units from Scenario: {scenarioToLoad.name}");
            afSetups.Clear(); // 기존 시나리오 참가자 리스트 비우기

            foreach (var unitSetup in scenarioToLoad.units)
            {
                if (string.IsNullOrEmpty(unitSetup.assemblyID))
                {
                    Debug.LogWarning($"Scenario '{scenarioToLoad.name}'의 유닛 '{unitSetup.unitCallsign}'에 AssemblyID가 없습니다. 건너니다.");
                    continue;
                }

                if (assemblyDatabase.TryGetValue(unitSetup.assemblyID, out AssemblySO assemblySO))
                {
                    AFSetup newSetup = new AFSetup
                    {
                        useCustomAssembly = false, // 시나리오 로드는 항상 Assembly 모드
                        assembly = assemblySO,
                        customAFName = unitSetup.unitCallsign, // Callsign을 이름으로 사용
                        teamId = unitSetup.teamID,
                        startPosition = unitSetup.startPosition
                        // persistentId는 시나리오 유닛이므로 설정 안 함
                    };
                    afSetups.Add(newSetup);
                }
                else
                {
                    Debug.LogWarning($"Scenario '{scenarioToLoad.name}'의 유닛 '{unitSetup.unitCallsign}'에 지정된 AssemblyID '{unitSetup.assemblyID}'를 데이터베이스에서 찾을 수 없습니다.");
                }
            }

            // 로드 후 인스펙터 업데이트 및 검증
            ValidateSetup(); // <<< 주석 제거하여 복구 >>>
            #if UNITY_EDITOR
            // 각 항목의 프리뷰 업데이트 (필요 시 - ValidateSetup에서 처리될 수도 있음)
            foreach (var setup in afSetups)
            {
                // Odin Inspector가 리스트 변경 시 자동으로 OnValueChanged 등을 호출하지 않을 수 있으므로,
                // 명시적으로 프리뷰 업데이트를 트리거해야 할 수 있음.
                // 하지만 AFSetup.UpdatePreviews는 private이므로 직접 호출 불가.
                // ValidateSetup() 호출 후 인스펙터가 자동으로 갱신되기를 기대하거나,
                // UpdatePreviews 로직을 public으로 변경하거나 Editor 코드를 사용해야 함.
                // 여기서는 일단 ValidateSetup()에 의존.
            }
             // 씬을 Dirty로 표시하여 변경사항 저장 유도
             UnityEditor.EditorUtility.SetDirty(this);
             Debug.Log($"Scenario '{scenarioToLoad.name}' 로드 완료. 총 {afSetups.Count} 유닛이 추가되었습니다.");
            #endif
        }
        // +++ 시나리오 로드 기능 끝 +++

        [FoldoutGroup("참가자 설정", expanded: true)]
        [PropertySpace(5), HideInPlayMode]
        [ButtonGroup("참가자 설정/ManageButtons")] // Group buttons together
        [Button("플레이어 추가", ButtonSizes.Medium)]
        public void AddPlayerParticipant()
        {
            playerSquadSetups.Add(new AFSetup());
            ValidateSetup(); // Validate after adding
        }

        [PropertySpace(5), HideInPlayMode]
        [ButtonGroup("참가자 설정/ManageButtons")]
        [Button("시나리오 유닛 추가", ButtonSizes.Medium)]
        public void AddScenarioParticipant()
        {
            afSetups.Add(new AFSetup());
            ValidateSetup(); // Validate after adding
        }

        private void ValidateSetup()
        {
            // Validation logic might need updates if player/enemy setups have different rules
            // For now, just update team colors for all lists
            UpdateTeamColors();

            // Check for duplicate persistent IDs in player squad (Important!)
            var persistentIds = new HashSet<string>();
            var duplicatesFound = false;
            foreach (var setup in playerSquadSetups)
            {
                if (string.IsNullOrEmpty(setup.persistentId))
                {
                     Debug.LogError("플레이어 스쿼드 유닛에 Persistent ID가 설정되지 않았습니다!");
                     duplicatesFound = true; // Treat empty ID as an issue
                }
                else if (!persistentIds.Add(setup.persistentId))
                {
                    Debug.LogError($"중복된 Persistent ID '{setup.persistentId}'가 플레이어 스쿼드에 존재합니다!");
                    duplicatesFound = true;
                }
            }
             if (duplicatesFound)
             {
                 // Optionally show an error message in the inspector
                 // Sirenix.OdinInspector.Editor.InspectorUtilities.MarkSceneDirty(); // Mark dirty to show error icon
             }

            // Update Odin validation message visibility
            #if UNITY_EDITOR
            _showDuplicateCallsignError = HasDuplicateCallsigns();
            _showEmptyCallsignError = HasEmptyCallsigns();
            #endif
        }

        private void UpdateTeamColors()
        {
            // Reset colors used in this validation cycle
             _currentTeamColors.Clear();
             int colorIndex = 0;

            // Assign colors to Player Squad
             foreach (var setup in playerSquadSetups)
             {
                 if (!_currentTeamColors.ContainsKey(setup.teamId))
                 {
                     _currentTeamColors.Add(setup.teamId, teamColorPalette[colorIndex % teamColorPalette.Count]);
                     colorIndex++;
                 }
                 setup.teamColorPreview = _currentTeamColors[setup.teamId];
             }

            // Assign colors to Scenario Participants
             foreach (var setup in afSetups)
             {
                 if (!_currentTeamColors.ContainsKey(setup.teamId))
                 {
                     _currentTeamColors.Add(setup.teamId, teamColorPalette[colorIndex % teamColorPalette.Count]);
                     colorIndex++;
                 }
                 setup.teamColorPreview = _currentTeamColors[setup.teamId];
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

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("로그에 스프라이트 아이콘 사용")]
        public bool useSpriteIconsInLog = true;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("디버그 레벨 로그 표시")]
        public bool logDebugMessages = false;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("AI 결정 로그 표시 (Debug)")]
        public bool logAIDecisions = false; // 기본값은 false

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("최대 전투 턴 (Safety Break)")]
        [MinValue(1)]
        public int maxCombatTurns = 50; // Default value

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
        public int currentCycle;
        [FoldoutGroup("현재 전투 상태"), ReadOnly, LabelText("전투 진행 중")]
        public bool isInCombat;

        private void Awake()
        {
            Instance = this;
            LoadAllScriptableObjects();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
             // Call the simpler Log method without snapshot parameters
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

            ValidateSetup(); // Re-validate before starting
            #if UNITY_EDITOR
            if (_showDuplicateCallsignError || _showEmptyCallsignError)
            {
                Log("콜사인 오류가 발견되어 전투를 시작할 수 없습니다. 인스펙터를 확인하세요.", LogLevel.Error);
                EditorUtility.DisplayDialog("콜사인 오류", "콜사인이 중복되었거나 비어있습니다. 인스펙터를 확인하고 수정해주세요.", "확인");
                return;
            }
            #endif

            // --- 서비스 가져오기 ---
            combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
            textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();

            if (combatSimulator == null || textLogger == null)
            {
                Log("필수 서비스(CombatSimulatorService or TextLoggerService)를 찾을 수 없습니다!", LogLevel.Error);
                return;
            }
            
            // --- 로그 포맷 설정 --- 
            textLogger.SetShowLogLevel(showLogLevelPrefix);
            textLogger.SetShowTurnPrefix(showTurnPrefix);
            textLogger.SetUseIndentation(useLogIndentation);
            textLogger.SetLogActionSummaries(logActionSummaries);
            textLogger.SetUseSpriteIcons(useSpriteIconsInLog);
            textLogger.SetLogDebugMessages(logDebugMessages);
            // ---------------------

            // --- 상태 초기화 ---
            textLogger.ConcreteLogger?.Clear(); // TextLogger 직접 초기화
            isInCombat = true; // 전투 시작 플래그 설정
            currentCycle = 0;
            currentBattleId = Guid.NewGuid().ToString().Substring(0, 8); // 고유한 전투 ID 생성

            Log($"전투 시뮬레이션 시작 준비... (ID: {currentBattleId})", LogLevel.System);

            // --- 참가자 구성 ---
            List<ArmoredFrame> allParticipants = new List<ArmoredFrame>();

            // 1. 플레이어 스쿼드 처리 (상태 유지 로직)
            for (int i = 0; i < playerSquadSetups.Count; i++)
            {
                var playerSetup = playerSquadSetups[i];
                ArmoredFrame playerAf = null;

                if (string.IsNullOrEmpty(playerSetup.customAFName))
                {
                    Log($"플레이어 스쿼드 {i+1}: 유닛 이름이 비어있습니다! 이 유닛은 전투에 참여할 수 없습니다.", LogLevel.Error);
                    continue;
                }

                try
                {
                    if (_persistentPlayerFrames.TryGetValue(playerSetup.customAFName, out ArmoredFrame existingAf))
                    {
                        Log($"기존 플레이어 유닛 '{playerSetup.customAFName}' 재사용.", LogLevel.Info);
                        playerAf = existingAf;
                        playerAf.Position = playerSetup.startPosition; 
                    }
                    else
                    {
                        if (playerSetup.useCustomAssembly)
                        {
                            playerAf = CreateCustomArmoredFrame(playerSetup, playerSquadSetups.Count + i + 1);
                        }
                        else
                        {
                            if (playerSetup.assembly == null)
                            {
                                Log($"플레이어 스쿼드 {i+1} ({playerSetup.customAFName}): AssemblySO가 없습니다. 생성 불가.", LogLevel.Warning);
                                continue;
                            }
                            playerAf = CreateTestArmoredFrame(
                                playerSetup.assembly.AssemblyID,
                                playerSetup.teamId,
                                playerSetup.startPosition,
                                playerSetup.customAFName
                            );
                        }

                        if (playerAf != null)
                        {
                            _persistentPlayerFrames.Add(playerSetup.customAFName, playerAf);
                        }
                    }

                    if (playerAf != null)
                    {
                        allParticipants.Add(playerAf);
                    }
                }
                catch (Exception ex)
                {
                    Log($"플레이어 스쿼드 유닛 {i+1} ({playerSetup.customAFName}) 처리 중 오류 발생: {ex.Message}", LogLevel.Error);
                }
            }

            // 2. 시나리오 참가자 처리 (일회성 유닛)
            for (int i = 0; i < afSetups.Count; i++)
            {
                var scenarioSetup = afSetups[i];
                ArmoredFrame scenarioAf = null;
                 try
                 {
                     if (string.IsNullOrEmpty(scenarioSetup.customAFName))
                     {
                         Log($"시나리오 유닛 {i+1}: 유닛 이름이 비어있습니다! 생성 불가.", LogLevel.Warning);
                         continue;
                     }
                     
                     if (scenarioSetup.useCustomAssembly)
                     {
                         scenarioAf = CreateCustomArmoredFrame(scenarioSetup, playerSquadSetups.Count + i + 1);
                     }
                     else
                     {
                         if (scenarioSetup.assembly == null)
                         {
                             Log($"시나리오 유닛 {i+1}: AssemblySO가 없습니다. 생성 불가.", LogLevel.Warning);
                             continue;
                         }
                         scenarioAf = CreateTestArmoredFrame(
                             scenarioSetup.assembly.AssemblyID,
                             scenarioSetup.teamId,
                             scenarioSetup.startPosition,
                             scenarioSetup.customAFName
                         );
                     }

                     if (scenarioAf != null)
                     {
                         allParticipants.Add(scenarioAf);
                     }
                 }
                 catch (Exception ex)
                 {
                     Log($"시나리오 유닛 {i+1} 생성 중 오류 발생: {ex.Message}", LogLevel.Error);
                 }
            }

            // --- 최종 참가자 검증 ---
            if (allParticipants.Count < 2)
            {
                Log("전투에 참여할 유효한 기체가 2기 미만입니다. (플레이어 스쿼드 ID 확인 필요)", LogLevel.Error);
                isInCombat = false; // 전투 상태 초기화
                return;
            }
            // 팀 수 검증 (최소 2팀 필요)
            if (allParticipants.Select(p => p.TeamId).Distinct().Count() < 2)
            {
                 Log("전투 시작 조건 미충족: 최소 2개의 다른 팀이 필요합니다.", LogLevel.Error);
                 isInCombat = false; // 전투 상태 초기화
                 return;
            }

            // --- 스냅샷 생성 로직 추가 ---
            var initialSnapshot = new Dictionary<string, ArmoredFrameSnapshot>();
            foreach (var unit in allParticipants)
            {
                if (unit != null && !string.IsNullOrEmpty(unit.Name))
                {
                    initialSnapshot[unit.Name] = new ArmoredFrameSnapshot(unit);
                }
            }
            // --- 스냅샷 생성 로직 끝 ---

            // --- Log 호출 수정: 스냅샷 추가 ---
            textLogger?.TextLogger?.Log(
                $"총 {allParticipants.Count}기 참가 확정. 전투 시뮬레이션 시작.",
                LogLevel.System,
                LogEventType.CombatStart,
                null,
                false,
                initialSnapshot
            );
            // --- Log 호출 수정 끝 ---

            Log("전투 시뮬레이터 시작...");
            string battleName = $"Test Battle {DateTime.Now:HH:mm:ss}";
            string battleId = combatSimulator.StartCombat(allParticipants.ToArray(), battleName, false);

            try
            {
                bool combatEnded = false;
                while (!combatEnded && isInCombat && combatSimulator.CurrentTurn < maxCombatTurns)
                {
                    combatEnded = !combatSimulator.ProcessNextTurn(); 
                    await UniTask.Yield(PlayerLoopTiming.Update); 
                }

                if (combatSimulator.CurrentTurn >= maxCombatTurns)
                {
                    Log($"안전 브레이크 발동! ({maxCombatTurns} 턴 초과)", LogLevel.Warning);
                    if (isInCombat) combatSimulator.EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Draw);
                }
            }
            catch (Exception ex)
            {
                Log($"전투 시뮬레이션 중 오류 발생: {ex.Message} {ex.StackTrace}", LogLevel.Error);
            }
            finally
            {
                if (combatSimulator != null && combatSimulator.IsInCombat) 
                {
                    combatSimulator.EndCombat();
                }
                isInCombat = false;
                Log("전투 프로세스 정리 완료.", LogLevel.System);

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
            if (isInCombat) return false;

            #if UNITY_EDITOR
            if (HasDuplicateCallsigns() || HasEmptyCallsigns()) return false;
            #endif

            List<AFSetup> allPotentialSetups = new List<AFSetup>(playerSquadSetups);
            allPotentialSetups.AddRange(afSetups);

            if (allPotentialSetups.Count < 2) return false;
            if (allPotentialSetups.Select(p => p.teamId).Distinct().Count() < 2) return false;

            foreach (var setup in allPotentialSetups)
            {
                if (setup.useCustomAssembly)
                {
                     if (setup.customFrame == null || setup.customBody == null) return false;
                }
                else
                {
                     if (setup.assembly == null) return false;
                    }
             }

            return true;
        }

        private ArmoredFrame CreateTestArmoredFrame(string assemblyId, int teamId, Vector3 position, string callsign)
        {
            if (!assemblyDatabase.TryGetValue(assemblyId, out AssemblySO assemblyData))
            {
                Debug.LogError($"CreateTestArmoredFrame Error: AssemblySO with ID '{assemblyId}' not found in database.");
                return null;
            }

            string instanceName = !string.IsNullOrEmpty(callsign) ? callsign : assemblyData.AFName ?? assemblyId;

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

            return af;
        }

        private ArmoredFrame CreateCustomArmoredFrame(AFSetup setup, int participantIndex)
        {
            if (setup.customFrame == null || setup.customBody == null)
            {
                Log($"커스텀 AF 생성 오류 (참가자 {participantIndex}): 필수 파츠(Frame 또는 Body)가 설정되지 않았습니다.", LogLevel.Error);
                return null;
            }

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
            if (frame == null) { return null; }

            string instanceName = string.IsNullOrEmpty(setup.customAFName) ? $"Custom AF {participantIndex}" : setup.customAFName;
            ArmoredFrame af = new ArmoredFrame(instanceName, frame, setup.startPosition, setup.teamId);

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

            AttachCustomPart(af, setup.customHead, frameData.Slot_Head);
            AttachCustomPart(af, setup.customBody, frameData.Slot_Body);
            AttachCustomPart(af, setup.customArms, frameData.Slot_Arm_L);
            AttachCustomPart(af, setup.customArms, frameData.Slot_Arm_R);
            AttachCustomPart(af, setup.customLegs, frameData.Slot_Legs);

            AttachCustomWeapon(af, setup.customWeaponR1);
            AttachCustomWeapon(af, setup.customWeaponL1);

            Log($"--- 커스텀 AF 생성 완료: [{af.Name}] (팀: {af.TeamId}) ---", LogLevel.System);

            return af;
        }

        private void AttachCustomPart(ArmoredFrame af, PartSO partSO, string slotId)
        {
            if (partSO == null || string.IsNullOrEmpty(slotId)) { return; }

            Stats partStats = new Stats(
                partSO.Stat_AttackPower, partSO.Stat_Defense, partSO.Stat_Speed, partSO.Stat_Accuracy,
                partSO.Stat_Evasion, partSO.Stat_Durability, partSO.Stat_EnergyEff, partSO.Stat_MaxAP, partSO.Stat_APRecovery
            );
            Part runtimePart = null;
            try
            {
                switch (partSO.PartType)
                {
                    case PartType.Head: runtimePart = new HeadPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    case PartType.Body: runtimePart = new AF.Models.BodyPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    case PartType.Arm: runtimePart = new ArmsPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    case PartType.Legs: runtimePart = new LegsPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break;
                    default: Debug.LogWarning($"Unhandled PartType '{partSO.PartType}' for PartSO '{partSO.name}'."); break;
                }

                if (runtimePart != null)
                {
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

        private void AttachCustomWeapon(ArmoredFrame af, WeaponSO weaponSO)
        {
            if (weaponSO == null) { return; }

            try {
                 RangeData rangeData;
                 if (weaponSO.Range > 0)
                 {
                     rangeData = new RangeData(weaponSO.Range, weaponSO.Range * 0.7f);
                 }
                 else
                 {
                     rangeData = RangeData.Default;
                 }

                 if (rangeData.OptimalRange <= 0.1f && rangeData.MaxRange > 0) 
                 {
                     rangeData.OptimalRange = rangeData.MaxRange * 0.7f;
                     Debug.LogWarning($"Custom Weapon '{weaponSO.WeaponName}' had invalid OptimalRange ({weaponSO.Range}). Estimating ({rangeData.OptimalRange:F1}) based on MaxRange ({rangeData.MaxRange:F1}) using 70% rule.");
                 }

                 Weapon runtimeWeapon = new Weapon(
                     weaponSO.WeaponName ?? weaponSO.name,
                     weaponSO.WeaponType, 
                     weaponSO.DamageType,
                     weaponSO.BaseDamage, 
                     weaponSO.Accuracy, 
                     rangeData,
                     weaponSO.AttackSpeed, 
                     weaponSO.OverheatPerShot,
                     weaponSO.BaseAPCost,
                     weaponSO.AmmoCapacity,
                     weaponSO.ReloadAPCost,
                     weaponSO.ReloadTurns,
                     weaponSO.AttackFlavorKey,
                     weaponSO.ReloadFlavorKey 
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
                        case PartType.Body: runtimePart = new AF.Models.BodyPart(partSO.PartName, partStats, partSO.MaxDurability, partSO.PartWeight); break;
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
                     RangeData rangeData;
                     if (weaponSO.Range > 0)
                     {
                         rangeData = new RangeData(weaponSO.Range, weaponSO.Range * 0.7f);
                     }
                     else
                     {
                         rangeData = RangeData.Default;
                     }

                     if (rangeData.OptimalRange <= 0.1f && rangeData.MaxRange > 0) 
                     {
                         rangeData.OptimalRange = rangeData.MaxRange * 0.7f;
                         Debug.LogWarning($"Custom Weapon '{weaponSO.WeaponName}' had invalid OptimalRange ({weaponSO.Range}). Estimating ({rangeData.OptimalRange:F1}) based on MaxRange ({rangeData.MaxRange:F1}) using 70% rule.");
                     }

                     Weapon runtimeWeapon = new Weapon(
                         weaponSO.WeaponName ?? weaponSO.name,
                         weaponSO.WeaponType, 
                         weaponSO.DamageType,
                         weaponSO.BaseDamage, 
                         weaponSO.Accuracy, 
                         rangeData,
                         weaponSO.AttackSpeed, 
                         weaponSO.OverheatPerShot,
                         weaponSO.BaseAPCost,
                         weaponSO.AmmoCapacity,
                         weaponSO.ReloadAPCost,
                         weaponSO.ReloadTurns,
                         weaponSO.AttackFlavorKey,
                         weaponSO.ReloadFlavorKey
                     );
                    af.AttachWeapon(runtimeWeapon);
                } catch (Exception e) { Debug.LogError($"Weapon instantiation/attach error for {weaponId}: {e.Message}"); }
            }
            else { Debug.LogWarning($"WeaponSO ID '{weaponId}' not found in database."); }
        }

        private static IEnumerable<ValueDropdownItem<FrameSO>> GetFrameValues()
        {
            return Resources.LoadAll<FrameSO>("Frames")
                .Select(f => new ValueDropdownItem<FrameSO>(f.FrameName ?? f.name, f));
        }

        private static IEnumerable<ValueDropdownItem<PartSO>> GetPartValuesFiltered(PartType type)
        {
             return Resources.LoadAll<PartSO>("Parts")
                 .Where(p => p != null && p.PartType == type)
                 .Select(p => new ValueDropdownItem<PartSO>($"{p.PartName ?? p.name} ({p.PartType})", p));
        }

        private static IEnumerable<ValueDropdownItem<PartSO>> GetHeadPartValues() => GetPartValuesFiltered(PartType.Head);
        private static IEnumerable<ValueDropdownItem<PartSO>> GetBodyPartValues() => GetPartValuesFiltered(PartType.Body);
        private static IEnumerable<ValueDropdownItem<PartSO>> GetArmsPartValues() => GetPartValuesFiltered(PartType.Arm);
        private static IEnumerable<ValueDropdownItem<PartSO>> GetLegsPartValues() => GetPartValuesFiltered(PartType.Legs);

        private static IEnumerable<ValueDropdownItem<WeaponSO>> GetWeaponValues()
        {
            return Resources.LoadAll<WeaponSO>("Weapons")
                .Select(w => new ValueDropdownItem<WeaponSO>(w.WeaponName ?? w.name, w));
        }

        private static IEnumerable<ValueDropdownItem<PilotSO>> GetPilotValues()
        {
            return Resources.LoadAll<PilotSO>("Pilots")
                .Select(p => new ValueDropdownItem<PilotSO>(p.PilotName ?? p.name, p));
        }

        [TitleGroup("플레이어 스쿼드 (상태 유지)")]
        [Button("플레이어 상태 리셋", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
        [PropertySpace(10)]
        public void ResetPlayerSquadState()
        {
            if (Application.isPlaying)
            {
                 _persistentPlayerFrames.Clear();
                 Debug.Log("플레이어 스쿼드 상태가 리셋되었습니다.");
            }
            else
            {
                 Debug.LogWarning("플레이어 상태 리셋은 플레이 모드에서만 가능합니다.");
            }
        }

        #if UNITY_EDITOR
        [ShowIf("HasDuplicateCallsigns")]
        [PropertyOrder(-5)]
        [InfoBox("유닛 이름이 중복되었습니다! 각 유닛의 유닛 이름은 고유해야 합니다.", InfoMessageType.Error)]
        private bool _showDuplicateCallsignError;

        private bool HasDuplicateCallsigns()
        {
            if (playerSquadSetups == null && afSetups == null) return false;

            var allCallsigns = new List<string>();
            if (playerSquadSetups != null) allCallsigns.AddRange(playerSquadSetups.Select(s => s.customAFName));
            if (afSetups != null) allCallsigns.AddRange(afSetups.Select(s => s.customAFName));
            
            var validCallsigns = allCallsigns.Where(cs => !string.IsNullOrEmpty(cs)).ToList();

            return validCallsigns.Count != validCallsigns.Distinct().Count();
        }

        [ShowIf("HasEmptyCallsigns")]
        [PropertyOrder(-4)]
        [InfoBox("비어있는 유닛 이름이 있습니다! 모든 유닛은 유닛 이름을 가져야 합니다.", InfoMessageType.Error)]
        private bool _showEmptyCallsignError;

        private bool HasEmptyCallsigns()
        {
             if (playerSquadSetups == null && afSetups == null) return false;
             
             bool playerEmpty = playerSquadSetups?.Any(s => string.IsNullOrEmpty(s.customAFName)) ?? false;
             bool scenarioEmpty = afSetups?.Any(s => string.IsNullOrEmpty(s.customAFName)) ?? false;
             
             return playerEmpty || scenarioEmpty;
        }
        #endif
    }
} 
