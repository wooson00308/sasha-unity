using NPOI.SS.UserModel;
using System;
using UnityEngine;

namespace ExcelToSO.DataModels
{
    public class AssemblyData : IExcelRow
    {
        public string id => AssemblyID;

        public string AssemblyID { get; private set; }
        public string AFName { get; private set; }
        public int TeamID { get; private set; }
        public string FrameID { get; private set; } // FK
        public string PilotID { get; private set; } // FK
        public string HeadPartID { get; private set; } // FK
        public string BodyPartID { get; private set; } // FK
        public string LeftArmPartID { get; private set; } // FK
        public string RightArmPartID { get; private set; } // FK
        public string LegsPartID { get; private set; } // FK
        public string BackpackPartID { get; private set; } // FK
        public string Weapon1ID { get; private set; } // FK
        public string Weapon2ID { get; private set; } // FK
        public string Notes { get; private set; }

        public void FromExcelRow(IRow row)
        {
            AssemblyID = row.GetCell(0)?.ToString() ?? "";
            AFName = row.GetCell(1)?.ToString() ?? "";
            TeamID = GetIntValue(row, 2);
            FrameID = row.GetCell(3)?.ToString() ?? "";
            PilotID = row.GetCell(4)?.ToString() ?? "";
            HeadPartID = row.GetCell(5)?.ToString() ?? "";
            BodyPartID = row.GetCell(6)?.ToString() ?? "";
            LeftArmPartID = row.GetCell(7)?.ToString() ?? "";
            RightArmPartID = row.GetCell(8)?.ToString() ?? "";
            LegsPartID = row.GetCell(9)?.ToString() ?? "";
            BackpackPartID = row.GetCell(10)?.ToString() ?? "";
            Weapon1ID = row.GetCell(11)?.ToString() ?? "";
            Weapon2ID = row.GetCell(12)?.ToString() ?? "";
            Notes = row.GetCell(13)?.ToString() ?? "";
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