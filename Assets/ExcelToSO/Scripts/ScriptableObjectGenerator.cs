using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using ExcelToSO.DataModels;

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
                var applyMethod = typeof(TSO).GetMethod("ApplyData");
                if (applyMethod == null)
                {
                    Debug.LogError($"ApplyData() not found in {typeof(TSO).Name}");
                    continue;
                }

                try
                {
                    applyMethod.Invoke(so, new object[] { data });
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error invoking ApplyData method on {typeof(TSO).Name} for data from sheet '{sheetName}': {ex.Message}\n{ex.InnerException?.StackTrace ?? ex.StackTrace}");
                    continue;
                }

                string assetPath = Path.Combine(savePath, so.name + ".asset").Replace("\\", "/");

                if (!Directory.Exists(savePath))
                {
                     Debug.LogError($"Target directory does not exist: {savePath}. Asset cannot be created.");
                     ScriptableObject.DestroyImmediate(so);
                     continue;
                }

                TSO existingAsset = AssetDatabase.LoadAssetAtPath<TSO>(assetPath);
                if (existingAsset == null)
                {
                    AssetDatabase.CreateAsset(so, assetPath);
                }
                else
                {
                    applyMethod.Invoke(existingAsset, new object[] { data });
                    EditorUtility.SetDirty(existingAsset);
                    ScriptableObject.DestroyImmediate(so);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static void GenerateScenarioScriptableObjects(string excelPath, string outputBasePath)
        {
            string sheetName = "Scenarios";
            List<ScenarioRowData> allRowsData = null;
            try
            {
                allRowsData = ExcelParser.Parse<ScenarioRowData>(excelPath, sheetName);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{sheetName} 시트 파싱 중 예외 발생: {ex.Message}\n{ex.StackTrace}");
                if (ex.InnerException != null)
                {
                     Debug.LogError($"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}");
                }
                return;
            }

            if (allRowsData == null || allRowsData.Count == 0)
            {
                 Debug.LogWarning($"Parsing '{sheetName}' resulted in 0 rows or null data list.");
                 AssetDatabase.Refresh();
                 return;
            }

            var groupedData = allRowsData
                .Where(row => !string.IsNullOrWhiteSpace(row.ScenarioID))
                .GroupBy(row => row.ScenarioID);

            string scenarioDirectory = Path.Combine(outputBasePath, "Scenarios");
            EnsureDirectoryExists(scenarioDirectory);

            foreach (var group in groupedData)
            {
                var scenarioRows = group.ToList();

                ScenarioData scenarioData = new ScenarioData
                {
                    ScenarioID = group.Key,
                    ScenarioName = scenarioRows.First().ScenarioName ?? "Unnamed Scenario",
                    Units = new List<UnitSetupData>()
                };

                foreach (var rowData in scenarioRows)
                {
                    try
                    {
                        UnitSetupData unitSetup = new UnitSetupData
                        {
                            UnitCallsign = rowData.UnitCallsign ?? "Unknown",
                            TeamID = rowData.TeamID,
                            StartPosX = rowData.StartPosX,
                            StartPosY = rowData.StartPosY,
                            StartPosZ = rowData.StartPosZ,
                            AssemblyID = rowData.AssemblyID ?? "",
                            Notes = rowData.Notes ?? ""
                        };

                        unitSetup.IsPlayerSquad = false;
                        unitSetup.BuildType = "Assembly";

                        scenarioData.Units.Add(unitSetup);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"ScenarioID '{group.Key}'의 유닛 데이터 처리 중 오류 (Row Callsign: {rowData.UnitCallsign}): {ex.Message}");
                    }
                }

                try
                {
                    ScenarioSO scenarioSO = null;
                    string assetPath = string.Empty;

                    string tentativeAssetName = $"Scenario_{group.Key}.asset";
                    string tentativeAssetPath = Path.Combine(scenarioDirectory, tentativeAssetName).Replace("\\", "/");
                    scenarioSO = AssetDatabase.LoadAssetAtPath<ScenarioSO>(tentativeAssetPath);

                    if (scenarioSO == null)
                    {
                        scenarioSO = ScriptableObject.CreateInstance<ScenarioSO>();
                        scenarioSO.ApplyData(scenarioData); // 데이터 적용
                        assetPath = Path.Combine(scenarioDirectory, scenarioSO.name + ".asset").Replace("\\", "/");
                        AssetDatabase.CreateAsset(scenarioSO, assetPath);
                    }
                    else
                    {
                        scenarioSO.ApplyData(scenarioData); // 데이터 적용 (업데이트)
                        assetPath = Path.Combine(scenarioDirectory, scenarioSO.name + ".asset").Replace("\\", "/");
                        EditorUtility.SetDirty(scenarioSO); // 변경사항 저장
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error processing or saving ScenarioSO for ID '{group.Key ?? "UNKNOWN"}'. Exception: {ex.Message}\n{ex.StackTrace}");
                    continue;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}