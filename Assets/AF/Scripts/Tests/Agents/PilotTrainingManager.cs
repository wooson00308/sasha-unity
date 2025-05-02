using UnityEngine;
using AF.Tests; // For CombatTestRunner
using AF.Services; // Added for ServiceLocator
using AF.EventBus; // Added for EventBusService and events
using AF.Combat; // Added for CombatSessionEvents
using Cysharp.Threading.Tasks; // For UniTask
using System.Threading; // For CancellationToken
using System; // Added for Exception and OperationCanceledException

namespace AF.Tests.Agents
{
    /// <summary>
    /// Manages the ML-Agents training loop for pilots.
    /// </summary>
    public class PilotTrainingManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField]
        private CombatTestRunner _combatTestRunner;

        [Header("Training Settings")]
        [SerializeField]
        private int _maxEpisodes = 1000;
        [SerializeField]
        private bool _runTrainingOnStart = true;

        private CancellationTokenSource _trainingCts;

        private void Awake()
        {
            if (_combatTestRunner == null)
            {
                Debug.LogError("CombatTestRunner reference is not set in PilotTrainingManager!", this);
            }
        }

        private void Start()
        {
            if (_runTrainingOnStart && _combatTestRunner != null)
            {
                _trainingCts = new CancellationTokenSource();
                StartTrainingLoop(_trainingCts.Token).Forget(); // Start the async loop
            }
        }

        private void OnDestroy()
        {
            // Cancel any ongoing training when the manager is destroyed
            _trainingCts?.Cancel();
            _trainingCts?.Dispose();
        }

        /// <summary>
        /// Starts and manages the training episodes.
        /// </summary>
        public async UniTask StartTrainingLoop(CancellationToken cancellationToken = default)
        {
            Debug.Log("Starting Pilot Training Loop...");

            for (int episode = 1; episode <= _maxEpisodes; episode++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    Debug.Log("Training loop cancelled.");
                    break;
                }

                Debug.Log($"--- Starting Episode {episode}/{_maxEpisodes} ---");

                _combatTestRunner.ResetForNewEpisode();

                // Configure CombatTestRunner for this episode (e.g., participants)
                // TODO: Implement episode-specific setup for CombatTestRunner if needed
                // Example: Randomize opponents, starting positions etc.

                try
                {
                    Debug.Log($"[Episode {episode}] Attempting to await CombatTestRunner.StartCombatTestAsync...");
                    await _combatTestRunner.StartCombatTestAsync().AttachExternalCancellation(cancellationToken);
                    Debug.Log($"Combat simulation for episode {episode} completed.");

                    Debug.Log($"Episode {episode} finished awaiting combat end. Proceeding to next iteration.");
                }
                catch (OperationCanceledException)
                {
                     Debug.Log($"Episode {episode} cancelled during combat simulation.");
                     break;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"Error during episode {episode}: {ex.Message}\n{ex.StackTrace}");
                    break;
                }

                Debug.Log($"End of episode {episode} loop iteration.");
            }

            Debug.Log("Pilot Training Loop finished.");
        }
    }
} 