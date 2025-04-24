using UnityEngine;
using AF.Services;
using System.Collections.Generic; // 나중에 사용할 수 있으므로 미리 추가
using TMPro; // UI 요소 사용 위해 추가
using UnityEngine.UI; // UI 요소 사용 위해 추가
using System.Linq; // Linq 사용 위해 추가
using AF.Data; // SO 클래스들 사용 위해 추가
using AF.Models; // PartType, ArmoredFrame 등 사용 위해 추가
using AF.Combat; // CombatSimulatorService 사용 위해 추가
using AF.EventBus; // EventBus 사용 위해 추가

namespace AF.UI
{
    // 참가자 슬롯 UI를 관리할 클래스 (새로 만들어야 함)
    // public class ParticipantSlotUI : MonoBehaviour { /* ... */ }

    /// <summary>
    /// uGUI를 통해 전투 테스트 설정을 관리하는 서비스
    /// </summary>
    public class CombatSetupUIService : MonoBehaviour, IService
    {
        // --- UI 참조 변수들 --- 
        [Header("UI References")]
        [SerializeField] private GameObject combatSetupPanel;
        [SerializeField] private GameObject combatLogPanel; // 기존 로그 패널 참조
        [SerializeField] private Button startCombatButton;
        [SerializeField] private Button addParticipantButton;
        [SerializeField] private ScrollRect participantListScrollView;
        [SerializeField] private Transform participantListContent;
        [SerializeField] private GameObject participantSlotPrefab; // 이 프리팹에는 ParticipantSlotUI 스크립트가 있어야 함
        
        // --- 데이터 로딩 및 관리 --- 
        private Dictionary<string, AssemblySO> _availableAssemblies = new Dictionary<string, AssemblySO>();
        private Dictionary<string, FrameSO> _availableFrames = new Dictionary<string, FrameSO>();
        private Dictionary<string, PartSO> _availableParts = new Dictionary<string, PartSO>();
        private Dictionary<string, WeaponSO> _availableWeapons = new Dictionary<string, WeaponSO>();
        private Dictionary<string, PilotSO> _availablePilots = new Dictionary<string, PilotSO>();
        
        // --- 드롭다운 옵션 리스트 --- 
        private List<TMP_Dropdown.OptionData> _assemblyOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _frameOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _headPartOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _bodyPartOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _armPartOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _legPartOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _weaponOptions = new List<TMP_Dropdown.OptionData>();
        private List<TMP_Dropdown.OptionData> _pilotOptions = new List<TMP_Dropdown.OptionData>();

        // --- 내부 상태 변수들 --- 
        private List<ParticipantSlotUI> _participantSlots = new List<ParticipantSlotUI>(); // 참가자 슬롯 UI 스크립트 리스트
        
        // --- 서비스 참조 --- 
        private ICombatSimulatorService _combatSimulator;
        private EventBus.EventBus _eventBus; // EventBus 참조 추가

        // --- IService 구현부 ---
        public void Initialize()
        {
            Debug.Log("CombatSetupUIService 초기화 시작");
            // 서비스 참조 가져오기
            try 
            {
                _combatSimulator = ServiceLocator.Instance.GetService<CombatSimulatorService>();
                _eventBus = ServiceLocator.Instance.GetService<EventBusService>().Bus; // EventBus 가져오기
            }
            catch (System.InvalidOperationException ex)
            {
                Debug.LogError($"필수 서비스 초기화 실패: {ex.Message}");
                enabled = false;
                return;
            }
            
            LoadAllData(); // 데이터 로딩 호출
            PrepareDropdownOptions(); // 드롭다운 옵션 준비
            RegisterButtonListeners(); // 버튼 리스너 등록
            SubscribeToEvents(); // 이벤트 구독 추가
            InitializeUI(); // UI 초기화 호출
            // 초기 상태는 설정 패널 활성화
            SwitchToSetupPanel(); 
            Debug.Log("CombatSetupUIService 초기화 완료");
        }

        public void Shutdown()
        {
             Debug.Log("CombatSetupUIService 종료");
             UnregisterButtonListeners(); // 버튼 리스너 해제
             UnsubscribeFromEvents(); // 이벤트 구독 해제 추가
             _combatSimulator = null; // 참조 해제
             _eventBus = null; // 참조 해제
        }
        
        // --- 데이터 로딩 메서드 ---
        private void LoadAllData()
        {
            Debug.Log("SO 데이터 로딩 시작...");
            LoadResource<AssemblySO>(_availableAssemblies, "Data/Assemblies");
            LoadResource<FrameSO>(_availableFrames, "Data/Frames");
            LoadResource<PartSO>(_availableParts, "Data/Parts");
            LoadResource<WeaponSO>(_availableWeapons, "Data/Weapons");
            LoadResource<PilotSO>(_availablePilots, "Data/Pilots");
            Debug.Log("SO 데이터 로딩 완료.");
        }

