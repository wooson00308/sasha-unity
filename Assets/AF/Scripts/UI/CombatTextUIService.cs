using AF.Combat;
using AF.EventBus;
using AF.Models;
using AF.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // List 사용 위해 추가
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System; // TimeSpan 사용 위해 추가
using System.Linq;

namespace AF.UI
{
    /// <summary>
    /// 전투 로그를 UI에 표시하는 서비스 (전투 종료 시 한 번에 표시)
    /// </summary>
    public class CombatTextUIService : MonoBehaviour, IService
    {
        [Header("UI References")] 
        // 기존 _logTextDisplay는 프리팹 방식으로 대체될 수 있으므로 주석 처리 또는 제거 고려
        // [SerializeField] private TMP_Text _logTextDisplay; 
        [SerializeField] private ScrollRect _scrollRect; // 자동 스크롤용
        [SerializeField] private GameObject _logLinePrefab; // 로그 한 줄을 표시할 프리팹
        [SerializeField] private Transform _logContainer; // 프리팹이 생성될 부모 컨테이너 (VerticalLayoutGroup 등)

        [Header("Animation Settings")]
        [SerializeField] private float _fadeInDuration = 0.3f; // 각 라인 페이드인 시간
        [SerializeField] private float _lineDelay = 0.1f; // 다음 라인 표시 전 딜레이

        private EventBus.EventBus _eventBus;
        // StringBuilder 와 라인 카운트는 프리팹 방식에서는 관리 방식 변경 필요
        // private System.Text.StringBuilder _logBuilder = new System.Text.StringBuilder();
        // private int _currentLogLines = 0;
        // private int _maxLogLines = 1000; // 최대 라인 수 제한 로직도 변경 필요 (오브젝트 삭제 등)
        private bool _isInitialized = false;
        
        #region IService Implementation

        public void Initialize()
        {
            if (_isInitialized) return;

            _eventBus = ServiceLocator.Instance.GetService<EventBusService>()?.Bus;
            if (_eventBus == null)
            {
                Debug.LogError("CombatTextUIService 초기화 실패: EventBus 참조를 가져올 수 없습니다.");
                return;
            }
            if (_logLinePrefab == null || _logContainer == null || _scrollRect == null)
            {
                Debug.LogError("CombatTextUIService 초기화 실패: 필요한 UI 참조(프리팹, 컨테이너, 스크롤렉트)가 설정되지 않았습니다.");
                return;
            }

            SubscribeToEvents();
            ClearLog(); 
            _isInitialized = true;
        }

        public void Shutdown()
        {
            if (!_isInitialized) return;
            UnsubscribeFromEvents();
            _eventBus = null;
            _isInitialized = false;
        }

        #endregion

        #region Event Handling

        private void SubscribeToEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Subscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            // HandleCombatEnd는 void 유지, 내부에서 비동기 메서드 호출
            _eventBus.Subscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
        }

        private void UnsubscribeFromEvents()
        {
            if (_eventBus == null) return;
            _eventBus.Unsubscribe<CombatSessionEvents.CombatStartEvent>(HandleCombatStart);
            _eventBus.Unsubscribe<CombatSessionEvents.CombatEndEvent>(HandleCombatEnd);
        }

        private void HandleCombatStart(CombatSessionEvents.CombatStartEvent evt)
        {
            ClearLog();
        }

        // HandleCombatEnd는 void 유지
        private void HandleCombatEnd(CombatSessionEvents.CombatEndEvent evt)
        {
            // 내부에서 비동기 처리 메서드 호출 (Fire-and-forget)
            ProcessLogsAsync(evt).Forget();
        }

        // 로그 처리 로직을 별도 async UniTaskVoid 메서드로 분리
        private async UniTaskVoid ProcessLogsAsync(CombatSessionEvents.CombatEndEvent evt)
        {
            ITextLogger textLogger = ServiceLocator.Instance.GetService<TextLoggerService>()?.TextLogger;
            if (textLogger == null)
            {
                Debug.LogError("CombatTextUIService: TextLogger 참조를 가져올 수 없습니다.");
                await CreateAndAnimateLogLine("오류: 전투 로그를 불러올 수 없습니다.");
                return;
            }

            List<string> combatLogs = textLogger.GetLogs();
            if (combatLogs != null)
            {
                foreach (string logEntry in combatLogs)
                {
                    await CreateAndAnimateLogLine(logEntry);
                }
            }
        }

        #endregion

        #region Logging Methods

        // 기존 AppendLog 대신 프리팹 생성 및 애니메이션 함수
        private async UniTask CreateAndAnimateLogLine(string message)
        {
            if (_logLinePrefab == null || _logContainer == null || _scrollRect == null) return;

            // 1. 프리팹 인스턴스화 및 컨테이너에 추가
            GameObject logInstance = Instantiate(_logLinePrefab, _logContainer);
            
            // 2. 텍스트 설정 (프리팹 내 TMP_Text 컴포넌트 가정)
            TMP_Text logText = logInstance.GetComponent<TMP_Text>(); // 자식에서 찾기 (더 안전)
            if (logText != null)
            {
                logText.text = message;
            }
            else
            {
                 Debug.LogWarning("로그 라인 프리팹에 TMP_Text 컴포넌트를 찾을 수 없습니다.");
            }

            // 3. CanvasGroup 가져오기 및 초기화 (페이드인 준비)
            CanvasGroup canvasGroup = logInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null) // 없다면 추가 (선택적)
            {
                canvasGroup = logInstance.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = 0f; // 처음엔 투명하게

            // 4. DOTween 페이드인 애니메이션 실행 및 완료 대기 (AsyncWaitForCompletion 사용)
            await canvasGroup.DOFade(1f, _fadeInDuration).AsyncWaitForCompletion();

            // 5. 스크롤 맨 아래로 이동
            await UniTask.Yield(PlayerLoopTiming.LastUpdate);
            if (_scrollRect != null) // 스크롤 렉트 null 체크 추가
            {
                 _scrollRect.verticalNormalizedPosition = 0f;
            }

            // 6. 다음 라인 전 딜레이 (TimeSpan 사용)
            if (_lineDelay > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(_lineDelay));
            }
        }


        public void ClearLog()
        {
            if (_logContainer == null) return;

            // 컨테이너의 모든 자식(로그 라인 인스턴스) 삭제
            foreach (Transform child in _logContainer)
            {
                Destroy(child.gameObject);
            }

            // 스크롤 위치 초기화 (선택적)
            if (_scrollRect != null)
            {
                 _scrollRect.normalizedPosition = new Vector2(0, 1); // 맨 위로
            }
        }

        #endregion
    }
} 