using NPOI.SS.UserModel;
using System;
using UnityEngine;

namespace ExcelToSO.DataModels
{
    public class PartData : IExcelRow
    {
        public string id => PartID;

        public string PartID { get; private set; }
        public string PartName { get; private set; }
        public string PartType { get; private set; } // Enum 파싱 필요
        public float Stat_AttackPower { get; private set; }
        public float Stat_Defense { get; private set; }
        public float Stat_Speed { get; private set; }
        public float Stat_Accuracy { get; private set; }
        public float Stat_Evasion { get; private set; }
        public float Stat_Durability { get; private set; }
        public float Stat_EnergyEff { get; private set; }
        public float Stat_MaxAP { get; private set; }
        public float Stat_APRecovery { get; private set; }
        public float MaxDurability { get; private set; }
        public float PartWeight { get; private set; }
        public string Abilities { get; private set; }
        public string Notes { get; private set; }

        public void FromExcelRow(IRow row)
        {
            PartID = row.GetCell(0)?.ToString() ?? "";
            PartName = row.GetCell(1)?.ToString() ?? "";
            PartType = row.GetCell(2)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            Stat_AttackPower = GetFloatValue(row, 3);
            Stat_Defense = GetFloatValue(row, 4);
            Stat_Speed = GetFloatValue(row, 5);
            Stat_Accuracy = GetFloatValue(row, 6);
            Stat_Evasion = GetFloatValue(row, 7);
            Stat_Durability = GetFloatValue(row, 8);
            Stat_EnergyEff = GetFloatValue(row, 9);
            Stat_MaxAP = GetFloatValue(row, 10);
            Stat_APRecovery = GetFloatValue(row, 11);
            MaxDurability = GetFloatValue(row, 12);
            PartWeight = GetFloatValue(row, 13);
            Abilities = row.GetCell(14)?.ToString() ?? "";
            Notes = row.GetCell(15)?.ToString() ?? "";
        }

        private float GetFloatValue(IRow row, int cellIndex, float defaultValue = 0f)
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null) return defaultValue;
            if (cell.CellType == CellType.Numeric) return (float)cell.NumericCellValue;
            if (cell.CellType == CellType.String && float.TryParse(cell.StringCellValue, out float result)) return result;
            return defaultValue;
        }
    }
} 