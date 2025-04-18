using System.Collections.Generic;
using System.IO;
using NPOI.XSSF.UserModel;
using NPOI.SS.UserModel;
using UnityEngine;

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

        public static List<TModel> Parse<TModel>(string excelPath, string sheetName)
            where TModel : IExcelRow, new()
        {
            List<TModel> results = new List<TModel>();

            using (FileStream fileStream = new FileStream(excelPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var workbook = new XSSFWorkbook(fileStream);
                var sheet = workbook.GetSheet(sheetName);

                if (sheet == null)
                {
                    Debug.LogError($"Sheet '{sheetName}' not found in Excel file: {excelPath}");
                    return results;
                }

                // Assuming header is row 0, start data from row 1
                int headerRowIndex = 0; 
                for (int rowIndex = headerRowIndex + 1; rowIndex <= sheet.LastRowNum; rowIndex++)
                {
                    var row = sheet.GetRow(rowIndex);
                    if (row == null) continue; // Skip empty rows

                    // Basic check: Skip if first cell is empty (often indicates end of data or blank row)
                    var firstCell = row.GetCell(0);
                    if (firstCell == null || string.IsNullOrWhiteSpace(firstCell.ToString()))
                    {
                        continue; 
                    }

                    try
                    {
                        TModel data = new TModel();
                        // Call FromExcelRow with only the row, assuming fixed indices inside the method
                        data.FromExcelRow(row); 
                        results.Add(data);
                    }
                    catch(System.Exception e)
                    {
                         // Log error for this specific row and continue if possible
                         Debug.LogError($"Error parsing row {rowIndex + 1} in sheet '{sheetName}': {e.Message}\n{e.StackTrace}");
                    }
                }
            }

            return results;
        }
    }
}
