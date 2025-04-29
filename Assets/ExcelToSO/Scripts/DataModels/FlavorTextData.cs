using NPOI.SS.UserModel;
using UnityEngine; // Needed for Debug.LogWarning

namespace ExcelToSO.DataModels
{
    /// <summary>
    /// Represents a single row from the 'FlavorTexts' sheet in the Excel data file.
    /// Contains the ID, template key, and the actual template text for descriptive combat logs.
    /// </summary>
    public class FlavorTextData : IExcelRow
    {
        public string id { get; private set; }           // Unique ID for the template entry (used for asset naming)
        public string templateKey { get; private set; }  // Key to group related templates (e.g., "DamageApplied_HighDamage")
        public string templateText { get; private set; } // The actual descriptive text with placeholders (e.g., "{attacker} hits {target}!")

        /// <summary>
        /// Parses data from an Excel row and populates the fields of this object.
        /// Assumes specific column indices: 0 for ID, 1 for TemplateKey, 2 for TemplateText.
        /// </summary>
        /// <param name="row">The Excel row to parse.</param>
        public void FromExcelRow(IRow row)
        {
            // Safely get cell values, providing warnings for null or unexpected types
            id = row.GetCell(0)?.ToString() ?? "";
            templateKey = row.GetCell(1)?.ToString() ?? "";
            templateText = row.GetCell(2)?.ToString() ?? "";

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"Row {row.RowNum + 1}: Missing ID in FlavorTexts sheet.");
            }
            if (string.IsNullOrEmpty(templateKey))
            {
                Debug.LogWarning($"Row {row.RowNum + 1} (ID: {id}): Missing TemplateKey in FlavorTexts sheet.");
            }
            if (string.IsNullOrEmpty(templateText))
            {
                 Debug.LogWarning($"Row {row.RowNum + 1} (ID: {id}): Missing TemplateText in FlavorTexts sheet.");
            }
        }
    }
} 