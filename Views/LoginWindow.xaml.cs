using System.Windows;
using AppSoftConsola.Services;
using AppSoftConsola.Models;

namespace AppSoftConsola.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Ingresar_Click(object sender, RoutedEventArgs e)
        {
            string userId = txtUserId.Text.Trim().ToUpper();
            string password = txtPassword.Password.Trim();

            var user = AuthService.GetUserById(userId);

            if (user == null)
            {
                MessageBox.Show("Usuario incorrecto");
                return;
            }

            string passHash = HashService.Hash(password);

            if (user.PasswordHash != passHash)
            {
                MessageBox.Show("Contraseña incorrecta");
                return;
            }

            string adminHash = HashService.Hash("ADMIN");
            GlobalState.CurrentUserRole = (user.RoleHash == adminHash) ? "ADMIN" : "USER";
            GlobalState.CurrentUser = user;

            new MainWindow().Show();
            this.Close();
        }

    }
}
