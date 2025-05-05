# Utility AI Specialization Implementation Plan

## 1. Goal

Modify the `UtilityAIPilotBehaviorStrategy` to exhibit distinct behavioral patterns based on the pilot's `SpecializationType`, similar to the legacy behavior strategies but leveraging the flexibility of the Utility AI system.

## 2. Chosen Approach

Combine two main techniques:

1.  **Action Type Weighting:** Adjust the final utility score of each action type (Attack, Move, Defend, Repair, Reload) based on the pilot's specialization. This controls the general preference for certain actions.
2.  **Dynamic Consideration Parameters & Curves:** Modify the parameters (e.g., range, curve steepness, inversion) and curve types used when creating `IConsideration` instances within each `UtilityAction`. This fine-tunes the situation assessment logic for each specialization.

## 3. Implementation Steps

### Step 3.1: Refine and Inject Action Type Weights

*   **Review `CombatSimulatorService._actionWeights`:** Ensure the static dictionary clearly defines appropriate weight sets for *each* `SpecializationType` (Melee, Ranged, Support, Defense, StandardCombat). Adjust default weights as needed.
    *   *Example (Melee):* Higher weights for `Attack` and `Move`, lower for `Defend`.
    *   *Example (Support):* Higher weights for `RepairSelf` and `RepairAlly`, lower for `Attack`.
*   **Modify `CombatSimulatorService.Initialize` (or pilot creation logic):**
    *   Currently, only `StandardCombat` uses `UtilityAIPilotBehaviorStrategy`. To apply this plan broadly, we need to either:
        *   **Option A (Recommended):** Transition *all* specializations to use `UtilityAIPilotBehaviorStrategy`. In `Initialize`, when creating the `_behaviorStrategies` dictionary, instantiate `UtilityAIPilotBehaviorStrategy` for *each* specialization, passing the corresponding weight set from `_actionWeights` to its constructor.
        *   **Option B (Incremental):** Keep legacy strategies for now. Modify the `DetermineActionForUnit` logic to check if the specialization *should* use Utility AI (e.g., based on a flag or list). If so, fetch the correct weights and potentially instantiate the strategy on-demand or use a pre-configured instance. *(This adds complexity).*
    *   **Decision:** Proceed with **Option A** for a unified approach. Update `CombatSimulatorService.Initialize` to create `UtilityAIPilotBehaviorStrategy` instances for all specializations, injecting the correct weights.

### Step 3.2: Pass SpecializationType Downstream

*   **Modify `UtilityAIPilotBehaviorStrategy.DetermineAction`:** When calling `GeneratePossibleActions`, pass the `activeUnit.Pilot.Specialization` enum value.
*   **Modify `UtilityAIPilotBehaviorStrategy.GeneratePossibleActions`:** Accept the `SpecializationType` as a parameter. When creating `IUtilityAction` instances (e.g., `new AttackUtilityAction(...)`, `new MoveUtilityAction(...)`), pass this `SpecializationType` to their constructors.

### Step 3.3: Adapt Action Classes to Receive SpecializationType

*   **Modify `UtilityActionBase` (or individual action classes):**
    *   Add a protected field/property to store the `SpecializationType` (e.g., `protected SpecializationType PilotSpecialization;`).
    *   Modify the constructors of `AttackUtilityAction`, `MoveUtilityAction`, `DefendUtilityAction`, `ReloadUtilityAction`, `RepairUtilityAction` to accept `SpecializationType` as a parameter and store it in the new field.

### Step 3.4: Dynamically Initialize Considerations (Core Logic)

