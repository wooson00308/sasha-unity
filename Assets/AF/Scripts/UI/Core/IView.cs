namespace AF.UI
{
    /// <summary>Lightweight contract every view must fulfil.</summary>
    public interface IView
    {
        void Show();
        void Hide();
    }
}