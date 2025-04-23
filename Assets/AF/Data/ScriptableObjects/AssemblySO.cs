using UnityEngine;
using ExcelToSO.DataModels;
using Sirenix.OdinInspector;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AF.Data
{
    // Note: This SO primarily stores IDs. 
    // The actual assembly logic (creating ArmoredFrame instance) 
    // would happen elsewhere by referencing these IDs and loading the corresponding SOs.
    [CreateAssetMenu(fileName = "NewAssemblySO", menuName = "AF/Data/Assembly SO")]
    public class AssemblySO : ScriptableObject
    {
        #if UNITY_EDITOR
        [BoxGroup("Assembly Preview", Order = -1)] // Order = -1 to show at the top
        [ShowInInspector, PreviewField(200), ReadOnly] // Preview size 200
        private Sprite assemblyPreview;
        #endif

        [BoxGroup("Assembly Info", ShowLabel = false)]
        [OnValueChanged("UpdatePreviews")]
        public string AssemblyID;
        [BoxGroup("Assembly Info")]
        public string AFName;
        [BoxGroup("Assembly Info")]
        public int TeamID;
        
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string FrameID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string PilotID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string HeadPartID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string BodyPartID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string LeftArmPartID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string RightArmPartID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string LegsPartID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string BackpackPartID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string Weapon1ID; 
        [BoxGroup("Component IDs")]
        [OnValueChanged("UpdatePreviews")]
        public string Weapon2ID; 
        [BoxGroup("Assembly Info")]
        [TextArea]
        public string Notes;

        // --- Preview Section Start ---
        #if UNITY_EDITOR
        [BoxGroup("Part Previews", ShowLabel = false)]
        [ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite headPreview;

        [BoxGroup("Part Previews")]
        [ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite bodyPreview;

        [BoxGroup("Part Previews")]
        [ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite leftArmPreview;

        [BoxGroup("Part Previews")]
        [ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite rightArmPreview;

        [BoxGroup("Part Previews")]
        [ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite legsPreview;

        [BoxGroup("Part Previews")]
        [ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite backpackPreview;
        
        [Button("Update Previews"), BoxGroup("Part Previews")]
        private void UpdatePreviews()
        {
            // Add assembly preview loading
            assemblyPreview = LoadAssemblySpritePreview(AssemblyID);

            // Existing part previews (using renamed method)
            headPreview = LoadPartSpritePreview(HeadPartID);
            bodyPreview = LoadPartSpritePreview(BodyPartID);
            leftArmPreview = LoadPartSpritePreview(LeftArmPartID);
            rightArmPreview = LoadPartSpritePreview(RightArmPartID);
            legsPreview = LoadPartSpritePreview(LegsPartID);
            backpackPreview = LoadPartSpritePreview(BackpackPartID);
        }

        // Renamed for clarity
        private Sprite LoadPartSpritePreview(string partID)
        {
            if (string.IsNullOrEmpty(partID)) return null;
            string path = $"Assets/AF/Sprites/Parts/{partID}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // New method for assembly preview
        private Sprite LoadAssemblySpritePreview(string assemblyID)
        {
             if (string.IsNullOrEmpty(assemblyID)) return null;
             string path = $"Assets/AF/Sprites/Assemblies/{assemblyID}.png"; // Use Assemblies folder
             Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
             // Optional: Log if assembly sprite not found
             // if (loadedSprite == null) Debug.LogWarning($"Preview sprite not found for assembly ID: {assemblyID} at path: {path}");
             return loadedSprite;
        }

        // Call UpdatePreviews when the SO is enabled in the editor or selection changes
        private void OnEnable()
        {
            // Ensure this runs only in the editor
             #if UNITY_EDITOR
            EditorApplication.delayCall += UpdatePreviews; // Delay call to ensure AssetDatabase is ready
             #endif
        }
         private void OnDisable()
         {
             #if UNITY_EDITOR
             EditorApplication.delayCall -= UpdatePreviews;
             #endif
         }
        #endif
        // --- Preview Section End ---


        public void Apply(AssemblyData data)
        {
            AssemblyID = data.AssemblyID;
            AFName = data.AFName;
            TeamID = data.TeamID;
            FrameID = data.FrameID;
            PilotID = data.PilotID;
            HeadPartID = data.HeadPartID;
            BodyPartID = data.BodyPartID;
            LeftArmPartID = data.LeftArmPartID;
            RightArmPartID = data.RightArmPartID;
            LegsPartID = data.LegsPartID;
            BackpackPartID = data.BackpackPartID;
            Weapon1ID = data.Weapon1ID;
            Weapon2ID = data.Weapon2ID;
            Notes = data.Notes;
            
            #if UNITY_EDITOR
            // Update previews after applying data from Excel
            UpdatePreviews();
            #endif
        }
    }
} 