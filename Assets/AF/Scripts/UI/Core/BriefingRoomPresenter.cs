using UnityEngine;

namespace AF.UI
{
    public class BriefingRoomPresenter : BasePresenter<BriefingRoomView>
    {
        private readonly UIManager ui;

        public BriefingRoomPresenter(BriefingRoomView view, UIManager ui) : base(view)
        {
            this.ui = ui;
            Initialize();
        }

        protected override void Initialize()
        {
            view.BackClicked   += ui.NavigateToMainMenu;
            view.EngageClicked += OnEngage;
        }

        private void OnEngage()
        {
            // TODO: plug into combat launch once runtime hooks are ready.
            Debug.Log("[UI] Engage pressed → Launch combat here");
            ui.NavigateToCombat();
        }
    }
}