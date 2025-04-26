using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace AF.UI
{
    public class CondensedLogPanelController : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _logText;
        [SerializeField] private int _maxLogLines = 3; // Inspector에서 최대 줄 수 조절 가능

        private Queue<string> _logQueue = new Queue<string>();

        private void Awake()
        {
            if (_logText == null)
            {
                Debug.LogError("Log Text (TextMeshProUGUI) is not assigned in CondensedLogPanelController!");
                enabled = false; // 컴포넌트 비활성화
            }
        }

        /// <summary>
        /// 패널에 새 로그 메시지를 추가합니다.
        /// </summary>
        public void AddLog(string message)
        {
            if (_logText == null || string.IsNullOrEmpty(message)) return;

            // 너무 긴 메시지 자르기 (선택적)
            // if (message.Length > 100) message = message.Substring(0, 97) + "...";

            if (_logQueue.Count >= _maxLogLines)
            {
                _logQueue.Dequeue();
            }
            _logQueue.Enqueue(message);

            // 큐 내용을 TextMeshPro 텍스트로 업데이트
            _logText.text = string.Join("\n", _logQueue);
        }

        /// <summary>
        /// 로그 패널을 초기화합니다.
        /// </summary>
        public void ClearLog()
        {
             if (_logText == null) return;
             _logQueue.Clear();
             _logText.text = "";
        }
    }
} 