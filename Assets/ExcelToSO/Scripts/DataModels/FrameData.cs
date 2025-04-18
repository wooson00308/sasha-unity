using NPOI.SS.UserModel;
using System;
using UnityEngine; // For Debug.LogWarning if needed

namespace ExcelToSO.DataModels
{
    public class FrameData : IExcelRow
    {
        // id 프로퍼티 (ScriptableObjectGenerator에서 파일명으로 사용)
        public string id => FrameID; 

        // Frames 시트 컬럼 대응 프로퍼티
        public string FrameID { get; private set; }
        public string FrameName { get; private set; }
        public string FrameType { get; private set; } // Enum 파싱 필요
        public float Stat_AttackPower { get; private set; }
        public float Stat_Defense { get; private set; }
        public float Stat_Speed { get; private set; }
        public float Stat_Accuracy { get; private set; }
        public float Stat_Evasion { get; private set; }
        public float Stat_Durability { get; private set; }
        public float Stat_EnergyEff { get; private set; }
        public float Stat_MaxAP { get; private set; }
        public float Stat_APRecovery { get; private set; }
        public float FrameWeight { get; private set; }
        public string Slot_Head { get; private set; }
        public string Slot_Body { get; private set; }
        public string Slot_Arm_L { get; private set; }
        public string Slot_Arm_R { get; private set; }
        public string Slot_Legs { get; private set; }
        public string Slot_Backpack { get; private set; }
        public string Slot_Weapon_1 { get; private set; }
        public string Slot_Weapon_2 { get; private set; }
        public string Notes { get; private set; }

        public void FromExcelRow(IRow row)
        {
            // NPOI 셀 인덱스는 0부터 시작
            FrameID = row.GetCell(0)?.ToString() ?? "";
            FrameName = row.GetCell(1)?.ToString() ?? "";
            FrameType = row.GetCell(2)?.ToString() ?? ""; // TODO: Enum.Parse 필요
            Stat_AttackPower = GetFloatValue(row, 3);
            Stat_Defense = GetFloatValue(row, 4);
            Stat_Speed = GetFloatValue(row, 5);
            Stat_Accuracy = GetFloatValue(row, 6);
            Stat_Evasion = GetFloatValue(row, 7);
            Stat_Durability = GetFloatValue(row, 8);
            Stat_EnergyEff = GetFloatValue(row, 9);
            Stat_MaxAP = GetFloatValue(row, 10);
            Stat_APRecovery = GetFloatValue(row, 11);
            FrameWeight = GetFloatValue(row, 12);
            Slot_Head = row.GetCell(13)?.ToString() ?? "";
            Slot_Body = row.GetCell(14)?.ToString() ?? "";
            Slot_Arm_L = row.GetCell(15)?.ToString() ?? "";
            Slot_Arm_R = row.GetCell(16)?.ToString() ?? "";
            Slot_Legs = row.GetCell(17)?.ToString() ?? "";
            Slot_Backpack = row.GetCell(18)?.ToString() ?? "";
            Slot_Weapon_1 = row.GetCell(19)?.ToString() ?? "";
            Slot_Weapon_2 = row.GetCell(20)?.ToString() ?? "";
            Notes = row.GetCell(21)?.ToString() ?? "";

            // TODO: 각 셀 타입에 맞는 더 견고한 파싱 로직 및 오류 처리 추가 필요
            // 예를 들어, 숫자 셀인데 문자열이 들어있는 경우 등 처리
        }

        // 간단한 float 파싱 헬퍼 메서드 (오류 처리 추가 필요)
        private float GetFloatValue(IRow row, int cellIndex)
        {
            ICell cell = row.GetCell(cellIndex);
            if (cell == null) return 0f;

            if (cell.CellType == CellType.Numeric)
            {
                return (float)cell.NumericCellValue;
            }
            else if (cell.CellType == CellType.String)
            {
                if (float.TryParse(cell.StringCellValue, out float result))
                {
                    return result;
                }
            }
            // TODO: 다른 셀 타입(Formula 등) 처리 및 오류 로그 추가
            return 0f; 
        }
    }
} 