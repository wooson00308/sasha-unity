using UnityEngine;
using ExcelToSO.DataModels;

namespace AF.Data
{
    // Note: This SO primarily stores IDs. 
    // The actual assembly logic (creating ArmoredFrame instance) 
    // would happen elsewhere by referencing these IDs and loading the corresponding SOs.
    [CreateAssetMenu(fileName = "NewAssemblySO", menuName = "AF/Data/Assembly SO")]
    public class AssemblySO : ScriptableObject
    {
        public string AssemblyID;
        public string AFName;
        public int TeamID;
        
        // Store IDs as strings, referencing other SOs
        public string FrameID; 
        public string PilotID; 
        public string HeadPartID; 
        public string BodyPartID; 
        public string LeftArmPartID; 
        public string RightArmPartID; 
        public string LegsPartID; 
        public string BackpackPartID; 
        public string Weapon1ID; 
        public string Weapon2ID; 
        public string Notes;

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
        }
    }
} 