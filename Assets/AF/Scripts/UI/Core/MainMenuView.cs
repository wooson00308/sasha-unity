using UnityEngine;
using UnityEngine.UI;
using System;

namespace AF.UI
{
    public class MainMenuView : BaseView
    {
        [SerializeField] private Button briefingRoomButton;
        [SerializeField] private Button hangarButton;

        public event Action BriefingClicked;
        public event Action HangarClicked;

        private void Awake()
        {
            briefingRoomButton.onClick.AddListener(() => BriefingClicked?.Invoke());
            hangarButton.onClick.AddListener(() => HangarClicked?.Invoke());
        }
    }
}