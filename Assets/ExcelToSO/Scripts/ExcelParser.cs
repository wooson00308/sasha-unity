using System.Collections.Generic;
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;

namespace ExcelToSO
{
    public static class ExcelParser
    {
        public static List<T> Parse<T>(string excelPath) where T : IExcelRow, new()
        {
            var list = new List<T>();
            using var fs = new FileStream(excelPath, FileMode.Open, FileAccess.Read);
            var workbook = new XSSFWorkbook(fs);
            var sheet = workbook.GetSheetAt(0);

            for (int i = 1; i <= sheet.LastRowNum; i++)
            {
                var row = sheet.GetRow(i);
                if (row == null) continue;

                T item = new T();
                item.FromExcelRow(row);
                list.Add(item);
            }

            return list;
        }
    }
}