        private void LoadResource<T>(Dictionary<string, T> database, string path) where T : ScriptableObject
        {
            database.Clear();
            var resources = Resources.LoadAll<T>(path);
            foreach (var resource in resources)
            {
                if (!database.ContainsKey(resource.name))
                {
                    database.Add(resource.name, resource);
                }
                else
                {
                    Debug.LogWarning($"{typeof(T).Name} '{resource.name}' (이)가 이미 로드되었습니다. 중복된 이름일 수 있습니다.");
                }
            }
             Debug.Log($"{typeof(T).Name} 타입 {database.Count}개 로드 완료.");
        }

        // --- UI 초기화 및 리스너/이벤트 구독 ---
        private void PrepareDropdownOptions()
        {
            Debug.Log("드롭다운 옵션 준비 시작...");
            _assemblyOptions = CreateDropdownOptions(_availableAssemblies.Keys);
            _frameOptions = CreateDropdownOptions(_availableFrames.Keys);
            _headPartOptions = CreateDropdownOptions(_availableParts.Where(p => p.Value.PartType == PartType.Head).Select(p => p.Key));
            _bodyPartOptions = CreateDropdownOptions(_availableParts.Where(p => p.Value.PartType == PartType.Body).Select(p => p.Key));
            _armPartOptions = CreateDropdownOptions(_availableParts.Where(p => p.Value.PartType == PartType.Arm).Select(p => p.Key));
            _legPartOptions = CreateDropdownOptions(_availableParts.Where(p => p.Value.PartType == PartType.Legs).Select(p => p.Key));
            _weaponOptions = CreateDropdownOptions(_availableWeapons.Keys);
            _pilotOptions = CreateDropdownOptions(_availablePilots.Keys);
            Debug.Log("드롭다운 옵션 준비 완료.");
        }

        private List<TMP_Dropdown.OptionData> CreateDropdownOptions(IEnumerable<string> keys)
        {
            var options = new List<TMP_Dropdown.OptionData>();
            options.Add(new TMP_Dropdown.OptionData("-")); 
            options.AddRange(keys.OrderBy(k => k).Select(key => new TMP_Dropdown.OptionData(key))); // 이름순 정렬 추가
            return options;
        }
        
        private void RegisterButtonListeners()
        {
            startCombatButton?.onClick.AddListener(OnStartCombatButtonClicked);
            addParticipantButton?.onClick.AddListener(OnAddParticipantButtonClicked);
        }

        private void UnregisterButtonListeners()
        {
            startCombatButton?.onClick.RemoveListener(OnStartCombatButtonClicked);
            addParticipantButton?.onClick.RemoveListener(OnAddParticipantButtonClicked);
        }

        private void SubscribeToEvents()
        {
             _eventBus?.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
        }

        private void UnsubscribeFromEvents()
        {
            _eventBus?.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
        }
        
        private void InitializeUI()
        {
            Debug.Log("UI 초기화 시작...");
            // 기존 슬롯들 클리어 (씬에 미리 배치된 슬롯이 있다면)
            foreach (Transform child in participantListContent)
            {
                Destroy(child.gameObject);
            }
            _participantSlots.Clear();

            // 초기 참가자 슬롯 1개 추가 (기본값)
            OnAddParticipantButtonClicked(); 
            Debug.Log("UI 초기화 완료.");
        }

        // --- UI 상태 전환 메서드 ---
        private void SwitchToSetupPanel()
        {
            combatSetupPanel?.SetActive(true);
            combatLogPanel?.SetActive(false);
            Debug.Log("UI 상태: 설정 패널 활성화");
        }

        private void SwitchToLogPanel()
        {
            combatSetupPanel?.SetActive(false);
            combatLogPanel?.SetActive(true);
            Debug.Log("UI 상태: 로그 패널 활성화");
            // 로그 패널이 활성화될 때 로그 UI 클리어 (CombatTextUIService 와 중복될 수 있으니 확인 필요)
            // ServiceLocator.Instance.GetService<CombatTextUIService>()?.ClearLog(); 
        }

        // --- 이벤트 핸들러 ---
        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent evt)
        {
            Debug.Log("CombatEndEvent 수신됨. 설정 패널로 전환합니다.");
            // 전투 종료 시 바로 설정 패널로 돌아가도록 설정
            SwitchToSetupPanel(); 
            // 필요하다면, 로그 표시가 끝난 후 전환하도록 CombatTextUIService와 연계 필요
        }
        
