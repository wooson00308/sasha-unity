using System;
using System.Collections.Generic;
using UnityEngine;
using AF.Combat;
using AF.EventBus;

namespace AF.Services
{
    /// <summary>
    /// 유니티 게임 오브젝트로 존재하는 서비스 관리자
    /// 게임 실행 시 필요한 서비스들을 자동으로 등록
    /// </summary>
    public class ServiceManager : MonoBehaviour
    {
        [SerializeField] private bool _dontDestroyOnLoad = true;
        
        // 서비스 로케이터 인스턴스 참조
        private ServiceLocator _serviceLocator;
        
        // 등록할 서비스 인스턴스들 (하이어라키에서 추가 및 설정 가능)
        [SerializeField] private List<MonoBehaviour> _serviceObjects = new List<MonoBehaviour>();
        
        // 종료 순서 관리를 위한 우선순위 (낮은 수가 먼저 종료됨)
        [SerializeField] private bool _useShutdownOrder = false;
        [SerializeField] private List<ShutdownOrderItem> _shutdownOrder = new List<ShutdownOrderItem>();
        
        private void Awake()
        {
            if (_dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
            
            _serviceLocator = ServiceLocator.Instance;
            RegisterServices();
        }
        
        /// <summary>
        /// 인스펙터에 등록된 서비스 오브젝트들을 서비스 로케이터에 등록
        /// </summary>
        private void RegisterServices()
        {
            RegisterCoreServices();

            foreach (var serviceObject in _serviceObjects)
            {
                if (serviceObject is IService service)
                {
                    // 서비스 타입을 검색해서 가장 적합한 인터페이스 타입으로 등록
                    RegisterServiceWithInterface(service, serviceObject.GetType());
                }
                else
                {
                    Debug.LogError($"{serviceObject.name}은(는) IService를 구현하지 않았습니다.");
                }
            }
        }
        
        /// <summary>
        /// 서비스 객체를 가장 적합한 인터페이스 타입으로 등록
        /// </summary>
        private void RegisterServiceWithInterface(IService service, System.Type concreteType)
        {
            // 모든 인터페이스를 가져와서 IService를 구현한 것 찾기
            var interfaces = concreteType.GetInterfaces();
            
            foreach (var interfaceType in interfaces)
            {
                if (interfaceType != typeof(IService) && typeof(IService).IsAssignableFrom(interfaceType))
                {
                    // 리플렉션을 사용하여 RegisterService<T>를 올바른 타입으로 호출
                    var method = typeof(ServiceLocator).GetMethod("RegisterService");
                    var genericMethod = method.MakeGenericMethod(interfaceType);
                    genericMethod.Invoke(_serviceLocator, new object[] { service });
                    
                    return;
                }
            }
            
            // 적절한 인터페이스를 찾지 못했을 경우 구체 타입으로 등록
            var registerMethod = typeof(ServiceLocator).GetMethod("RegisterService");
            var genericRegisterMethod = registerMethod.MakeGenericMethod(concreteType);
            genericRegisterMethod.Invoke(_serviceLocator, new object[] { service });
        }
        
        /// <summary>
        /// 기본 서비스들을 등록합니다.
        /// </summary>
        private void RegisterCoreServices()
        {
            // 이벤트 버스 서비스 등록
            ServiceLocator.Instance.RegisterService(new EventBusService());
            
            // 텍스트 로거 서비스 등록
            ServiceLocator.Instance.RegisterService(new TextLoggerService());

            // 전투 시뮬레이터 서비스 등록 (인터페이스 타입으로 등록)
            ServiceLocator.Instance.RegisterService<ICombatSimulatorService>(new CombatSimulatorService());
        }
        
        private void OnDestroy()
        {
            if (_useShutdownOrder && _shutdownOrder.Count > 0)
            {
                // 종료 우선순위에 따라 서비스 순차 종료
                _shutdownOrder.Sort((a, b) => a.shutdownPriority.CompareTo(b.shutdownPriority));
                
                foreach (var item in _shutdownOrder)
                {
                    if (item.serviceType != null)
                    {
                        // 리플렉션을 사용하여 RemoveService<T>를 올바른 타입으로 호출
                        try
                        {
                            var method = typeof(ServiceLocator).GetMethod("RemoveService");
                            var genericMethod = method.MakeGenericMethod(item.serviceType);
                            genericMethod.Invoke(_serviceLocator, null);
                        }
                        catch (System.Exception ex)
                        {
                            Debug.LogError($"서비스 {item.serviceType.Name} 종료 중 오류 발생: {ex.Message}");
                        }
                    }
                }
                
                // 남은 서비스 정리
                _serviceLocator.ClearAllServices();
            }
            else
            {
                // 우선순위가 없는 경우 모든 서비스 한 번에 정리
                _serviceLocator.ClearAllServices();
            }
        }
        
        #if UNITY_EDITOR
        // 에디터에서 사용할 유틸리티 메서드 (선택적)
        /// <summary>
        /// 에디터 전용: 서비스 오브젝트 목록에 새 서비스를 추가
        /// </summary>
        public void AddService(MonoBehaviour service)
        {
            if (service is IService)
            {
                _serviceObjects.Add(service);
            }
            else
            {
                Debug.LogError($"{service.name}은(는) IService를 구현하지 않았습니다.");
            }
        }
        #endif
    }
} 