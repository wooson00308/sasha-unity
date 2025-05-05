using System.Collections.Generic;

// 엑셀에서 읽어온 데이터를 그룹화하여 시나리오 하나를 나타내는 클래스
public class ScenarioData
{
    public string ScenarioID { get; set; }
    public string ScenarioName { get; set; }
    public List<UnitSetupData> Units { get; set; } = new List<UnitSetupData>();
} 