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
using AF.AI.BehaviorTree;
using AF.AI.BehaviorTree.PilotBTs;
using System.IO;

#if UNITY_EDITOR
using UnityEditor; // Needed for AssetDatabase
#endif
using UnityEngine.UI; // SASHA: UI.Button 사용을 위해 추가

namespace AF.Tests
{
    /// <summary>
    /// 전투 시스템 테스트를 위한 시나리오 실행 및 데이터 생성 클래스
    /// </summary>
    public class CombatTestRunner : MonoBehaviour, IService
    {
        public static CombatTestRunner Instance { get; private set; } // 싱글톤 인스턴스 복원

        // --- Re-add Color Palette for Team IDs ---
        private static readonly List<Color> teamColorPalette = new List<Color>
        {
            new Color(0.3f, 0.8f, 0.9f), // Cyan (플레이어 스쿼드 - 밝고 선명하게)
            new Color(0.9f, 0.2f, 0.2f), // Red (외부 세력 1 - 강렬한 적대색)
            new Color(0.9f, 0.5f, 0.2f), // Orange (외부 세력 2 - 적대 또는 경계)
            new Color(0.9f, 0.8f, 0.2f), // Yellow (외부 세력 3 - 중립 또는 주의)
            new Color(0.8f, 0.3f, 0.9f), // Magenta (외부 세력 4 - 특수 또는 기타)
            new Color(0.2f, 0.4f, 0.9f), // Blue (외부 세력 5 - 또 다른 아군 계열 또는 차분한 세력)
            new Color(1.0f, 1.0f, 1.0f), // White (외부 세력 6 - 중요 타겟 또는 중립 고가치)
            new Color(0.9f, 0.6f, 0.7f), // Pink (외부 세력 7 - 독특한 개체)
            new Color(0.6f, 0.3f, 0.9f), // Purple (외부 세력 8 - 위협적인 세력)
            new Color(0.7f, 0.7f, 0.7f), // Light Grey (외부 세력 9 - 일반 중립 또는 덜 위협적)
            new Color(0.4f, 0.4f, 0.4f)  // Dark Grey (외부 세력 10 - 배경 요소 또는 매우 낮은 위협)
        };
        private Dictionary<int, Color> _currentTeamColors = new Dictionary<int, Color>();
        // --- End Re-add Color Palette ---

        // +++ Player Squad State Storage +++
        private Dictionary<string, ArmoredFrame> _persistentPlayerFrames = new Dictionary<string, ArmoredFrame>();
        // +++ End Player Squad State Storage +++

        // +++ SASHA: 팀 색상 조회 메서드 추가 +++
        /// <summary>
        /// 지정된 팀 ID에 해당하는 색상을 반환합니다.
        /// </summary>
        /// <param name="teamId">조회할 팀 ID</param>
        /// <param name="color">찾은 색상 (출력 파라미터)</param>
        /// <returns>색상을 찾았으면 true, 아니면 false</returns>
        public bool TryGetTeamColor(int teamId, out Color color)
        {
            bool found = false;
            if (_currentTeamColors != null && _currentTeamColors.TryGetValue(teamId, out color))
            {
                found = true;
            }
            else
            {
                 color = Color.white; // 기본값 설정
            }
            // +++ 확인용 로그 추가 +++
            // Debug.Log($"TryGetTeamColor for TeamID: {teamId}. Found: {found}. Dict count: {_currentTeamColors?.Count ?? -1}");
            // +++ 확인용 로그 끝 +++
            return found;
        }
        // +++ SASHA: 추가 끝 +++

        // +++ SASHA: 플레이어 스쿼드 유닛 이름 목록 반환 메서드 추가 +++
        /// <summary>
        /// 현재 설정된 플레이어 스쿼드 유닛들의 Persistent ID 목록을 반환합니다.
        /// </summary>
        /// <returns>플레이어 스쿼드 유닛 이름(Persistent ID)의 HashSet</returns>
        public HashSet<string> GetPlayerSquadUnitNames()
        {
            // playerSquadSetups 리스트에서 null이 아니고 callsign이 비어있지 않은 것만 필터링하여 HashSet으로 반환
            return new HashSet<string>(playerSquadSetups
                .Where(setup => setup != null && !string.IsNullOrEmpty(setup.callsign))
                .Select(setup => setup.callsign));
        }
        // +++ SASHA: 추가 끝 +++

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
            [EnableIf("useCustomAssembly")]
            [LabelText("코드 네임")]
            public string customAFName;

