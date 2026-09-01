using AppSoftConsola.ViewModels;
using System.Windows;
using System.Windows.Media;

namespace AppSoftConsola.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            txtRoleDisplay.Text = $"ROL: {GlobalState.CurrentUserRole}";
            
            if (GlobalState.CurrentUserRole == "ADMIN")
                txtRoleDisplay.Foreground = new SolidColorBrush(Colors.Red);
            else
                txtRoleDisplay.Foreground = new SolidColorBrush(Colors.Green);

            if (GlobalState.CurrentUserRole != "ADMIN")
            {
               // btnAdmin.Visibility = Visibility.Collapsed;
               // btnParametros.Visibility = Visibility.Collapsed;
            }

        }

        public void NotifyMainWindowToRefresh()
        {
            if (DataContext is MainViewModel vm)
            {
                vm.RecargarParametros();   // refresca TicketNro y PtoVta
                vm.CargarTotalesCaja();    // refresca EF / MP
            }
        }

        private void OpenCrudProductos(object sender, RoutedEventArgs e)
        {
            var win = new CrudProductosWindow();
            win.ShowDialog();
        }
        private void OpenParametros(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Abrir parámetros");
            var win = new ParametrosWindow();
            win.ShowDialog();
        }
        private void OpenAdminLogin(object sender, RoutedEventArgs e)
        {
           // if (GlobalState.CurrentUserRole != "ADMIN")
           // {
           //     MessageBox.Show("Solo un administrador puede acceder a estas funciones.");
           //     return;
           // }
            var win = new CrudUsuariosWindow();
            win.ShowDialog();
        }

        private void AbrirParametros_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalState.CurrentUserRole != "ADMIN")
            {
                MessageBox.Show("Solo un administrador puede acceder a Parámetros.");
                return;
            }

            new ParametrosWindow().ShowDialog();
        }
        


    }
}