namespace WaitingScreenTerminal;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        Window window = new(new MainPage()) { Title = "WaitingScreenTerminal" };

        const int newWidth = 932;
        const int newHeight = 428;
        
        window.Width = newWidth;
        window.Height = newHeight;
        
        return window;
    }
}