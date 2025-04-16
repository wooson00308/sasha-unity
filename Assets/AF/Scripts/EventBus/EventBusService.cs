using UnityEngine;
using AF.Services;

namespace AF.EventBus
{
    /// <summary>
    /// 이벤트 버스를 서비스로 제공하는 클래스
    /// ServiceLocator를 통해 접근할 수 있는 서비스로 작동합니다.
    /// </summary>
    public class EventBusService : IService
    {
        private EventBus _eventBus;
        
        /// <summary>
        /// EventBus 인스턴스에 대한 접근자
        /// </summary>
        public EventBus Bus => _eventBus;
        
        /// <summary>
        /// 서비스 초기화 메서드
        /// </summary>
        public void Initialize()
        {
            _eventBus = new EventBus();
            Debug.Log("[EventBusService] 초기화 완료");
        }
        
        /// <summary>
        /// 서비스 종료 메서드
        /// </summary>
        public void Shutdown()
        {
            if (_eventBus != null)
            {
                _eventBus.Clear();
                _eventBus = null;
                Debug.Log("[EventBusService] 종료됨");
            }
        }
    }
    
    /// <summary>
    /// EventBusService 확장 메서드
    /// </summary>
    public static class EventBusExtensions
    {
        /// <summary>
        /// 서비스 로케이터를 통해 이벤트 버스에 접근하는 간편한 방법을 제공합니다.
        /// </summary>
        public static EventBus Events(this ServiceLocator locator)
        {
            return locator.GetService<EventBusService>().Bus;
        }
    }
} 