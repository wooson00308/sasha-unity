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

        // Frame Preview
        [BoxGroup("Frame Preview", ShowLabel = false)]
        [ShowInInspector, PreviewField(150), ReadOnly]
        private Sprite framePreview;

        // Pilot Preview
        [BoxGroup("Pilot Preview", ShowLabel = false)]
        [ShowInInspector, PreviewField(150), ReadOnly]
        private Sprite pilotPreview;

        // Part Previews (Existing, slightly reorganized)
        [BoxGroup("Part Previews", ShowLabel = true)] // Show label for this group
        [HorizontalGroup("Part Previews/Row1"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite headPreview;

        [HorizontalGroup("Part Previews/Row1"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite bodyPreview;

        [HorizontalGroup("Part Previews/Row2"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite leftArmPreview;

        [HorizontalGroup("Part Previews/Row2"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite rightArmPreview;

        [HorizontalGroup("Part Previews/Row3"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite legsPreview;

        [HorizontalGroup("Part Previews/Row3"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite backpackPreview;

        // Weapon Previews
        [BoxGroup("Weapon Previews", ShowLabel = true)] // Show label for this group
        [HorizontalGroup("Weapon Previews/Row1"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite weapon1Preview;

        [HorizontalGroup("Weapon Previews/Row1"), ShowInInspector, PreviewField(100), ReadOnly]
        private Sprite weapon2Preview;

        [Button("Update All Previews"), PropertyOrder(100)] // Move button below all previews
        private void UpdatePreviews()
        {
            // Load assembly preview
            assemblyPreview = LoadSpritePreview(AssemblyID, "Assemblies");

            // Load component previews
            framePreview = LoadSpritePreview(FrameID, "Frames");
            pilotPreview = LoadSpritePreview(PilotID, "Pilots");
            headPreview = LoadSpritePreview(HeadPartID, "Parts");
            bodyPreview = LoadSpritePreview(BodyPartID, "Parts");
            leftArmPreview = LoadSpritePreview(LeftArmPartID, "Parts");
            rightArmPreview = LoadSpritePreview(RightArmPartID, "Parts");
            legsPreview = LoadSpritePreview(LegsPartID, "Parts");
            backpackPreview = LoadSpritePreview(BackpackPartID, "Parts");
            weapon1Preview = LoadSpritePreview(Weapon1ID, "Weapons");
            weapon2Preview = LoadSpritePreview(Weapon2ID, "Weapons");
        }

        // Unified method to load previews based on ID and type (folder)
        private Sprite LoadSpritePreview(string id, string typeFolder)
        {
            if (string.IsNullOrEmpty(id)) return null;
            // Construct path assuming base folder Assets/AF/Sprites/
            string path = $"Assets/AF/Sprites/{typeFolder}/{id}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
            // Optional: Log if sprite not found
            // Sprite loadedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            // if (loadedSprite == null) Debug.LogWarning($"Preview sprite not found for ID: {id} in folder: {typeFolder} at path: {path}");
            // return loadedSprite;
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