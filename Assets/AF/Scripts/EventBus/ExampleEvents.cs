using UnityEngine;

namespace AF.EventBus.Examples
{
    /// <summary>
    /// 플레이어와 관련된 샘플 이벤트 정의
    /// </summary>
    public class PlayerEvents
    {
        /// <summary>
        /// 플레이어 체력 변경 이벤트
        /// </summary>
        public class HealthChanged : IEvent
        {
            public int CurrentHealth { get; private set; }
            public int MaxHealth { get; private set; }
            public int Delta { get; private set; }

            public HealthChanged(int currentHealth, int maxHealth, int delta)
            {
                CurrentHealth = currentHealth;
                MaxHealth = maxHealth;
                Delta = delta;
            }
        }

        /// <summary>
        /// 플레이어 레벨업 이벤트
        /// </summary>
        public class LevelUp : IEvent
        {
            public int NewLevel { get; private set; }
            public int StatPointsGained { get; private set; }

            public LevelUp(int newLevel, int statPointsGained)
            {
                NewLevel = newLevel;
                StatPointsGained = statPointsGained;
            }
        }
        
        /// <summary>
        /// 플레이어 위치 변경 이벤트
        /// </summary>
        public class PositionChanged : IEvent
        {
            public Vector3 NewPosition { get; private set; }
            public Vector3 OldPosition { get; private set; }

            public PositionChanged(Vector3 newPosition, Vector3 oldPosition)
            {
                NewPosition = newPosition;
                OldPosition = oldPosition;
            }
        }
    }

    /// <summary>
    /// 게임 시스템과 관련된 샘플 이벤트 정의
    /// </summary>
    public class GameEvents
    {
        /// <summary>
        /// 게임 상태 변경 이벤트
        /// </summary>
        public class GameStateChanged : IEvent
        {
            public enum State { MainMenu, Loading, Playing, Paused, GameOver }
            
            public State PreviousState { get; private set; }
            public State CurrentState { get; private set; }

            public GameStateChanged(State previousState, State currentState)
            {
                PreviousState = previousState;
                CurrentState = currentState;
            }
        }
        
        /// <summary>
        /// 씬 전환 이벤트
        /// </summary>
        public class SceneTransition : IEvent
        {
            public string PreviousScene { get; private set; }
            public string NewScene { get; private set; }
            public float LoadProgress { get; private set; }

            public SceneTransition(string previousScene, string newScene, float loadProgress = 0f)
            {
                PreviousScene = previousScene;
                NewScene = newScene;
                LoadProgress = loadProgress;
            }
        }
    }
} 