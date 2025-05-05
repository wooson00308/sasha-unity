using ExcelToSO; // For IExcelRow
using NPOI.SS.UserModel; // For IRow
using UnityEngine; // For Debug.LogWarning/Error

public class ScenarioRowData : IExcelRow
{
    // IExcelRow 인터페이스 구현 (ID가 명시적으로 필요하지 않다면 빈 문자열 반환)
    public string id => ScenarioID ?? ""; 

    // Scenarios 시트 컬럼과 일치하는 프로퍼티 정의
    public string ScenarioID { get; set; }
    public string ScenarioName { get; set; }
    public string UnitCallsign { get; set; }
    public int TeamID { get; set; }
    public float StartPosX { get; set; }
    public float StartPosY { get; set; }
    public float StartPosZ { get; set; }
    public bool IsPlayerSquad { get; set; } // Excel에 컬럼이 있다면 유지, 없다면 제거하거나 기본값 처리
    public string BuildType { get; set; }   // Excel에 컬럼이 있다면 유지
    public string AssemblyID { get; set; }
    public string Notes { get; set; }

    // IExcelRow 인터페이스의 Apply 메서드 구현 (ExcelParser가 사용할 수 있도록)
    // 이 클래스는 데이터만 담고 실제 Apply 로직은 Generator에서 처리하므로 비워둘 수 있음
    /*
    public void Apply(object data) 
    { 
        // Data is mapped by ExcelParser via reflection based on column names
        // No specific application logic needed here for this intermediate model
    } 
    */

    // IExcelRow 인터페이스의 필수 메서드 구현
    public void FromExcelRow(IRow row)
    {
        ScenarioID = GetStringValue(row, 0);    // A
        ScenarioName = GetStringValue(row, 1);  // B
        AssemblyID = GetStringValue(row, 2);    // C (New)
        UnitCallsign = GetStringValue(row, 3);  // D
        TeamID = GetIntValue(row, 4);         // E
        StartPosX = GetFloatValue(row, 5);      // F
        StartPosY = GetFloatValue(row, 6);      // G
        StartPosZ = GetFloatValue(row, 7);      // H
        IsPlayerSquad = GetBoolValue(row, 8);   // I
        BuildType = GetStringValue(row, 9);     // J
        Notes = GetStringValue(row, 10);        // K
    }

    // Helper methods for safe cell value retrieval and type conversion
    private string GetStringValue(IRow row, int cellIndex)
    {
        return row.GetCell(cellIndex)?.ToString()?.Trim() ?? string.Empty;
    }

    private int GetIntValue(IRow row, int cellIndex)
    {
        if (int.TryParse(GetStringValue(row, cellIndex), out int value))
        {
            return value;
        }
        // Debug.LogWarning($"Row {row.RowNum + 1}, Cell {cellIndex + 1}: Could not parse integer value. Defaulting to 0.");
        return 0;
    }

    private float GetFloatValue(IRow row, int cellIndex)
    {
        if (float.TryParse(GetStringValue(row, cellIndex), out float value))
        {
            return value;
        }
        // Debug.LogWarning($"Row {row.RowNum + 1}, Cell {cellIndex + 1}: Could not parse float value. Defaulting to 0f.");
        return 0f;
    }

    private bool GetBoolValue(IRow row, int cellIndex)
    {
        string valueStr = GetStringValue(row, cellIndex).ToUpper();
        if (valueStr == "TRUE" || valueStr == "1" || valueStr == "YES" || valueStr == "Y")
        {
            return true;
        }
        if (valueStr == "FALSE" || valueStr == "0" || valueStr == "NO" || valueStr == "N" || string.IsNullOrEmpty(valueStr))
        {
             return false;
        }
        // Debug.LogWarning($"Row {row.RowNum + 1}, Cell {cellIndex + 1}: Could not parse boolean value ('{valueStr}'). Defaulting to false.");
        return false;
    }
} 