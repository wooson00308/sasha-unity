using UnityEngine;

namespace AF.UI
{
    /// <summary>Single MonoBehaviour that owns presenters & pushes simple screen navigation.</summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Views – drag your prefabs here")]        
        [SerializeField] private MainMenuView mainMenuView;
        [SerializeField] private BriefingRoomView briefingRoomView;
        [SerializeField] private HangarView hangarView;

        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private Canvas combatCanvas;

        private MainMenuPresenter mainMenuPresenter;
        private BriefingRoomPresenter briefingRoomPresenter;
        private HangarPresenter hangarPresenter;

        private void Awake()
        {
            // Build presenters – they’re lightweight, recreating is fine.
            mainMenuPresenter      = new MainMenuPresenter(mainMenuView, this);
            briefingRoomPresenter  = new BriefingRoomPresenter(briefingRoomView, this);
            hangarPresenter        = new HangarPresenter(hangarView, this);

            HideAll();
            mainMenuPresenter.Show();
        }

        private void HideAll()
        {
            mainMenuPresenter.Hide();
            briefingRoomPresenter.Hide();
            hangarPresenter.Hide();
        }

        #region Navigation API (called by presenters)

        public void NavigateToMainMenu()
        {
            HideAll();
            mainMenuPresenter.Show();
        }

        public void NavigateToBriefingRoom()
        {
            HideAll();
            briefingRoomPresenter.Show();
        }

        public void NavigateToHangar()
        {
            HideAll();
            hangarPresenter.Show();
        }

        public void NavigateToCombat()
        {
            HideAll();
            mainCanvas.gameObject.SetActive(false);
            combatCanvas.gameObject.SetActive(true);
        }

        public void NavigateToMain()
        {
            HideAll();
            mainCanvas.gameObject.SetActive(true);
            combatCanvas.gameObject.SetActive(false);
            mainMenuPresenter.Show();
        }

        #endregion
    }
}