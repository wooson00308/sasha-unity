using NPOI.SS.UserModel;
using UnityEngine;

namespace ExcelToSO.DataModels
{
    public class AbilityData : IExcelRow
    {
        public string id => AbilityID;

        public string AbilityID { get; private set; }          // A: AB_001
        public string AbilityName { get; private set; }        // B: 강화 레이저
        public string Description { get; private set; }        // C: 일시적으로 레이저 공격력을 강화한다.
        public string AbilityType { get; private set; }        // D: Active / Passive / Triggered
        public string TargetType { get; private set; }         // E: Self / EnemyUnit / AllyUnit / EnemyPart / AllyPart / Position / AoE_EnemyUnits / AoE_AllyUnits / AoE_AllUnits / None
        public string EffectType { get; private set; }         // F: StatModifier / ApplyStatusEffect / DirectDamage / DirectHeal / SpawnObject / SpecialAction / Composite / ControlAbilityUsage / None
        public string EffectParameters { get; private set; }   // G: 예: "Stat:AttackPower,ModType:Additive,Value:10,Duration:3;StatusID:Burn,Chance:0.5"
        public float APCost { get; private set; }             // H: 2.0
        public int CooldownTurns { get; private set; }        // I: 3
        public string ActivationCondition { get; private set; }// J: 예: "OnHit", "OnKill", "HPLessThan:30%", "TurnStart", "Always"
        public string VisualEffectKey { get; private set; }    // K: VFX_LaserBuff
        public string SoundEffectKey { get; private set; }     // L: SFX_LaserBuff
        public string IconID { get; private set; }             // M: ICON_Ability_LaserBuff (Assets/AF/Sprites/Icons/Abilities/ICON_Ability_LaserBuff.png)
        public string Notes { get; private set; }              // N: 기획 노트

        public void FromExcelRow(IRow row)
        {
            AbilityID = GetStringValue(row, 0, "AB_ERR_NO_ID");
            AbilityName = GetStringValue(row, 1, "이름 없음");
            Description = GetStringValue(row, 2);
            AbilityType = GetStringValue(row, 3, "Passive");
            TargetType = GetStringValue(row, 4, "None");
            EffectType = GetStringValue(row, 5, "None");
            EffectParameters = GetStringValue(row, 6);
            APCost = GetFloatValue(row, 7);
            CooldownTurns = GetIntValue(row, 8);
            ActivationCondition = GetStringValue(row, 9, "Always");
            VisualEffectKey = GetStringValue(row, 10);
            SoundEffectKey = GetStringValue(row, 11);
            IconID = GetStringValue(row, 12);
            Notes = GetStringValue(row, 13);
        }

        private string GetStringValue(IRow row, int cellIndex, string defaultValue = "")
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null || string.IsNullOrWhiteSpace(cell.ToString())) return defaultValue;
            return cell.ToString().Trim();
        }

        private float GetFloatValue(IRow row, int cellIndex, float defaultValue = 0f)
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null) return defaultValue;
            if (cell.CellType == CellType.Numeric) return (float)cell.NumericCellValue;
            if (cell.CellType == CellType.String && float.TryParse(cell.StringCellValue, out float result)) return result;
            if (string.IsNullOrWhiteSpace(cell.ToString())) return defaultValue; // 빈 문자열일 경우 기본값
            Debug.LogWarning($"AbilityData - Row {row.RowNum + 1}, Col {cellIndex + 1}: Cannot parse float from '{cell.ToString()}'. Using default value {defaultValue}.");
            return defaultValue;
        }

        private int GetIntValue(IRow row, int cellIndex, int defaultValue = 0)
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null) return defaultValue;
            if (cell.CellType == CellType.Numeric) return (int)cell.NumericCellValue;
            if (cell.CellType == CellType.String && int.TryParse(cell.StringCellValue, out int result)) return result;
            if (string.IsNullOrWhiteSpace(cell.ToString())) return defaultValue; // 빈 문자열일 경우 기본값
            Debug.LogWarning($"AbilityData - Row {row.RowNum + 1}, Col {cellIndex + 1}: Cannot parse int from '{cell.ToString()}'. Using default value {defaultValue}.");
            return defaultValue;
        }
    }
} 