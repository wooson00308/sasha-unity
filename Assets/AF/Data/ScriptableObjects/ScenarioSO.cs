using System.Collections.Generic;
using UnityEngine;
using ExcelToSO.DataModels; // 네임스페이스 수정 -> 복구
// using ExcelToSO.DataModels; // Temporarily unused

[CreateAssetMenu(fileName = "Scenario_", menuName = "AF/Data/Scenario")]
public class ScenarioSO : ScriptableObject
{
    public string scenarioID;
    public string scenarioName;

    // 주석 해제
    [System.Serializable]
    public class UnitSetup
    {
        public string unitCallsign;
        public int teamID;
        public Vector3 startPosition;
        public bool isPlayerSquad;
        public string buildType;
        public string assemblyID;
        public string notes;
    }

    public List<UnitSetup> units = new List<UnitSetup>();
    // 주석 해제

    // 주석 해제
    public void ApplyData(ScenarioData data)
    {
        scenarioID = data.ScenarioID;
        scenarioName = data.ScenarioName;
        this.name = data.ScenarioID; // 이름 설정 유지

        units.Clear(); // 기존 유닛 정보 초기화
        if (data.Units != null)
        {
            foreach (var unitData in data.Units)
            {
                units.Add(new UnitSetup
                {
                    unitCallsign = unitData.UnitCallsign,
                    teamID = unitData.TeamID,
                    startPosition = unitData.StartPosition,
                    isPlayerSquad = unitData.IsPlayerSquad,
                    buildType = unitData.BuildType,
                    assemblyID = unitData.AssemblyID,
                    notes = unitData.Notes
                });
            }
        }
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    // 주석 해제
} 