using UnityEngine;
using UnityEngine.UI;
using System;

namespace AF.UI
{
    public class HangarView : BaseView
    {
        [SerializeField] private Button repairAllButton;
        [SerializeField] private Button backButton;

        public event Action RepairAllClicked;
        public event Action BackClicked;

        private void Awake()
        {
            repairAllButton.onClick.AddListener(() => RepairAllClicked?.Invoke());
            backButton.onClick.AddListener(() => BackClicked?.Invoke());
        }
    }
}