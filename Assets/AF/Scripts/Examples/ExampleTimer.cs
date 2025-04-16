using UnityEngine;
using System;

namespace AF.Examples
{
    /// <summary>
    /// Example timer utility that demonstrates basic timer functionality.
    /// </summary>
    public class ExampleTimer : MonoBehaviour
    {
        private float currentTime;
        private bool isRunning;
        private Action onTimerComplete;

        public bool IsRunning => isRunning;
        public float CurrentTime => currentTime;

        public void StartTimer(float duration, Action onComplete = null)
        {
            currentTime = duration;
            isRunning = true;
            onTimerComplete = onComplete;
        }

        public void PauseTimer()
        {
            isRunning = false;
        }

        public void ResumeTimer()
        {
            isRunning = true;
        }

        public void StopTimer()
        {
            isRunning = false;
            currentTime = 0f;
            onTimerComplete = null;
        }

        private void Update()
        {
            if (!isRunning) return;

            currentTime -= Time.deltaTime;

            if (currentTime <= 0f)
            {
                currentTime = 0f;
                isRunning = false;
                onTimerComplete?.Invoke();
            }
        }
    }
} 