        // --- UI 상호작용 메서드들 --- 
        private void OnAddParticipantButtonClicked()
        {
            Debug.Log("참가자 추가 버튼 클릭됨");
            if (participantSlotPrefab == null || participantListContent == null)
            {
                Debug.LogError("Participant Slot Prefab 또는 Content Transform이 설정되지 않았습니다!");
                return;
            }

            GameObject slotInstance = Instantiate(participantSlotPrefab, participantListContent);
            ParticipantSlotUI slotUI = slotInstance.GetComponent<ParticipantSlotUI>();

            if (slotUI != null)
            {
                slotUI.Initialize(this, _assemblyOptions, _frameOptions, _headPartOptions, _bodyPartOptions, _armPartOptions, _legPartOptions, _weaponOptions, _pilotOptions);
                _participantSlots.Add(slotUI);
                slotUI.OnRemoveButtonClicked += () => RemoveParticipant(slotUI);
            }
            else
            {
                Debug.LogError("Participant Slot Prefab에 ParticipantSlotUI 스크립트가 없습니다!");
                Destroy(slotInstance);
            }
        }
        
        private void RemoveParticipant(ParticipantSlotUI slotToRemove)
        {
            if (slotToRemove != null && _participantSlots.Contains(slotToRemove))
            {
                 slotToRemove.OnRemoveButtonClicked -= () => RemoveParticipant(slotToRemove); 
                _participantSlots.Remove(slotToRemove);
                Destroy(slotToRemove.gameObject);
                Debug.Log($"참가자 슬롯 제거 완료. 현재 참가자 수: {_participantSlots.Count}");
            }
        }

        private void OnStartCombatButtonClicked()
        {
            Debug.Log("전투 시작 버튼 클릭됨");
            if (_combatSimulator == null || _participantSlots.Count < 2) return;

            List<ArmoredFrame> participants = new List<ArmoredFrame>();
            List<string> participantNames = new List<string>(); 

            foreach (var slotUI in _participantSlots)
            {
                // TODO: ParticipantSlotUI에 GetCurrentSettings() 메서드 구현 필요
                // ParticipantSettings settings = slotUI.GetCurrentSettings();
                // if (settings == null) { /* 오류 처리 */ return; }
                
                // TODO: settings 정보를 바탕으로 ArmoredFrame 생성
                // ArmoredFrame af = CreateArmoredFrameFromSettings(settings);
                ArmoredFrame af = null; // 임시 null
                
                if (af != null)
                {
                    // TODO: 이름 중복 체크 및 추가 로직
                    // if (participantNames.Contains(af.Name)) { /* 오류 처리 */ return; }
                    // participantNames.Add(af.Name);
                    participants.Add(af);
                }
                else
                {
                     Debug.LogError("ArmoredFrame 인스턴스 생성에 실패했습니다. (TODO)");
                     // return; // 실제 구현 시 주석 해제
                }
            }

            // 참가자 생성 시뮬레이션 (테스트용) -> 실제 생성 로직 구현 전까지 주석 처리
            // if (participants.Count == 0 && _participantSlots.Count >= 2)
            // {
            //      Debug.LogWarning("ArmoredFrame 생성 로직이 아직 구현되지 않아 임시 ArmoredFrame을 사용합니다.");
            //      // TODO: 임시 AF 생성 로직 제거하고 실제 생성 로직 구현
            //      participants.Add(new ArmoredFrame("TempAF1", /* frame */ null, Vector3.zero, 1)); 
            //      participants.Add(new ArmoredFrame("TempAF2", /* frame */ null, Vector3.one, 2)); 
            // }

            // 유효한 참가자로 전투 시작 (임시: 참가자 리스트가 비어있어도 일단 진행)
            if (participants.Count >= 0) // 임시로 0명 이상이면 진행 (나중에 >= 2 로 복구)
            {
                 // 실제 참가자 수가 0이면 경고 로그 추가
                 if (participants.Count == 0)
                 {
                     Debug.LogWarning("생성된 ArmoredFrame이 없어 전투를 시작할 수 없습니다. (ArmoredFrame 생성 로직 구현 필요)");
                     return; // 전투 시작 방지
                 }
                 
                Debug.Log($"{participants.Count}명의 참가자로 전투를 시작합니다.");
                string battleName = $"TestBattle_{System.DateTime.Now:HHmmss}"; 
                _combatSimulator.StartCombat(participants.ToArray(), battleName);
                
                // UI 상태 전환
                SwitchToLogPanel(); 
            }
            else // 이 조건은 현재 도달하지 않음
            {
                 Debug.LogError("유효한 참가자를 준비하지 못해 전투를 시작할 수 없습니다.");
            }
        }
        
        // TODO: ArmoredFrame 생성 헬퍼 메서드 구현
        // ... (CreateArmoredFrameFromSettings)

    }
} 