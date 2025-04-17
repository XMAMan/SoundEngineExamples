using System.Windows;

namespace AudioInput
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new ViewModel();

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (this.DataContext is IDisposable)
            {
                (this.DataContext as IDisposable).Dispose();
            }
        }
    }
}