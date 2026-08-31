using System.Windows;

namespace AppSoftConsola.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenParametros(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abrir parámetros");
            // var win = new ParametrosWindow();
            // win.ShowDialog();
        }

        private void OpenAdminLogin(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Abrir login administrador");
            // var win = new AdminLoginWindow();
            // win.ShowDialog();
        }

        private void OpenCrudProductos(object sender, RoutedEventArgs e)
        {
            var win = new CrudProductosWindow();
            win.ShowDialog();
        }

    }
}