using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace AF.UI
{
    public class BriefingRoomView : BaseView
    {
        [SerializeField] private TextMeshProUGUI missionTitleText;
        [SerializeField] private Button engageButton;
        [SerializeField] private Button backButton;

        public event Action EngageClicked;
        public event Action BackClicked;

        private void Awake()
        {
            engageButton.onClick.AddListener(() => EngageClicked?.Invoke());
            backButton.onClick.AddListener(() => BackClicked?.Invoke());
        }

        public void SetMissionTitle(string title) => missionTitleText.text = title;
    }
}