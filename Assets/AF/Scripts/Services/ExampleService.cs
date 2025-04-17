using UnityEngine;

namespace AF.Services.Examples
{
    /// <summary>
    /// 서비스 로케이터 패턴 사용 예제를 위한 간단한 서비스 구현
    /// </summary>
    public class ExampleService : MonoBehaviour, IExampleService
    {
        [SerializeField] private string _serviceName = "Example Service";
        private bool _isInitialized = false;
        
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public void Initialize()
        {
            Debug.Log($"{_serviceName} 초기화 완료!");
            _isInitialized = true;
        }
        
        /// <summary>
        /// 서비스 종료 및 리소스 정리
        /// </summary>
        public void Shutdown()
        {
            Debug.Log($"{_serviceName} 종료 및 리소스 정리 완료!");
            _isInitialized = false;
        }
        
        /// <summary>
        /// 서비스 예제 메서드
        /// </summary>
        public void DoSomething()
        {
            if (!_isInitialized)
            {
                Debug.LogWarning($"{_serviceName}가 초기화되지 않았습니다. 기능이 제한될 수 있습니다.");
            }
            
            Debug.Log($"{_serviceName}가 무언가를 수행합니다!");
        }
        
        /// <summary>
        /// 서비스 상태 확인
        /// </summary>
        /// <returns>현재 서비스 상태</returns>
        public string GetServiceInfo()
        {
            return _isInitialized 
                ? $"{_serviceName}는 활성화되어 있으며 정상 작동 중입니다." 
                : $"{_serviceName}가 아직 초기화되지 않았습니다.";
        }
    }
} 