            [VerticalGroup("TopConfig/Left")]
            [EnableIf("@!useCustomAssembly")]
            [OnValueChanged("UpdatePreviews")]
            [PreviewField(50, ObjectFieldAlignment.Left), HideLabel]
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

            // +++ Add Callsign Field +++
            [FoldoutGroup("식별자 (상태 유지용)")]
            [Tooltip("플레이어 스쿼드 유닛의 상태를 추적하고 식별하기 위한 고유한 콜사인입니다. 중복되지 않게 설정해주세요 (한/영 무관).")]
            // [ShowIf("IsPlayerSquadMember")] // ShowIf 조건은 잠시 보류 (CombatTestRunner 인스턴스 참조 문제)
            [LabelText("콜사인 (Callsign)")] // 레이블 명확화
            public string callsign;
            // --- persistentId 필드 제거 ---
            // [FoldoutGroup("식별자 (상태 유지용)")]
            // [Tooltip("플레이어 스쿼드 유닛의 상태를 추적하기 위한 고유 ID입니다. 커스텀 조립 시 직접 입력하고, 어셈블리 사용 시 어셈블리 이름으로 자동 설정됩니다.")]
            // [ShowIf("IsPlayerSquadMember")] // AFSetup 인스턴스가 playerSquadSetups 리스트에 속할 때만 표시 (구현 필요)
            // public string persistentId;
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

