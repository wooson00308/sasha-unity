using AF.Services;
using UnityEngine;

namespace AF.Combat
{
    /// <summary>
    /// TextLogger 서비스를 관리하는 서비스 클래스
    /// 서비스 로케이터 패턴에 의해 등록됩니다.
    /// </summary>
    public class TextLoggerService : IService
    {
        private TextLogger _textLogger;
        
        /// <summary>
        /// TextLogger 인스턴스에 대한 퍼블릭 접근자
        /// </summary>
        public ITextLogger TextLogger => _textLogger;
        
        /// <summary>
        /// 서비스 초기화
        /// </summary>
        public void Initialize()
        {
            _textLogger = new TextLogger();
            
            // TextLogger 초기화
            _textLogger.Initialize();
            
            Debug.Log("TextLoggerService가 초기화되었습니다.");
        }

        /// <summary>
        /// 서비스 종료
        /// </summary>
        public void Shutdown()
        {
            if (_textLogger != null)
            {
                _textLogger.Shutdown();
                _textLogger = null;
            }
            
            Debug.Log("TextLoggerService가 종료되었습니다.");
        }
    }
} 