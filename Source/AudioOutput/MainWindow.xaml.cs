using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AudioOutput
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                this.DataContext = new ViewModel();
            }catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                this.Close();
            }            

            this.Closed += MainWindow_Closed;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (this.DataContext is IDisposable)
            {
                (this.DataContext as IDisposable).Dispose();
            }
        }

        private void canvas1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewModel = (ViewModel)this.DataContext;
            viewModel.HandleCanvasMouseClick(this.canvas1, e.GetPosition(canvas1), e);
        }
    }
}