namespace AF.UI
{
    public class MainMenuPresenter : BasePresenter<MainMenuView>
    {
        private readonly UIManager ui;

        public MainMenuPresenter(MainMenuView view, UIManager ui) : base(view)
        {
            this.ui = ui;
            Initialize();
        }

        protected override void Initialize()
        {
            view.BriefingClicked += ui.NavigateToBriefingRoom;
            view.HangarClicked   += ui.NavigateToHangar;
        }
    }
}