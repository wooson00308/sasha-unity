using NPOI.SS.UserModel;
using System;
using UnityEngine;

namespace ExcelToSO.DataModels
{
    public class PilotData : IExcelRow
    {
        public string id => PilotID;

        public string PilotID { get; private set; }
        public string PilotName { get; private set; }
        public float Stat_AttackPower { get; private set; }
        public float Stat_Defense { get; private set; }
        public float Stat_Speed { get; private set; }
        public float Stat_Accuracy { get; private set; }
        public float Stat_Evasion { get; private set; }
        public float Stat_Durability { get; private set; }
        public float Stat_EnergyEff { get; private set; }
        public float Stat_MaxAP { get; private set; }
        public float Stat_APRecovery { get; private set; }
        public string Specialization { get; private set; } // Enum 파싱 필요
        public int InitialLevel { get; private set; }
        public string InitialSkills { get; private set; }
        public string Notes { get; private set; }

        public void FromExcelRow(IRow row)
        {
            PilotID = row.GetCell(0)?.ToString() ?? "";
            PilotName = row.GetCell(1)?.ToString() ?? "";
            Stat_AttackPower = GetFloatValue(row, 2);
            Stat_Defense = GetFloatValue(row, 3);
            Stat_Speed = GetFloatValue(row, 4);
            Stat_Accuracy = GetFloatValue(row, 5);
            Stat_Evasion = GetFloatValue(row, 6);
            Stat_Durability = GetFloatValue(row, 7);
            Stat_EnergyEff = GetFloatValue(row, 8);
            Stat_MaxAP = GetFloatValue(row, 9);
            Stat_APRecovery = GetFloatValue(row, 10);
            Specialization = row.GetCell(11)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            InitialLevel = GetIntValue(row, 12, 1); // 기본값 1
            InitialSkills = row.GetCell(13)?.ToString() ?? "";
            Notes = row.GetCell(14)?.ToString() ?? "";
        }

        private float GetFloatValue(IRow row, int cellIndex, float defaultValue = 0f)
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null) return defaultValue;
            if (cell.CellType == CellType.Numeric) return (float)cell.NumericCellValue;
            if (cell.CellType == CellType.String && float.TryParse(cell.StringCellValue, out float result)) return result;
            return defaultValue;
        }

        private int GetIntValue(IRow row, int cellIndex, int defaultValue = 0)
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null) return defaultValue;
            if (cell.CellType == CellType.Numeric) return (int)cell.NumericCellValue;
            if (cell.CellType == CellType.String && int.TryParse(cell.StringCellValue, out int result)) return result;
            return defaultValue;
        }
    }
} 