*   **Modify `InitializeConsiderations` in each `UtilityAction` class:**
    *   Access the stored `PilotSpecialization`.
    *   Use `if/else if` or `switch` statements based on `PilotSpecialization` to create `IConsideration` instances with different parameters or curve types.

    *   **Example - `MoveUtilityAction.InitializeConsiderations`:**
        ```csharp
        protected override void InitializeConsiderations()
        {
            // Common considerations
            var apCostConsideration = new ActionPointCostConsideration(CalculateMoveAPCost()); // AP cost might vary slightly per spec?

            // Specialization-specific considerations
            IConsideration distanceConsideration;
            IConsideration safetyConsideration;

            switch (PilotSpecialization)
            {
                case SpecializationType.MeleeCombat:
                    // Prefer getting close
                    distanceConsideration = new DistanceToEnemyConsideration(
                        _targetPosition, // Target position for the move action
                        UtilityCurveType.Polynomial, // e.g., Inverse square curve
                        minDistance: 0f,
                        maxDistance: 30f, // Relevant max distance for melee
                        steepness: 2f,
                        invert: true // Closer is much better
                    );
                    // Safety is less of a concern for melee
                    safetyConsideration = new TargetPositionSafetyConsideration(
                        _targetPosition,
                        safetyRadius: 3f // Smaller radius?
                        // Maybe use a flatter curve for safety
                    );
                    break;

                case SpecializationType.RangedCombat:
                    // Prefer optimal range, avoid getting too close
                    float optimalRange = AssociatedWeapon?.Range.OptimalRange ?? 15f; // Get from weapon if possible
                    float maxRange = AssociatedWeapon?.Range.MaxRange ?? 25f;
                    distanceConsideration = new TargetDistanceConsideration(
                        _targetPosition,
                        UtilityCurveType.Gaussian, // Bell curve around optimal
                        minDistance: 0f,
                        maxDistance: maxRange * 1.2f, // A bit beyond max
                        offsetX: optimalRange, // Center the curve at optimal range
                        steepness: 3f // Adjust steepness as needed
                        // invert: false (default)
                    );
                    // Safety is more important
                    safetyConsideration = new TargetPositionSafetyConsideration(
                        _targetPosition,
                        safetyRadius: 7f // Larger radius?
                        // Use the default steeper curve
                    );
                    break;

                // ... other specializations ...

                default: // StandardCombat or fallback
                    distanceConsideration = new DistanceToEnemyConsideration(_targetPosition, 100f);
                    safetyConsideration = new TargetPositionSafetyConsideration(_targetPosition);
                    break;
            }

            Considerations = new List<IConsideration>
            {
                distanceConsideration,
                safetyConsideration,
                apCostConsideration
                // Add other relevant considerations
            };
        }
        ```
    *   **Example - `RepairUtilityAction.InitializeConsiderations`:**
        ```csharp
         protected override void InitializeConsiderations()
         {
             IConsideration healthConsideration;
             float apCost = (actor == _targetToRepair) ? 2.0f : 2.5f; // Example cost variation

             switch (PilotSpecialization)
             {
                 case SpecializationType.Support:
                     // Very sensitive to low health
                     healthConsideration = new TargetHealthConsideration(
                         _targetToRepair, UtilityCurveType.Logistic, steepness: 15f, offsetX: 0.1f, invert: true);
                     break;
                 default:
                     healthConsideration = new TargetHealthConsideration(
                         _targetToRepair, UtilityCurveType.Linear, invert: true);
                     break;
             }

             Considerations = new List<IConsideration>
             {
                 new TargetDamagedConsideration(_targetToRepair), // Blocking
                 healthConsideration,
                 new IsAllyOrSelfConsideration(_targetToRepair), // Blocking
                 new ActionPointCostConsideration(apCost)       // Blocking
             };
         }
        ```
    *   **Important:** Ensure Action classes have access to necessary context (like the `actor` or specific `weapon`) if needed to determine parameters during `InitializeConsiderations`. This might require passing more info to the Action constructor.

### Step 3.5: Testing and Tuning

*   Utilize `CombatTestRunner` to set up scenarios with pilots of different `SpecializationType`.
*   Enable AI decision logging (`logAIDecisions = true` in `CombatTestRunner`) to observe:
    *   Consideration scores for each action.
    *   Final weighted utility scores.
    *   The chosen action for each specialization in various situations.
*   Iteratively adjust:
    *   Action type weights (`_actionWeights` in `CombatSimulatorService`).
    *   Consideration parameters and curve types within `InitializeConsiderations` methods.
*   Verify that the observed behavior aligns with the intended characteristics of each specialization.

## 4. Potential Challenges & Considerations

*   **Complexity Management:** `InitializeConsiderations` methods could become complex with many specializations. Consider helper methods or potentially a factory pattern if it gets unwieldy.
*   **Data Access:** Action classes need access to relevant data (pilot spec, weapon stats) during initialization. Ensure this data is passed correctly through constructors.
*   **Tuning Difficulty:** Finding the right balance of weights and curve parameters requires careful testing and iteration.
*   **Legacy Strategy Transition:** Ensure a smooth transition if replacing legacy strategies (Option A). Test thoroughly to avoid regressions.

This plan provides a structured way to introduce specialized AI behaviors using the existing Utility AI framework. 