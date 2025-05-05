using UnityEngine; // Vector3 사용 위해

// 시나리오 내 단일 유닛 배치 정보를 담는 클래스
// IExcelRow를 구현하지 않음 (Generator에서 직접 생성)
[System.Serializable] // ScenarioSO에서 리스트로 사용할 수 있도록
public class UnitSetupData
{
    public string UnitCallsign { get; set; }
    public int TeamID { get; set; }
    public float StartPosX { get; set; }
    public float StartPosY { get; set; }
    public float StartPosZ { get; set; }
    public bool IsPlayerSquad { get; set; } // 항상 false가 될 예정
    public string BuildType { get; set; }   // 항상 "Assembly"가 될 예정
    public string AssemblyID { get; set; } // 참조할 Assembly ID
    public string Notes { get; set; }

    // Vector3 로 변환하는 편의 프로퍼티
    public Vector3 StartPosition => new Vector3(StartPosX, StartPosY, StartPosZ);
} 