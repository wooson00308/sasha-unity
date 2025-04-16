using UnityEngine;

namespace AF.Services.Examples
{
    /// <summary>
    /// 서비스 로케이터 패턴 예제를 위한 샘플 서비스 인터페이스
    /// </summary>
    public interface IExampleService : IService
    {
        void DoSomething();
        string GetServiceInfo();
    }

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
    
    /// <summary>
    /// 서비스 사용 예제
    /// </summary>
    public class ServiceConsumerExample : MonoBehaviour
    {
        private void Start()
        {
            // 서비스가 이미 등록되어 있는지 확인
            if (this.HasService<IExampleService>())
            {
                // 확장 메서드를 사용하여 서비스 가져오기
                IExampleService exampleService = this.GetService<IExampleService>();
                
                // 서비스 사용
                exampleService.DoSomething();
                Debug.Log(exampleService.GetServiceInfo());
            }
            else
            {
                Debug.LogWarning("IExampleService가 등록되어 있지 않습니다.");
                
                // 또는 예외 발생 없이 안전하게 서비스 가져오기 시도
                IExampleService service = this.TryGetService<IExampleService>();
                
                if (service != null)
                {
                    service.DoSomething();
                }
            }
        }
        
        private void OnDestroy()
        {
            // 애플리케이션 종료 시 또는 씬 전환 시 서비스 직접 정리 예시
            if (this.HasService<IExampleService>())
            {
                // 필요한 경우 특정 서비스만 제거할 수 있음
                // ServiceLocator.Instance.RemoveService<IExampleService>();
            }
        }
    }
} 