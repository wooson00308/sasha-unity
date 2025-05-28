namespace AF.UI
{
    public abstract class BasePresenter<TView> where TView : IView
    {
        protected readonly TView view;

        protected BasePresenter(TView view)
        {
            this.view = view;
        }

        /// <summary>Hook view events → presenter callbacks here.</summary>
        protected virtual void Initialize() { }

        public virtual void Show() => view.Show();
        public virtual void Hide() => view.Hide();
    }
}