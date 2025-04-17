using UnityEngine;
using AF.Models;
using AF.Combat;
using AF.Services;
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
                RunSimpleCombatTest();
            }
        }

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            _textLogger.TextLogger.Log(message, level);
        }

        /// <summary>
        /// 간단한 1:1 전투 테스트를 실행합니다.
        /// </summary>
        private void RunSimpleCombatTest()
        {
            Debug.Log("=== Combat Test Start ===");
            Log("=== Combat Test Start ===", LogLevel.System);
            _textLogger.TextLogger.Clear(); // 이전 로그 지우기

            // 1. 더미 데이터 생성 (ArmoredFrame 두 대)
            ArmoredFrame playerAF = CreateTestArmoredFrame("Player AF", 0, new Vector3(0, 0, 0));
            ArmoredFrame enemyAF = CreateTestArmoredFrame("Enemy AF", 1, new Vector3(10, 0, 0)); // 적당히 떨어진 위치

            if (playerAF == null || enemyAF == null)
            {
                Debug.LogError("테스트 AF 생성 실패!");
                Log("테스트 AF 생성 실패!", LogLevel.Critical);
                return;
            }
            
            // ArmoredFrame이 무기를 가지고 있는지 확인
            if (playerAF.GetEquippedWeapons().Count == 0 || enemyAF.GetEquippedWeapons().Count == 0)
            {
                 Debug.LogError("테스트 AF에 무기가 없습니다!");
                 Log("테스트 AF에 무기가 없습니다!", LogLevel.Critical);
                 return;
            }

            // 2. 전투 시작
            ArmoredFrame[] participants = { playerAF, enemyAF };
            string battleId = _combatSimulator.StartCombat(participants, "Simple Test Battle");
            Debug.Log($"Battle Started with ID: {battleId}");
            Log($"Battle Started with ID: {battleId}", LogLevel.System);

            // 3. 전투 진행 (자동 진행)
            // CombatSimulatorService의 ProcessNextTurn과 행동 결정 로직에 따라 진행됨
            // 여기서는 최대 턴 수를 제한하거나 특정 조건에서 종료할 수 있음
            int maxTurns = 300;
            while (_combatSimulator.IsInCombat && _combatSimulator.CurrentTurn < maxTurns)
            {
                 Debug.Log($"--- Turn {_combatSimulator.CurrentTurn + 1} Processing --- ");
                Log($"--- Turn {_combatSimulator.CurrentTurn + 1} Processing --- ", LogLevel.System);
                bool turnProcessed = _combatSimulator.ProcessNextTurn();
                 if (!turnProcessed)
                 {
                     // 전투가 중간에 종료된 경우 (예: 한쪽 전멸)
                     break;
                 }
                 // 각 턴의 상세 로그를 보고 싶다면 여기서 출력 가능
                 if (_logCombatDetails)
                 {
                     //LogCurrentTurnDetails();
                 }
            }
            
            // 4. 전투 종료 확인
            if (_combatSimulator.IsInCombat)
            {
                 Debug.LogWarning($"최대 턴({maxTurns}) 경과 후에도 전투 미종료. 강제 종료.");
                 Log($"최대 턴({maxTurns}) 경과 후에도 전투 미종료. 강제 종료.", LogLevel.Warning);
                _combatSimulator.EndCombat(CombatSessionEvents.CombatEndEvent.ResultType.Draw);
            }

            // 5. 최종 로그 출력
            Debug.Log("=== Combat Test End ===");
            Log("=== Combat Test End ===", LogLevel.System);
            Debug.Log("--- Final Combat Log ---");
            List<string> finalLogs = _textLogger.TextLogger.GetLogs();
            // foreach (string log in finalLogs) // Don't call user Log here, it's already in _textLogger
            // {
            //     Debug.Log(log);
            // }
             _textLogger.TextLogger.SaveToFile("SimpleCombatTest");
        }

        /// <summary>
        /// 테스트용 ArmoredFrame 인스턴스를 생성합니다.
        /// </summary>
        private ArmoredFrame CreateTestArmoredFrame(string name, int teamId, Vector3 position)
        {
            // 기본 프레임 생성 (임시 스탯)
            Stats frameStats = new Stats(attackPower: 0, defense: 10, speed: 5, accuracy: 0, evasion: 0.1f, durability: 500, energyEfficiency: 1.0f);
            Frame testFrame = new Frame("TestFrame", FrameType.Standard, frameStats);
            
            // 파일럿 생성 (임시 스탯)
            Stats pilotStats = new Stats(attackPower: 5, defense: 5, speed: 5, accuracy: 0.7f, evasion: 0.2f, durability: 0, energyEfficiency: 0);
            Pilot testPilot = new Pilot("TestPilot " + teamId, pilotStats, SpecializationType.Combat);
            
            // ArmoredFrame 생성
            ArmoredFrame af = new ArmoredFrame(name, testFrame, position);
            af.AssignPilot(testPilot);

            // 기본 파츠 생성 및 부착
            Stats headStats = new Stats(durability: 50);
            Stats bodyStats = new Stats(durability: 150);
            Stats armsStats = new Stats(durability: 80);
            Stats legsStats = new Stats(durability: 100, speed: 2);
            
            Part head = new HeadPart("Test Head", headStats, 50f);
            Part body = new BodyPart("Test Body", bodyStats, 150f);
            Part arms = new ArmsPart("Test Arms", armsStats, 80f);

            Part legs = new LegsPart("Test Legs", legsStats, 100f);
            
            af.AttachPart(head);
            af.AttachPart(body);
            af.AttachPart(arms);
            af.AttachPart(legs);

            // 무기 생성 및 부착 (임시 스탯)
            Weapon testWeapon = new Weapon("Test Rifle", WeaponType.MidRange, DamageType.Physical, 30f, 0.8f, 10f, 1.0f, 0.1f);
            af.AttachWeapon(testWeapon);

            Debug.Log($"Created Test AF: {name}, Team: {teamId}, Pos: {position}");
            Log($"Created Test AF: {name}, Team: {teamId}, Pos: {position}", LogLevel.Info);
            return af;
        }
        
        // TODO: 각 턴의 상세 정보를 로깅하는 메서드 구현
        // private void LogCurrentTurnDetails()
        // {
        //     // 현재 유닛, 체력, 위치 등 정보 로깅
        // }
    }
} 