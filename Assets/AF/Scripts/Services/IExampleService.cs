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
} 