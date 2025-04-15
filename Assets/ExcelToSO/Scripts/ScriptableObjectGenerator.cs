using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace ExcelToSO
{
    public static class ScriptableObjectGenerator
    {
        public static void Generate<TModel, TSO>(string excelPath, string savePath)
            where TModel : IExcelRow, new()
            where TSO : ScriptableObject, new()
        {
            var dataList = ExcelParser.Parse<TModel>(excelPath);

            foreach (var data in dataList)
            {
                var so = ScriptableObject.CreateInstance<TSO>();
                var applyMethod = typeof(TSO).GetMethod("Apply");
                if (applyMethod == null)
                {
                    Debug.LogError($"Apply() not found in {typeof(TSO).Name}");
                    continue;
                }

                applyMethod.Invoke(so, new object[] { data });

                string assetName = $"{data.GetType().GetProperty("id")?.GetValue(data)}.asset";
                string relativePath = "Assets" + savePath.Replace(Application.dataPath, "") + "/" + assetName;

                AssetDatabase.CreateAsset(so, relativePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
