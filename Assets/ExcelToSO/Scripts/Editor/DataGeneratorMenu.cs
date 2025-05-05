using UnityEditor;
using UnityEngine;
using System.IO;
using ExcelToSO.DataModels; // Your data model classes
using AF.Data; // Your ScriptableObject classes
using ExcelToSO; // ScriptableObjectGenerator 네임스페이스

namespace ExcelToSO.Editor
{
    public class DataGeneratorMenu : EditorWindow
    {
        private const string ExcelFileName = "AF_Data.xlsx"; // Your Excel file name
        private const string ExcelDirectory = "Assets\\AF\\Data"; // Directory relative to Assets
        private const string OutputDirectoryRoot = "Assets\\AF\\Data\\Resources"; // Base output directory

        [MenuItem("AF/Generate Data/Generate All Objects (Except Scenarios)")]
        public static void GenerateAllObjects()
        {
            // Get the project root directory
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("Could not determine project root directory.");
                return;
            }

            // Combine project root with relative path
            string excelFilePath = Path.Combine(projectRoot, ExcelDirectory, ExcelFileName);

            if (!File.Exists(excelFilePath))
            {
                Debug.LogError($"Excel file not found at: {excelFilePath}");
                return;
            }

            Debug.Log($"Starting data generation (except scenarios) from: {excelFilePath}");

            // Generate for each data type, specifying the correct sheet name
            GenerateSO<FrameData, FrameSO>(excelFilePath, "Frames", "Frames");
            GenerateSO<PartData, PartSO>(excelFilePath, "Parts", "Parts");
            GenerateSO<WeaponData, WeaponSO>(excelFilePath, "Weapons", "Weapons");
            GenerateSO<PilotData, PilotSO>(excelFilePath, "Pilots", "Pilots");
            GenerateSO<AssemblyData, AssemblySO>(excelFilePath, "AF_Assemblies", "Assemblies"); // Sheet name is AF_Assemblies
            GenerateSO<FlavorTextData, FlavorTextSO>(excelFilePath, "FlavorTexts", "FlavorTexts");

            Debug.Log("Data generation (except scenarios) complete!");
            AssetDatabase.Refresh(); // Refresh Asset Database after all generation
        }

        private static void GenerateSO<TModel, TSO>(string excelPath, string sheetName, string subFolderName)
            where TModel : IExcelRow, new()
            where TSO : ScriptableObject, new()
        {
            string fullOutputPath = Path.Combine(OutputDirectoryRoot, subFolderName);

            // Create the directory if it doesn't exist
            if (!Directory.Exists(fullOutputPath))
            {
                Directory.CreateDirectory(fullOutputPath);
            }

            try
            {
                // Pass sheetName to the generator
                ScriptableObjectGenerator.Generate<TModel, TSO>(excelPath, sheetName, fullOutputPath);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error generating {typeof(TSO).Name} assets from sheet '{sheetName}': {e.Message}\n{e.StackTrace}");
            }
        }

        // Optional helper to clear existing assets
        private static void ClearDirectory(string path)
        {
             DirectoryInfo directoryInfo = new DirectoryInfo(path);
             if (!directoryInfo.Exists) return;

             Debug.Log($"Clearing directory: {path}");
             foreach (FileInfo file in directoryInfo.GetFiles("*.asset"))
             {
                 // Construct the path relative to the Assets folder for AssetDatabase
                 string relativeAssetPath = Path.Combine(path, file.Name).Replace(Application.dataPath, "Assets").Replace("\\", "/");
                 AssetDatabase.DeleteAsset(relativeAssetPath);
                 Debug.Log($"Deleted asset: {relativeAssetPath}");
             }
             AssetDatabase.Refresh(); // Refresh needed after deletions
        }

        // +++ 시나리오 SO 생성 메뉴 항목 +++
        [MenuItem("AF/Generate Data/Generate Scenario Objects")]
        public static void GenerateScenarioObjects()
        {
            // <<< 기존 OpenFilePanel 삭제 >>>
            // string excelPath = EditorUtility.OpenFilePanel("Select Excel File", "", "xlsx");
            // if (string.IsNullOrEmpty(excelPath)) { ... return; }

            // <<< GenerateAllObjects와 동일하게 경로 계산 로직 추가 >>>
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(projectRoot))
            {
                Debug.LogError("Could not determine project root directory.");
                return;
            }
            string excelFilePath = Path.Combine(projectRoot, ExcelDirectory, ExcelFileName);
            if (!File.Exists(excelFilePath))
            {
                Debug.LogError($"Excel file not found at: {excelFilePath}");
                return;
            }
            // <<< 경로 계산 끝 >>>

            string outputBasePath = Path.Combine("Assets", "AF", "Data", "Resources");

            try
            {
                 // <<< 계산된 excelFilePath 사용 >>>
                 ScriptableObjectGenerator.GenerateScenarioScriptableObjects(excelFilePath, outputBasePath);
            }
            catch (System.Exception ex)
            {
                 Debug.LogError($"Exception occurred during GenerateScenarioScriptableObjects call: {ex.Message}\n{ex.StackTrace}");
            }
            // 시나리오 생성 완료 로그 추가 (필요하다면)
            Debug.Log("Scenario SO generation request completed."); 
            AssetDatabase.Refresh(); // 시나리오 생성 후에도 리프레시
        }
    }
} 