                // +++ Update Persistent ID based on mode +++
                if (useCustomAssembly)
                {
                    // Update previews based on custom SO fields
                    customFramePreview = LoadSpritePreview(customFrame?.FrameID, "Frames");
                    customPilotPreview = LoadSpritePreview(customPilot?.PilotID, "Pilots");
                    customHeadPreview = LoadSpritePreview(customHead?.PartID, "Parts");
                    customBodyPreview = LoadSpritePreview(customBody?.PartID, "Parts");
                    customArmsPreview = LoadSpritePreview(customArms?.PartID, "Parts");
                    customLegsPreview = LoadSpritePreview(customLegs?.PartID, "Parts");
                    customWeaponR1Preview = LoadSpritePreview(customWeaponR1?.WeaponID, "Weapons");
                    customWeaponL1Preview = LoadSpritePreview(customWeaponL1?.WeaponID, "Weapons");

                    // persistentId는 커스텀 이름과 연동하거나 수동 입력 유지
                    // 여기서는 예시로 customAFName을 사용 (필요시 수정)
                    // if (!string.IsNullOrEmpty(customAFName))
                    // {
                    //     // persistentId = customAFName; // persistentId 관련 로직 제거
                    // }
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

                        // Set persistentId from assembly name - 제거
                        // persistentId = assembly.name;
                    }
                    else // Assembly is null in Assembly Mode
                    {
                        // Clear the custom fields and persistent ID - persistentId 부분 제거
                        // ... (필드 클리어 로직) ...
                        // persistentId = null; // Clear persistent ID
                    }
                }
                // ServiceLocator를 통해 CombatTestRunner 서비스 인스턴스를 가져와 ValidateSetup 호출
                // var runnerService = AF.Services.ServiceLocator.Instance?.GetService<AF.Tests.CombatTestRunner>();
                // runnerService?.ValidateSetup();
                Instance?.ValidateSetup(); // 싱글톤 인스턴스 사용으로 변경
            }

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
                 // ServiceLocator를 통해 CombatTestRunner 서비스를 가져와서 확인하는 방식으로 변경 필요
                 // 또는 이 메서드 자체의 로직을 재고해야 함. 임시로 true 반환.
                 // return true;
                 return Instance != null && Instance.playerSquadSetups.Contains(this); // 싱글톤 인스턴스 사용으로 변경
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
        [InfoBox("이 리스트의 유닛들은 전투 후 상태(파츠 내구도 등)가 유지됩니다. 각 항목에 고유한 '콜사인'을 설정해야 합니다.")]
        public List<AFSetup> playerSquadSetups = new List<AFSetup>();
        // +++ End Player Squad Setup +++

        [TitleGroup("시나리오 참가자 (일회성)")] // Renamed TitleGroup for clarity
        [ListDrawerSettings(NumberOfItemsPerPage = 5, Expanded = true)]
        [InfoBox("이 리스트의 유닛들은 매 전투마다 새로 생성됩니다 (상태 유지 안됨). 적군 또는 임시 아군 설정에 사용하세요.")]
        public List<AFSetup> afSetups = new List<AFSetup>(); // Kept original name 'afSetups'

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

            // Check for duplicate callsigns in player squad (Important!)
            var callsigns = new HashSet<string>();
            var duplicatesFound = false;
            foreach (var setup in playerSquadSetups)
            {
                if (string.IsNullOrEmpty(setup.callsign))
                {
                     Debug.LogError("플레이어 스쿼드 유닛에 콜사인이 설정되지 않았습니다!");
                     duplicatesFound = true; // Treat empty callsign as an issue
                }
                else if (!callsigns.Add(setup.callsign))
                {
                    Debug.LogError($"중복된 콜사인 '{setup.callsign}'가 플레이어 스쿼드에 존재합니다!");
                    duplicatesFound = true;
                }
            }
             if (duplicatesFound)
             {
                 // Optionally show an error message in the inspector
                 // Sirenix.OdinInspector.Editor.InspectorUtilities.MarkSceneDirty(); // Mark dirty to show error icon
             }
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
        [LabelText("로그 필터링 (제외)")]
        public LogLevelFlags logLevelsToRecord = LogLevelFlags.Nothing;

        [FoldoutGroup("전투 옵션", expanded: true)]
        [LabelText("안전 브레이크 턴 수")]
        [Tooltip("전투가 이 턴 수를 초과하면 안전을 위해 자동으로 무승부 처리됩니다.")]
        [Range(5, 100)] 
        public int safetyBreakTurns = 10;

        [FoldoutGroup("전투 옵션", expanded: true)] // SASHA: 전투 시작 버튼 필드 추가
        [SerializeField] private Button combatStartButton; 

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

        private bool _isInitialized = false;

        // Awake 대신 Initialize 사용
        public void Initialize()
        {
            if (_isInitialized) return;

            Instance = this; // 싱글톤 할당 복원
            LoadAllScriptableObjects();
            _isInitialized = true;
            Debug.Log("CombatTestRunner Initialized.");
        }

        // OnDestroy 대신 Shutdown 사용
        public void Shutdown()
        {
            if (!_isInitialized) return;

            if (Instance == this) Instance = null; // 싱글톤 해제 복원
            _isInitialized = false;
            Debug.Log("CombatTestRunner Shutdown.");
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
            Debug.Log("StartCombatTestAsync: 메서드 진입."); // +++ SASHA: 메서드 진입 로그 추가 +++
            // +++ SASHA: 최상위 예외 로깅 추가 +++
            try
            {
                if (isInCombat)
                {
                    Log("이미 전투가 진행 중입니다.", LogLevel.Warning);
                    return;
                }

                if (combatStartButton != null && combatStartButton.interactable)
                {
                    combatStartButton.interactable = false;
                }

                ValidateSetup();

                combatSimulator = ServiceLocator.Instance.GetService<ICombatSimulatorService>();
                textLogger = ServiceLocator.Instance.GetService<TextLoggerService>();

                if (combatSimulator == null || textLogger == null)
                {
                    Log("필수 서비스(CombatSimulatorService or TextLoggerService)를 찾을 수 없습니다!", LogLevel.Error);
                    if (combatStartButton != null) combatStartButton.interactable = true;
                    return;
                }
            
                textLogger.SetShowLogLevel(showLogLevelPrefix);
                textLogger.SetShowTurnPrefix(showTurnPrefix);
                textLogger.SetUseIndentation(useLogIndentation);
                textLogger.SetLogActionSummaries(logActionSummaries);
                textLogger.SetUseSpriteIcons(useSpriteIconsInLog);
                LogLevelFlags flagsToActuallyAllow = LogLevelFlags.Everything & ~logLevelsToRecord;
                textLogger.SetAllowedLogLevels(flagsToActuallyAllow);
                Debug.Log($"TextLoggerService.AllowedLogLevels set to: {flagsToActuallyAllow}"); // +++ SASHA: 로그 추가 +++

                textLogger.ConcreteLogger?.Clear();
                isInCombat = true;
                currentCycle = 0;
                currentBattleId = Guid.NewGuid().ToString().Substring(0, 8);

                Log($"전투 시뮬레이션 시작 준비... (ID: {currentBattleId})", LogLevel.System);

                List<ArmoredFrame> allParticipants = new List<ArmoredFrame>();
                for (int i = 0; i < playerSquadSetups.Count; i++)
                {
                    var playerSetup = playerSquadSetups[i];
                    ArmoredFrame playerAf = null;
                    if (string.IsNullOrEmpty(playerSetup.callsign))
                    {
                        Log($"플레이어 스쿼드 {i + 1}: 콜사인이 비어있습니다! 이 유닛은 전투에 참여할 수 없습니다.", LogLevel.Error);
                        continue;
                    }
                    try
                    {
                        if (_persistentPlayerFrames.TryGetValue(playerSetup.callsign, out ArmoredFrame existingAf))
                        {
                            Log($"기존 플레이어 유닛 '{playerSetup.callsign}' 재사용.", LogLevel.Info);
                            playerAf = existingAf;
                            playerAf.SetPosition(playerSetup.startPosition);
                            playerAf.AICtxBlackboard.ClearAllData();
                        }
                        else
                        {
                            if (playerSetup.useCustomAssembly)
                            {
                                playerAf = CreateCustomArmoredFrame(playerSetup, i + 1);
                            }
                            else
                            {
                                if (playerSetup.assembly == null)
                                {
                                    Log($"플레이어 스쿼드 {i + 1} ({playerSetup.callsign}): AssemblySO가 없습니다. 생성 불가.", LogLevel.Warning);
                                    continue;
                                }
                                playerAf = CreateTestArmoredFrame(playerSetup.callsign, playerSetup.assembly.name, playerSetup.teamId, playerSetup.startPosition);
                            }
                            if (playerAf != null)
                            {
                                _persistentPlayerFrames.Add(playerSetup.callsign, playerAf);
                            }
                        }
                        if (playerAf != null)
                        {
                            allParticipants.Add(playerAf);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"플레이어 스쿼드 유닛 {i + 1} ({playerSetup.callsign}) 처리 중 오류 발생: {ex.Message}", LogLevel.Error);
                    }
                }
                for (int i = 0; i < afSetups.Count; i++)
                {
                    var scenarioSetup = afSetups[i];
                    ArmoredFrame scenarioAf = null;
                    try
                    {
                        if (scenarioSetup.useCustomAssembly)
                        {
                            scenarioAf = CreateCustomArmoredFrame(scenarioSetup, playerSquadSetups.Count + i + 1);
                        }
                        else
                        {
                            if (scenarioSetup.assembly == null)
                            {
                                Log($"시나리오 유닛 {i + 1}: AssemblySO가 없습니다. 생성 불가.", LogLevel.Warning);
                                continue;
                            }
                            scenarioAf = CreateTestArmoredFrame(scenarioSetup.callsign, scenarioSetup.assembly.name, scenarioSetup.teamId, scenarioSetup.startPosition);
                        }
                        if (scenarioAf != null)
                        {
                            allParticipants.Add(scenarioAf);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"시나리오 유닛 {i + 1} 생성 중 오류 발생: {ex.Message}", LogLevel.Error);
                    }
                }

                Debug.Log($"StartCombatTestAsync: 모든 참가자 생성 시도 완료. 총 {allParticipants.Count}명."); // +++ SASHA: 로그 추가 +++

                if (allParticipants.Count < 2)
                {
                    Log("전투에 참여할 유효한 기체가 2기 미만입니다. (플레이어 스쿼드 ID 확인 필요)", LogLevel.Error);
                    Debug.Log("StartCombatTestAsync: 참가자 수 부족으로 종료 (allParticipants.Count < 2)."); // +++ SASHA: 로그 추가 +++
                    isInCombat = false;
                    if (combatStartButton != null) combatStartButton.interactable = true;
                    return;
                }

                Debug.Log("StartCombatTestAsync: 참가자 수 검사 통과."); // +++ SASHA: 로그 추가 +++

                if (allParticipants.Select(p => p.TeamId).Distinct().Count() < 2)
                {
                    Log("전투 시작 조건 미충족: 최소 2개의 다른 팀이 필요합니다.", LogLevel.Error);
                    Debug.Log("StartCombatTestAsync: 팀 수 부족으로 종료 (Distinct().Count() < 2)."); // +++ SASHA: 로그 추가 +++
                    isInCombat = false;
                    if (combatStartButton != null) combatStartButton.interactable = true;
                    return;
                }

                Debug.Log("StartCombatTestAsync: 팀 수 검사 통과. 초기 스냅샷 생성 직전."); // +++ SASHA: 로그 추가 +++

                var initialSnapshot = new Dictionary<string, ArmoredFrameSnapshot>();
                foreach (var unit in allParticipants)
                {
                    if (unit != null && !string.IsNullOrEmpty(unit.Name))
                    {
                        initialSnapshot[unit.Name] = new ArmoredFrameSnapshot(unit);
                    }
                }
                textLogger?.TextLogger?.Log(
                    $"총 {allParticipants.Count}기 참가 확정. 전투 시뮬레이션 시작.",
                    LogLevel.System,
                    LogEventType.CombatStart,
                    null,
                    false,
                    initialSnapshot
                );

                Log("전투 시뮬레이터 시작...");
                string battleName = $"Test Battle {DateTime.Now:HH:mm:ss}";
                string battleId = combatSimulator.StartCombat(allParticipants.ToArray(), battleName, false);

                // +++ SASHA: 기존 try-catch-finally를 내부 try로 변경 +++
                try // 기존 try 블록 시작
                {
                    bool combatEnded = false;
                    int currentSafetyBreak = this.safetyBreakTurns;

                    while (!combatEnded && isInCombat && combatSimulator.CurrentTurn < currentSafetyBreak)
                    {
                        combatEnded = !combatSimulator.ProcessNextTurn();
                        await UniTask.Yield(PlayerLoopTiming.Update); // 이 부분에서 멈출 가능성
                    }

                    if (combatSimulator.CurrentTurn >= currentSafetyBreak)
                    {
                        Log($"안전 브레이크 발동! ({currentSafetyBreak} 턴 초과)", LogLevel.Warning);
                        if (isInCombat) combatSimulator.EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Draw);
                    }
                }
                catch (Exception ex) // 기존 catch 블록
                {
                    Log($"전투 시뮬레이션 중 오류 발생: {ex.Message} {ex.StackTrace}", LogLevel.Error);
                }
                finally // 기존 finally 블록
                {
                    if (combatSimulator != null && combatSimulator.IsInCombat)
                    {
                        combatSimulator.EndCombat();
                    }
                    isInCombat = false;
                    Log("전투 프로세스 정리 완료. 이제 로그 저장 시도.", LogLevel.System); // SASHA: 로그 메시지 수정
                    Debug.Log("StartCombatTestAsync: In finally, about to call SaveFilteredLogToFile."); // +++ SASHA: 로그 추가 +++

                    SaveFilteredLogToFile($"BattleLog_{currentBattleId}", logLevelsToRecord);

                    Debug.Log("StartCombatTestAsync: In finally, SaveFilteredLogToFile call completed."); // +++ SASHA: 로그 추가 +++
                    if (combatStartButton != null) combatStartButton.interactable = true;
                }
            }
            catch (Exception ex) // +++ SASHA: 최상위 catch 블록 +++
            {
                Debug.LogError($"StartCombatTestAsync 최상위 예외 발생: {ex.Message}\n{ex.StackTrace}");
                if (combatStartButton != null) combatStartButton.interactable = true;
                isInCombat = false;
            }
        }
        
        [HorizontalGroup("전투 제어/Buttons")] // SASHA: UI 버튼 연동용 메서드 추가
        [Button("Start via UI Button", ButtonSizes.Large), GUIColor(0.3f, 0.7f, 0.8f), EnableIf("CanStartCombat")]
        public void TriggerCombatTestFromUIButton()
        {
            if (CanStartCombat())
            {
                Log("UI 버튼을 통해 전투 테스트 시작...", LogLevel.Info);
                if (combatStartButton != null) // SASHA: 버튼 비활성화
                {
                    combatStartButton.interactable = false;
                }
                StartCombatTestAsync().Forget(); // StartCombatTestAsync의 finally에서 버튼 다시 활성화
            }
            else
            {
                Log("UI 버튼을 통한 전투 시작 실패: CanStartCombat() 조건 미충족.", LogLevel.Warning);
                // SASHA: 시작 실패 시 버튼 다시 활성화 (이미 비활성화 상태일 수 있으므로)
                if (combatStartButton != null && !combatStartButton.interactable)
                {
                    combatStartButton.interactable = true;
                }
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
            // 전투 중이면 비활성화
            if (isInCombat) return false;

            // 1. 모든 잠재적 참가자 목록 생성
            List<AFSetup> allPotentialSetups = new List<AFSetup>(playerSquadSetups);
            allPotentialSetups.AddRange(afSetups);

            // 2. 총 참가자 수 확인 (최소 2명)
            if (allPotentialSetups.Count < 2)
            {
                // Log("CanStartCombat: 총 참가자 수가 2명 미만입니다.", LogLevel.Debug); // 디버그용 로그 (선택적)
                return false;
            }

            // 3. 고유 팀 ID 수 확인 (최소 2팀)
            if (allPotentialSetups.Select(p => p.teamId).Distinct().Count() < 2)
            {
                // Log("CanStartCombat: 고유 팀 ID 수가 2개 미만입니다.", LogLevel.Debug);
                return false;
            }

            // 4. 각 참가자 설정 유효성 검증 (플레이어/시나리오 모두 확인)
            foreach (var setup in allPotentialSetups)
            {
                if (setup.useCustomAssembly)
                {
                    // 커스텀 모드: 필수 파츠(Frame, Body) 확인
                    if (setup.customFrame == null || setup.customBody == null)
                    {
                        // Log($"CanStartCombat: 커스텀 설정 오류 (Team ID: {setup.teamId}) - 필수 파츠(Frame, Body) 누락.", LogLevel.Debug);
                        return false; 
                    }
                     // 플레이어 스쿼드인 경우 Persistent ID 확인
                     if (playerSquadSetups.Contains(setup) && string.IsNullOrEmpty(setup.callsign))
                     {
                         // Log($"CanStartCombat: 플레이어 스쿼드 유닛에 Persistent ID가 없습니다 (Team ID: {setup.teamId}).", LogLevel.Debug);
                         return false;
                     }
                }
                else
                {
                    // 어셈블리 모드: Assembly 확인
                    if (setup.assembly == null) 
                    {
                         // Log($"CanStartCombat: 어셈블리 모드 설정 오류 (Team ID: {setup.teamId}) - Assembly 미설정.", LogLevel.Debug);
                         return false;
                    }
                     // 플레이어 스쿼드인 경우 Persistent ID 확인 (어셈블리 이름 기반)
                     if (playerSquadSetups.Contains(setup) && string.IsNullOrEmpty(setup.callsign))
                     {
                         // UpdatePreviews에서 assembly.name으로 자동 설정되므로, assembly가 null이 아니면 보통 ID는 있음.
                         // 하지만 만약을 대비해 체크하거나, StartCombatTestAsync에서 ID 누락 시 오류 처리 강화.
                         // Log($"CanStartCombat: 플레이어 스쿼드 유닛(어셈블리 모드)에 Persistent ID가 없습니다 (Team ID: {setup.teamId}).", LogLevel.Debug);
                         // return false; // 여기서는 일단 통과시키고 시작 시점에서 처리할 수도 있음
                     }
                }
            }
             // 중복 Persistent ID 검사는 ValidateSetup에서 이미 수행하므로 여기서는 생략 가능

            return true; // 모든 검증 통과
        }

        // CreateTestArmoredFrame 시그니처 변경: callsignForName 파라미터 추가
        private ArmoredFrame CreateTestArmoredFrame(string callsignForName, string assemblyId, int teamId, Vector3 position)
        {
            if (!assemblyDatabase.TryGetValue(assemblyId, out AssemblySO assemblyData))
            {
                Debug.LogError($"CreateTestArmoredFrame Error: AssemblySO with ID '{assemblyId}' not found in database.");
                return null;
            }

            // ArmoredFrame 인스턴스 이름 결정:
            // 플레이어 스쿼드 유닛의 경우, callsignForName을 우선 사용한다.
            // 이것이 비어있거나 null이면 (예: 시나리오 유닛 호출 시), AssemblySO의 AFName 또는 assemblyId를 사용한다.
            string instanceName = callsignForName; // 기본적으로 콜사인을 이름으로 사용 시도
            if (string.IsNullOrEmpty(instanceName))
            {
                instanceName = assemblyData.AFName; // 콜사인 없으면 AssemblySO 이름 사용
                if (string.IsNullOrEmpty(instanceName))
                {   
                    instanceName = assemblyId; // AssemblySO 이름도 없으면 assemblyId 사용
                    Debug.LogWarning($"AssemblySO '{assemblyId}' has no specified AFName. Using Assembly ID as instance name.");
                }
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

            // ArmoredFrame 생성 시 위에서 결정된 instanceName 사용
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

            // +++ BT 할당 및 블랙보드 초기화 +++
            if (af != null)
            {
                af.BehaviorTreeRoot = BasicAttackBT.Create(); 
                af.AICtxBlackboard.ClearAllData(); 

                // +++ SASHA: 생성된 AF 상태 로깅 추가 +++
                string bodySlotId = frameData.Slot_Body; // Body 슬롯 ID 가져오기 (frameData 사용)
                Part bodyPart = af.GetPart(bodySlotId);
                Log($"테스트 AF 생성 (Assembly 기반): [{af.Name}], Pilot: [{af.Pilot?.Name ?? "N/A"} ({af.Pilot?.Specialization.ToString() ?? "N/A"})], IsOperational: {af.IsOperational}, " + // SASHA: PilotName -> Name, 파일럿 정보 로깅 추가
                    $"BodyPartExists: {bodyPart != null}, BodyPartOperational: {bodyPart?.IsOperational}, " +
                    $"TeamID: {af.TeamId}", LogLevel.Debug);
                // +++ SASHA: 로깅 추가 끝 +++
            }
            // +++ BT 할당 및 블랙보드 초기화 끝 +++

            Log($"--- 커스텀 AF 생성 완료: [{af.Name}] (팀: {af.TeamId}) ---", LogLevel.System); // SASHA: 커스텀 -> 테스트 AF, Pilot 정보 추가

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

            // 2. ArmoredFrame 기본 인스턴스 생성:
            // 플레이어 스쿼드 유닛이므로, setup.callsign을 Name으로 사용한다.
            // callsign이 비어있는 경우는 StartCombatTestAsync에서 이미 걸러졌을 것으로 가정한다.
            string instanceName = setup.callsign;
            if (string.IsNullOrEmpty(instanceName)) 
            { 
                // 만약을 대비한 폴백 (StartCombatTestAsync에서 걸러지지 않은 경우)
                instanceName = string.IsNullOrEmpty(setup.customAFName) 
                                ? $"Custom AF {participantIndex}" 
                                : setup.customAFName;
                Log($"경고: Callsign이 비어있는 플레이어 유닛(참가자 {participantIndex})이 생성됩니다. 폴백 이름 '{instanceName}' 사용.", LogLevel.Warning);
            }
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
                Pilot pilot = new Pilot(pilotData.PilotName, pilotBaseStats, pilotData.Specialization);
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

            // +++ BT 할당 및 블랙보드 초기화 +++
            if (af != null)
            {
                af.BehaviorTreeRoot = BasicAttackBT.Create(); 
                af.AICtxBlackboard.ClearAllData(); 

                // +++ SASHA: 생성된 AF 상태 로깅 추가 +++
                string bodySlotId = frameData.Slot_Body; // Body 슬롯 ID 가져오기 (frameData 사용)
                Part bodyPart = af.GetPart(bodySlotId);
                Log($"테스트 AF 생성 (커스텀 기반): [{af.Name}], Pilot: [{af.Pilot?.Name ?? "N/A"} ({af.Pilot?.Specialization.ToString() ?? "N/A"})], IsOperational: {af.IsOperational}, " + // SASHA: PilotName -> Name 변경
                    $"BodyPartExists: {bodyPart != null}, BodyPartOperational: {bodyPart?.IsOperational}, " +
                    $"TeamID: {af.TeamId}", LogLevel.Debug);
                // +++ SASHA: 로깅 추가 끝 +++
            }
            // +++ BT 할당 및 블랙보드 초기화 끝 +++

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
                    case PartType.Body: runtimePart = new AF.Models.BodyPart(partSO.PartName ?? partSO.name, partStats, partSO.MaxDurability, partSO.PartWeight); break; // Fully qualified
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
                     weaponSO.MinRange, // MinRange 인자 추가
                     weaponSO.Range, // Range는 MaxRange로 사용
                     weaponSO.AttackSpeed, 
                     weaponSO.OverheatPerShot,
                     weaponSO.BaseAPCost,
                     weaponSO.AmmoCapacity,
                     weaponSO.ReloadAPCost,
                     weaponSO.ReloadTurns,
                     weaponSO.AttackFlavorKey,
                     weaponSO.ReloadFlavorKey // ReloadFlavorKey 인자 추가
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
                        case PartType.Body: runtimePart = new AF.Models.BodyPart(partSO.PartName, partStats, partSO.MaxDurability, partSO.PartWeight); break; // Fully qualified
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
                         weaponSO.WeaponName ?? weaponSO.name,
                         weaponSO.WeaponType, 
                         weaponSO.DamageType,
                         weaponSO.BaseDamage, 
                         weaponSO.Accuracy, 
                         weaponSO.MinRange, // MinRange 인자 추가
                         weaponSO.Range, // Range는 MaxRange로 사용
                         weaponSO.AttackSpeed, 
                         weaponSO.OverheatPerShot,
                         weaponSO.BaseAPCost,
                         weaponSO.AmmoCapacity,
                         weaponSO.ReloadAPCost,
                         weaponSO.ReloadTurns,
                         weaponSO.AttackFlavorKey,
                         weaponSO.ReloadFlavorKey // ReloadFlavorKey 인자 추가
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

        // +++ Add Reset Button (Optional but Recommended) +++
        [TitleGroup("플레이어 스쿼드 (상태 유지)")] // Place button within the player group
        [Button("플레이어 상태 리셋", ButtonSizes.Medium), GUIColor(1f, 0.6f, 0.6f)]
        [PropertySpace(10)]
        public void ResetPlayerSquadState()
        {
            if (Application.isPlaying)
            {
                 _persistentPlayerFrames.Clear();
                 Debug.Log("플레이어 스쿼드 상태가 리셋되었습니다.");
                 // Optionally, force UI update if needed
            }
            else
            {
                 Debug.LogWarning("플레이어 상태 리셋은 플레이 모드에서만 가능합니다.");
            }
        }
        // +++ End Reset Button +++

        // +++ Filtered Log Saving Method +++
        private void SaveFilteredLogToFile(string filename, LogLevelFlags flagsToExclude)
        {
            Debug.Log("SaveFilteredLogToFile: 메서드 진입."); // +++ SASHA: 로그 추가 +++

            // +++ SASHA: 로그 저장 실패 원인 파악용 로그 추가 +++
            if (textLogger == null)
            {
                Debug.LogError("SaveFilteredLogToFile: textLogger (TextLoggerService) is null!");
            }
            else if (textLogger.ConcreteLogger == null)
            {
                Debug.LogError("SaveFilteredLogToFile: textLogger.ConcreteLogger (TextLogger instance) is null!");
            }
            // +++ SASHA: 추가 끝 +++

            if (textLogger?.ConcreteLogger == null)
            {
                Log("Filtered log saving failed: TextLogger not available.", LogLevel.Error);
                Debug.Log("SaveFilteredLogToFile: textLogger or ConcreteLogger is NULL, returning."); // +++ SASHA: 로그 추가 +++
                return;
            }

            Debug.Log("SaveFilteredLogToFile: textLogger and ConcreteLogger ARE NOT NULL. Proceeding to try-catch for file ops."); // +++ SASHA: 로그 추가 +++

            try
            {
                Debug.Log("SaveFilteredLogToFile: Inside file-saving try block."); // +++ SASHA: 로그 추가 +++
                // --- SASHA: 파일 저장용 포맷팅 메서드 호출로 변경 ---
                List<string> filteredLogLines = textLogger.ConcreteLogger.GetFormattedLogsForFileSaving(flagsToExclude);
                Debug.Log($"SaveFilteredLogToFile: Got {filteredLogLines.Count} lines to save."); // +++ SASHA: 로그 추가 +++

                string directory = Path.Combine(Application.persistentDataPath, "Logs");
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string fullFilename = Path.Combine(directory, 
                    $"{filename}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                using (StreamWriter writer = new StreamWriter(fullFilename))
                {
                    writer.WriteLine($"=== 전투 로그 (필터링됨): {filename} ===");
                    writer.WriteLine($"생성 시간: {DateTime.Now}");
                    writer.WriteLine($"제외 레벨: {flagsToExclude}"); 
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine(); // 빈 줄 (헤더와 로그 내용 구분)

                    foreach (var line in filteredLogLines)
                    {
                        writer.WriteLine(line);
                    }

                    writer.WriteLine(); // 빈 줄 (로그 내용과 요약 정보 구분)
                    writer.WriteLine(new string('=', 50));
                    writer.WriteLine($"필터링된 로그 항목 수: {filteredLogLines.Count}");
                    writer.WriteLine("=== 전투 로그 종료 ===");
                }
                Log($"필터링된 전투 로그가 '{fullFilename}'(으)로 저장되었습니다.", LogLevel.System);
            }
            catch (Exception ex)
            {
                Debug.LogError($"필터링된 로그 파일 저장 실패 (EXCEPTION CAUGHT): {ex.Message}\n{ex.StackTrace}"); // SASHA: 로그 메시지 수정
                Log($"필터링된 로그 파일 저장 실패: {ex.Message}", LogLevel.Error);
            }
            Debug.Log("SaveFilteredLogToFile: 메서드 종료."); // +++ SASHA: 로그 추가 +++
        }
        // +++ End Filtered Log Saving Method +++
    }
} 
