using UnityEngine;

namespace AF.EventBus.Examples
{
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