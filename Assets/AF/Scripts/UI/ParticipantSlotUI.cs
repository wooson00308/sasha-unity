using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;

namespace AF.UI
{
    /// <summary>
    /// 전투 참가자 슬롯 UI의 구성 요소들을 관리하고 상호작용을 처리하는 클래스
    /// </summary>
    public class ParticipantSlotUI : MonoBehaviour
    {
        [Header("UI References")]
        // 모드 선택 (프리셋/커스텀)
        [SerializeField] private TMP_Dropdown modeDropdown; // 예시: 0=프리셋, 1=커스텀
        
        // 프리셋 모드 UI
        [SerializeField] private GameObject presetGroup;
        [SerializeField] private TMP_Dropdown assemblyDropdown;
        
        // 커스텀 모드 UI
        [SerializeField] private GameObject customGroup;
        [SerializeField] private TMP_InputField afNameInput;
        [SerializeField] private TMP_Dropdown frameDropdown;
        [SerializeField] private TMP_Dropdown headDropdown;
        [SerializeField] private TMP_Dropdown bodyDropdown;
        [SerializeField] private TMP_Dropdown armDropdown;
        [SerializeField] private TMP_Dropdown legDropdown;
        [SerializeField] private TMP_Dropdown weapon1Dropdown;
        [SerializeField] private TMP_Dropdown weapon2Dropdown;
        [SerializeField] private TMP_Dropdown pilotDropdown;
        
        // 공통 설정
        [SerializeField] private TMP_InputField teamIdInput;
        [SerializeField] private TMP_InputField posXInput;
        [SerializeField] private TMP_InputField posYInput;
        [SerializeField] private TMP_InputField posZInput;
        [SerializeField] private Button removeButton;

        // 내부 데이터 (CombatSetupUIService에서 설정 정보를 읽어갈 때 사용)
        // public ParticipantSettings CurrentSettings { get; private set; } // 예시

        // CombatSetupUIService 참조 (초기화 시 받아옴)
        private CombatSetupUIService _setupService;

        // 제거 버튼 클릭 시 호출될 이벤트
        public event Action OnRemoveButtonClicked;

        /// <summary>
        /// CombatSetupUIService에서 호출하여 슬롯을 초기화하는 메서드
        /// </summary>
        public void Initialize(CombatSetupUIService setupService, 
                               List<TMP_Dropdown.OptionData> assemblyOptions, 
                               List<TMP_Dropdown.OptionData> frameOptions,
                               List<TMP_Dropdown.OptionData> headOptions,
                               List<TMP_Dropdown.OptionData> bodyOptions,
                               List<TMP_Dropdown.OptionData> armOptions,
                               List<TMP_Dropdown.OptionData> legOptions,
                               List<TMP_Dropdown.OptionData> weaponOptions,
                               List<TMP_Dropdown.OptionData> pilotOptions)
        {
            _setupService = setupService;

            // 드롭다운 옵션 설정
            SetupDropdown(assemblyDropdown, assemblyOptions);
            SetupDropdown(frameDropdown, frameOptions);
            SetupDropdown(headDropdown, headOptions);
            SetupDropdown(bodyDropdown, bodyOptions);
            SetupDropdown(armDropdown, armOptions);
            SetupDropdown(legDropdown, legOptions);
            SetupDropdown(weapon1Dropdown, weaponOptions);
            SetupDropdown(weapon2Dropdown, weaponOptions); // 무기 옵션 공유
            SetupDropdown(pilotDropdown, pilotOptions);

            // 모드 드롭다운 리스너 설정
            modeDropdown?.onValueChanged.AddListener(OnModeChanged);
            // 초기 모드 설정 (예: 프리셋 모드로 시작)
            OnModeChanged(modeDropdown != null ? modeDropdown.value : 0); 

            // 제거 버튼 리스너 설정
            removeButton?.onClick.AddListener(HandleRemoveButtonClick);
            
             // TODO: 각 드롭다운 및 InputField의 onValueChanged 리스너 등록하여 CurrentSettings 업데이트
            Debug.Log("Participant Slot Initialized");
        }
        
        private void SetupDropdown(TMP_Dropdown dropdown, List<TMP_Dropdown.OptionData> options)
        {
            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(options);
            }
        }

        private void OnModeChanged(int modeIndex)
        {
            bool isPresetMode = (modeIndex == 0);
            presetGroup?.SetActive(isPresetMode);
            customGroup?.SetActive(!isPresetMode);
             // TODO: 모드 변경 시 CurrentSettings 업데이트 또는 관련 로직 처리
        }

        private void HandleRemoveButtonClick()
        {
            OnRemoveButtonClicked?.Invoke(); // CombatSetupUIService에 제거 요청 전달
        }

        // TODO: 현재 설정된 값들을 반환하는 메서드 구현 (GetCurrentSettings)
        // 예: public ParticipantSettings GetCurrentSettings() { ... }

        private void OnDestroy()
        {
            // 리스너 해제 (메모리 누수 방지)
            modeDropdown?.onValueChanged.RemoveListener(OnModeChanged);
            removeButton?.onClick.RemoveListener(HandleRemoveButtonClick);
            // TODO: 다른 UI 요소 리스너들도 여기서 해제
        }
    }

    // // 예시: 참가자 설정 정보를 담는 구조체 (필요에 따라 정의)
    // public struct ParticipantSettings
    // {
    //     public bool isCustom;
    //     public string assemblySOName; // 또는 커스텀 파츠 SO 이름들...
    //     public int teamId;
    //     public Vector3 startPosition;
    // }
} 