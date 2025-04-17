using UnityEngine;

namespace AF.Services.Examples
{
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