# Task 5: Debugging & Logging System - AI Behavior Troubleshooting

This document details the troubleshooting process undertaken to resolve issues with the Utility AI's combat behavior, specifically addressing why AI units were not attacking effectively.

## Initial Problem

AI units (Cobalt Vanguards in testing) exhibited looping behavior, constantly moving towards and away from each other without engaging in attacks, even when within weapon range. Analysis of early logs (e.g., `BattleLog_3ff2e248_20250504_005242.txt`) indicated that the calculated utility score for the `Move` action consistently outweighed the `Attack` action, preventing attacks from being selected.

## Troubleshooting Steps & Resolutions

The following steps were taken iteratively, involving log analysis and code modifications:

1.  **AP Cost Calculation (`ActionPointCostConsideration.cs`):**
    *   **Issue:** An incorrect `Mathf.Clamp(score, 0.01f, 1.0f)` was forcing a minimum score of 0.01 even when the unit lacked sufficient AP for an action (e.g., Rifle cost 4 AP, unit had 3.9 AP).
    *   **Fix:** Removed the incorrect clamping, ensuring the score correctly returned 0f when AP was insufficient. Verified via logs ([ActionPointCostConsideration] logs).
    *   **Result:** Correct AP cost evaluation, but AI still did not attack consistently.

2.  **Hit Chance Clamping (`HitChanceConsideration.cs`):**
    *   **Issue:** Similar to AP Cost, the hit chance score was clamped with a minimum of 0.01f (`Mathf.Clamp(finalHitChance, MIN_HIT, MAX_HIT)` where `MIN_HIT = 0.01f`). This meant out-of-range attacks (actual hit chance 0) still contributed a small score.
    *   **Fix:** Changed the minimum clamp value to `0f`: `Mathf.Clamp(finalHitChance, 0f, MAX_HIT)`.
    *   **Result:** Hit chance score correctly reflects 0 when out of range, but the core issue persisted.

3.  **Utility Score Aggregation (`AttackUtilityAction.CalculateUtility`):**
    *   **Issue:** The `CalculateUtility` method multiplied all non-blocking consideration scores together. A single low score (like `TargetDistance` when slightly outside optimal range, or the clamped `HitChance` of 0.01) drastically reduced the overall `Attack` utility, making `Move` seem preferable.
    *   **Fix:** Changed the aggregation logic from multiplication to averaging the scores of relevant considerations.
    *   **Result:** `Attack` scores became more reasonable, but AI sometimes chose to attack even when hit chance was effectively zero (due to the earlier clamping issue, now fixed).

4.  **Hit Chance Blocking (`AttackUtilityAction.CalculateUtility`):**
    *   **Issue:** After switching to averaging, attacks were sometimes selected even when `HitChanceConsideration` returned the minimum clamped value (0.01), signifying an impossible shot.
    *   **Fix:** Added an explicit check within `CalculateUtility` to immediately return 0 utility if the `HitChanceConsideration` score is less than or equal to 0.001f (later refined, but the principle is to block impossible attacks). This check was added *before* averaging other scores. *(Self-correction: Initially suggested 0.01f, but checking against near-zero is safer)*.
    *   **Result:** Prevented selection of attacks with zero or near-zero hit probability.

5.  **(Related Observation) Move Target Logic (`MoveUtilityAction.cs` & Considerations):**
    *   Identified oscillation behavior (moving towards then away). Adjusted `TargetPositionSafetyConsideration` curve to more heavily penalize proximity to enemies. Modified `DistanceToEnemyConsideration` logic (initially inverted incorrectly, then restored). Ensured `maxDistance` in `DistanceToEnemyConsideration` was sufficient. These helped refine movement but didn't solve the core attack selection problem directly addressed by steps 1-4.

## Final Outcome

After implementing the fixes, particularly the correction of score clamping in considerations and changing the utility aggregation in `AttackUtilityAction` to averaging with a hit chance block, the AI units began selecting `Attack` actions appropriately when within range and AP permitted. Combat proceeded dynamically, with units engaging each other until one was destroyed.

Reference final successful log: `BattleLog_7632acf1_20250504_035144.txt` 