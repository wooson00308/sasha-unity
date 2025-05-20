using UnityEditor;
using UnityEngine;
using System.IO;
using ExcelToSO.DataModels; // Your data model classes
using AF.Data; // Your ScriptableObject classes

namespace ExcelToSO.Editor
{
    public class DataGeneratorMenu
    {
        private const string ExcelFileName = "AF_Data.xlsx"; // Your Excel file name
        private const string ExcelDirectory = "Assets\\AF\\Data"; // Directory relative to Assets
        private const string OutputDirectoryRoot = "Assets\\AF\\Data\\Resources"; // Base output directory

        [MenuItem("Tools/Generate AF Data from Excel")]
        public static void GenerateData()
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

            Debug.Log($"Starting data generation from: {excelFilePath}");

            // Generate for each data type, specifying the correct sheet name
            GenerateSO<FrameData, FrameSO>(excelFilePath, "Frames", "Frames");
            GenerateSO<PartData, PartSO>(excelFilePath, "Parts", "Parts");
            GenerateSO<WeaponData, WeaponSO>(excelFilePath, "Weapons", "Weapons");
            GenerateSO<PilotData, PilotSO>(excelFilePath, "Pilots", "Pilots");
            GenerateSO<AssemblyData, AssemblySO>(excelFilePath, "AF_Assemblies", "Assemblies"); // Sheet name is AF_Assemblies
            GenerateSO<FlavorTextData, FlavorTextSO>(excelFilePath, "FlavorTexts", "FlavorTexts");
            GenerateSO<AbilityData, AbilitySO>(excelFilePath, "Abilities", "Abilities");

            Debug.Log("Data generation complete!");
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
                Debug.Log($"Created directory: {fullOutputPath}");
            }
            else
            {
                // Optional: Clear existing assets in the directory before generating new ones
                // ClearDirectory(fullOutputPath);
            }

            Debug.Log($"Generating {typeof(TSO).Name} assets from sheet '{sheetName}' into {fullOutputPath}...");
            try
            {
                // Pass sheetName to the generator
                ScriptableObjectGenerator.Generate<TModel, TSO>(excelPath, sheetName, fullOutputPath);
                Debug.Log($"Successfully generated {typeof(TSO).Name} assets from sheet '{sheetName}'.");
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
    }
} 