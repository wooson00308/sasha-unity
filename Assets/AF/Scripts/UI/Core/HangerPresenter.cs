using UnityEngine;

namespace AF.UI
{
    public class HangarPresenter : BasePresenter<HangarView>
    {
        private readonly UIManager ui;

        public HangarPresenter(HangarView view, UIManager ui) : base(view)
        {
            this.ui = ui;
            Initialize();
        }

        protected override void Initialize()
        {
            view.BackClicked      += ui.NavigateToMainMenu;
            view.RepairAllClicked += OnRepairAll;
        }

        private void OnRepairAll()
        {
            // TODO: call repair‑service later.
            Debug.Log("[UI] Repair All triggered");
        }
    }
}