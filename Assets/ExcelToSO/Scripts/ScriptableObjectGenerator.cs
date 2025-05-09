#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace ExcelToSO
{
    public static class ScriptableObjectGenerator
    {
        public static void Generate<TModel, TSO>(string excelPath, string sheetName, string savePath)
            where TModel : IExcelRow, new()
            where TSO : ScriptableObject, new()
        {
            var dataList = ExcelParser.Parse<TModel>(excelPath, sheetName);

            if (dataList == null || dataList.Count == 0)
            {
                 Debug.LogWarning($"No data parsed from sheet '{sheetName}' in {excelPath}. Skipping asset generation for {typeof(TSO).Name}.");
                 return;
            }

            foreach (var data in dataList)
            {
                var so = ScriptableObject.CreateInstance<TSO>();
                var applyMethod = typeof(TSO).GetMethod("Apply");
                if (applyMethod == null)
                {
                    Debug.LogError($"Apply() not found in {typeof(TSO).Name}");
                    continue;
                }

                try
                {
                    applyMethod.Invoke(so, new object[] { data });
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error invoking Apply method on {typeof(TSO).Name} for data ID '{data.GetType().GetProperty("id")?.GetValue(data)}': {ex.Message}\n{ex.InnerException?.StackTrace ?? ex.StackTrace}");
                    continue; // Skip this asset if Apply fails
                }

                var idProperty = data.GetType().GetProperty("id");
                string idValue = idProperty?.GetValue(data)?.ToString();
                if (string.IsNullOrEmpty(idValue))
                {
                    Debug.LogWarning($"Data item from sheet '{sheetName}' is missing an 'id' value. Skipping asset creation.");
                    continue;
                }
                string assetName = $"{idValue}.asset";

                string relativePath = Path.Combine(savePath, assetName).Replace("\\", "/");

                if (!Directory.Exists(savePath))
                {
                     Debug.LogError($"Target directory does not exist: {savePath}. Asset cannot be created.");
                     continue;
                }

                Debug.Log($"Creating/Updating asset at: {relativePath} from sheet '{sheetName}'");
                AssetDatabase.CreateAsset(so, relativePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
