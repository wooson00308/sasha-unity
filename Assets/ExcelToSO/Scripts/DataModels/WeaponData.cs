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
        public float Range { get; private set; } // MaxRange로 사용될 예정
        public float MinRange { get; private set; } // 새로 추가
        public float AttackSpeed { get; private set; }
        public float OverheatPerShot { get; private set; }
        public int AmmoCapacity { get; private set; }
        public float BaseAPCost { get; private set; }
        public float ReloadAPCost { get; private set; }
        public int ReloadTurns { get; private set; }
        public string SpecialEffects { get; private set; }
        public string AttackFlavorKey { get; private set; } // 공격 Flavor Key 추가
        public string ReloadFlavorKey { get; private set; } // 재장전 Flavor Key 추가
        public string Notes { get; private set; }

        public void FromExcelRow(IRow row)
        {
            WeaponID = row.GetCell(0)?.ToString() ?? "";
            WeaponName = row.GetCell(1)?.ToString() ?? "";
            WeaponType = row.GetCell(2)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            DamageType = row.GetCell(3)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            BaseDamage = GetFloatValue(row, 4);
            Accuracy = GetFloatValue(row, 5);
            Range = GetFloatValue(row, 6); // 기존 Range는 MaxRange로 사용
            MinRange = GetFloatValue(row, 7); // MinRange는 7번 셀에서 읽기 (새 컬럼 가정)
            AttackSpeed = GetFloatValue(row, 8); // 이후 셀 인덱스 1씩 밀림
            OverheatPerShot = GetFloatValue(row, 9);
            AmmoCapacity = GetIntValue(row, 10);
            BaseAPCost = GetFloatValue(row, 11);
            ReloadAPCost = GetFloatValue(row, 12);
            ReloadTurns = GetIntValue(row, 13);
            SpecialEffects = row.GetCell(14)?.ToString() ?? "";
            AttackFlavorKey = row.GetCell(15)?.ToString() ?? ""; // 15번 셀에서 읽기
            ReloadFlavorKey = row.GetCell(16)?.ToString() ?? ""; // 16번 셀에서 읽기
            Notes = row.GetCell(17)?.ToString() ?? ""; // Notes는 17번 셀로 이동
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