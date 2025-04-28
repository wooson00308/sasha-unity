using NPOI.SS.UserModel;
using System;
using UnityEngine;

namespace ExcelToSO.DataModels
{
    public class WeaponData : IExcelRow
    {
        public string id => WeaponID;

        public string WeaponID { get; private set; }
        public string WeaponName { get; private set; }
        public string WeaponType { get; private set; } // Enum 파싱 필요
        public string DamageType { get; private set; } // Enum 파싱 필요
        public float BaseDamage { get; private set; }
        public float Accuracy { get; private set; }
        public float Range { get; private set; }
        public float AttackSpeed { get; private set; }
        public float OverheatPerShot { get; private set; }
        public int AmmoCapacity { get; private set; }
        public float BaseAPCost { get; private set; }
        public float ReloadAPCost { get; private set; }
        public int ReloadTurns { get; private set; }
        public string SpecialEffects { get; private set; }
        public string Notes { get; private set; }

        public void FromExcelRow(IRow row)
        {
            WeaponID = row.GetCell(0)?.ToString() ?? "";
            WeaponName = row.GetCell(1)?.ToString() ?? "";
            WeaponType = row.GetCell(2)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            DamageType = row.GetCell(3)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            BaseDamage = GetFloatValue(row, 4);
            Accuracy = GetFloatValue(row, 5);
            Range = GetFloatValue(row, 6);
            AttackSpeed = GetFloatValue(row, 7);
            OverheatPerShot = GetFloatValue(row, 8);
            AmmoCapacity = GetIntValue(row, 9);
            BaseAPCost = GetFloatValue(row, 10);
            ReloadAPCost = GetFloatValue(row, 11);
            ReloadTurns = GetIntValue(row, 12);
            SpecialEffects = row.GetCell(13)?.ToString() ?? "";
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