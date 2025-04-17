using AF.Services;

namespace AF.EventBus
{
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