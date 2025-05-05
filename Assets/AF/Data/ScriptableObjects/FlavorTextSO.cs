using UnityEngine;
using ExcelToSO.DataModels; // Required to reference FlavorTextData

namespace AF.Data
{
    /// <summary>
    /// ScriptableObject representing a single flavor text template for combat logs.
    /// Created from data defined in the 'FlavorTexts' sheet of the Excel file.
    /// </summary>
    [CreateAssetMenu(fileName = "Flavor_", menuName = "AF/Data/Flavor Text Template", order = 6)] // Adjust order as needed
    public class FlavorTextSO : ScriptableObject
    {
        [Tooltip("Key used to group related templates (e.g., DamageApplied_HighDamage)")]
        public string templateKey;

        [Tooltip("The actual descriptive text with placeholders (e.g., {attacker} hits {target}!)")]
        [TextArea(3, 10)] // Make it easier to edit longer texts in the inspector
        public string templateText;

        /// <summary>
        /// Populates this ScriptableObject's fields from a FlavorTextData object.
        /// This method is typically called by the ScriptableObjectGenerator.
        /// </summary>
        /// <param name="data">The data parsed from the Excel row.</param>
        public void ApplyData(FlavorTextData data)
        {
            this.templateKey = data.templateKey;
            this.templateText = data.templateText;
            this.name = data.templateKey;
            // Note: The 'id' from FlavorTextData is used for the asset filename, not stored in the SO itself.
        }
    }
} 