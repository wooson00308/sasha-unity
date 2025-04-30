using System.Collections.Generic;
using System.Linq; // Required for Linq operations like Select
using UnityEngine; // Added for Vector3

namespace AF.Models
{
    /// <summary>
    /// Represents a snapshot of a Part's state.
    /// </summary>
    public struct PartSnapshot
    {
        public string Name { get; }
        public float CurrentDurability { get; }
        public float MaxDurability { get; }
        public bool IsOperational { get; }

        public PartSnapshot(Part part)
        {
             Name = part?.Name ?? "Unknown Part"; // Null check for part
             CurrentDurability = part?.CurrentDurability ?? 0;
             MaxDurability = part?.MaxDurability ?? 0;
             IsOperational = part?.IsOperational ?? false;
        }
    }

    /// <summary>
    /// Represents a snapshot of a Weapon's state.
    /// </summary>
    public struct WeaponSnapshot
    {
        public string Name { get; }
        public int CurrentAmmo { get; }
        public int MaxAmmo { get; }
        public bool IsOperational { get; }

        public WeaponSnapshot(Weapon weapon)
        {
            Name = weapon?.Name ?? "Unknown Weapon"; // Null check for weapon
            CurrentAmmo = weapon?.CurrentAmmo ?? 0;
            MaxAmmo = weapon?.MaxAmmo ?? 0;
            IsOperational = weapon?.IsOperational ?? false;
        }
    }

    /// <summary>
    /// Represents a snapshot of an ArmoredFrame's state at a specific moment.
    /// Used for replaying combat logs with synchronized UI updates.
    /// </summary>
    public struct ArmoredFrameSnapshot
    {
        public string Name { get; }
        public Vector3 Position { get; }
        public int TeamId { get; }
        public float CurrentAP { get; }
        public float MaxAP { get; }
        public float CurrentTotalDurability { get; } // Combined durability of all parts
        public float MaxTotalDurability { get; }     // Combined max durability
        public bool IsOperational { get; }         // Is the frame still operational?
        public Stats CombinedStats { get; } // Added Stats snapshot
        public Dictionary<string, PartSnapshot> PartSnapshots { get; } // Added Part snapshots
        public List<WeaponSnapshot> WeaponSnapshots { get; } // Added Weapon snapshots

        public ArmoredFrameSnapshot(ArmoredFrame frame)
        {
            if (frame == null)
            {
                Name = "Invalid";
                Position = Vector3.zero;
                TeamId = -1;
                CurrentAP = 0; MaxAP = 0;
                CurrentTotalDurability = 0; MaxTotalDurability = 0;
                IsOperational = false;
                CombinedStats = default; // Initialize with default
                PartSnapshots = new Dictionary<string, PartSnapshot>(); // Initialize empty
                WeaponSnapshots = new List<WeaponSnapshot>(); // Initialize empty
                return;
            }

            Name = frame.Name;
            Position = frame.Position;
            TeamId = frame.TeamId;
            CurrentAP = frame.CurrentAP;
            // Note: Accessing CombinedStats directly might trigger recalculation if not cached.
            // Consider if a snapshot of Stats itself is needed, or just the relevant values.
            // For simplicity, we take the stats object as is. It's likely struct-based or immutable enough.
            CombinedStats = frame.CombinedStats;
            MaxAP = CombinedStats.MaxAP; // Get MaxAP from the snapshotted stats
            IsOperational = frame.IsOperational;

            // Calculate total durability and create Part snapshots
            CurrentTotalDurability = 0;
            MaxTotalDurability = 0;
            PartSnapshots = new Dictionary<string, PartSnapshot>();
            if (frame.Parts != null)
            {
                foreach (var kvp in frame.Parts)
                {
                    if (kvp.Value != null) // Null check for part
                    {
                        CurrentTotalDurability += kvp.Value.CurrentDurability;
                        MaxTotalDurability += kvp.Value.MaxDurability;
                        PartSnapshots[kvp.Key] = new PartSnapshot(kvp.Value); // Create and add part snapshot
                    }
                    else
                    {
                        // Optionally handle null parts in the dictionary if needed
                        PartSnapshots[kvp.Key] = new PartSnapshot(null); // Or skip adding
                    }
                }
            }

            // Create Weapon snapshots
            WeaponSnapshots = new List<WeaponSnapshot>();
            var weapons = frame.GetAllWeapons(); // Assuming this returns List<Weapon> or similar
            if (weapons != null)
            {
                 WeaponSnapshots = weapons.Select(w => new WeaponSnapshot(w)).ToList();
            }
        }
    }
} 