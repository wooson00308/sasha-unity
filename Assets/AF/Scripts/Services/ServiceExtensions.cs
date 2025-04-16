using UnityEngine;

namespace AF.Services
{
    /// <summary>
    /// ServiceLocator 클래스에 대한 확장 메서드 모음
    /// 서비스 검색 및 사용을 더 편리하게 만드는 유틸리티 메서드들을 제공합니다.
    /// </summary>
    public static class ServiceExtensions    {
        /// <summary>
        /// MonoBehaviour에서 서비스를 쉽게 가져오기 위한 확장 메서드
        /// </summary>
        /// <typeparam name="T">가져올 서비스 타입</typeparam>
        /// <param name="component">확장 메서드 호출 컴포넌트</param>
        /// <returns>요청한 타입의 서비스 인스턴스</returns>
        public static T GetService<T>(this MonoBehaviour component) where T : IService
        {
            return ServiceLocator.Instance.GetService<T>();
        }
        
        /// <summary>
        /// 서비스가 등록되어 있는지 확인하는 확장 메서드
        /// </summary>
        /// <typeparam name="T">확인할 서비스 타입</typeparam>
        /// <param name="component">확장 메서드 호출 컴포넌트</param>
        /// <returns>서비스 등록 여부</returns>
        public static bool HasService<T>(this MonoBehaviour component) where T : IService
        {
            return ServiceLocator.Instance.HasService<T>();
        }
        
        /// <summary>
        /// 서비스를 안전하게 가져오는 확장 메서드 (없으면 null 반환)
        /// </summary>
        /// <typeparam name="T">가져올 서비스 타입</typeparam>
        /// <param name="component">확장 메서드 호출 컴포넌트</param>
        /// <returns>서비스 인스턴스 또는 null</returns>
        public static T TryGetService<T>(this MonoBehaviour component) where T : IService
        {
            try
            {
                return ServiceLocator.Instance.GetService<T>();
            }
            catch
            {
                return default;
            }
        }
    }
} 