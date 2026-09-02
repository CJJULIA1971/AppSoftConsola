using AppSoftConsola.Models;
using AppSoftConsola.Services;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Windows;
using System.Windows.Controls;

namespace AppSoftConsola.Views
{
    public partial class CrudUsuariosWindow : Window
    {
        private string dbPath = "Data/pos10.db";
        private List<User> usuarios = new List<User>();

        public CrudUsuariosWindow()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            usuarios.Clear();

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string query = "SELECT UserId, Nombre, Apellido FROM TblUsers";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        usuarios.Add(new User
                        {
                            UserId = reader["UserId"].ToString().ToUpper(),
                            Nombre = reader["Nombre"].ToString(),
                            Apellido = reader["Apellido"].ToString()
                        });
                    }
                }
            }

            lstUsuarios.ItemsSource = null;
            lstUsuarios.ItemsSource = usuarios;
        }

        private void lstUsuarios_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstUsuarios.SelectedItem is User u)
            {
                txtUserId.Text = u.UserId;
                txtNombre.Text = u.Nombre;
                txtApellido.Text = u.Apellido;

                txtUserId.IsEnabled = false; // bloquear UserId

                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {

                  

                    conn.Open();

                    string query = "SELECT PasswordHash, RoleHash FROM TblUsers WHERE UserId=@id";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", u.UserId);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtPassword.Password = "";

                                string roleHash = reader["RoleHash"].ToString();
                                string adminHash = HashService.Hash("ADMIN");

                                if (roleHash == adminHash)
                                    cmbRole.SelectedItem = cmbRole.Items[0]; // ADMIN
                                else
                                    cmbRole.SelectedItem = cmbRole.Items[1]; // USER
                            }
                        }
                    }
                }
            }
        }

        // ============================
        // AGREGAR → SOLO LIMPIA CAMPOS
        // ============================
        private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            LimpiarCampos();
            txtUserId.IsEnabled = true;
            lstUsuarios.SelectedIndex = -1;
        }

        // ============================
        // GUARDAR → INSERT o UPDATE
        // ============================
        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            string userId = txtUserId.Text.Trim().ToUpper();
            string nombre = txtNombre.Text.Trim();
            string apellido = txtApellido.Text.Trim();
            string password = txtPassword.Password.Trim();
            string roleText = (cmbRole.SelectedItem as ComboBoxItem)?.Content?.ToString();

            if (string.IsNullOrWhiteSpace(userId) ||
                string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(apellido))
            {
                MessageBox.Show("Completar todos los campos obligatorios.");
                return;
            }

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // INSERT si NO hay selección
                if (lstUsuarios.SelectedIndex == -1)
                {
                    if (string.IsNullOrWhiteSpace(password) ||
                        string.IsNullOrWhiteSpace(roleText))
                    {
                        MessageBox.Show("Password y Role son obligatorios para agregar.");
                        return;
                    }

                    string passHash = HashService.Hash(password);
                    string roleHash = HashService.Hash(roleText);

                    string checkQuery = "SELECT COUNT(*) FROM TblUsers WHERE UserId=@id";
                    using (var checkCmd = new SQLiteCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@id", userId);
                        long count = (long)checkCmd.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("Ya existe un usuario con ese UserID.");
                            return;
                        }
                    }

                    string insertQuery = @"INSERT INTO TblUsers 
                        (UserId, PasswordHash, RoleHash, Nombre, Apellido)
                        VALUES (@id, @p, @r, @n, @a)";

                    using (var cmd = new SQLiteCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", userId);
                        cmd.Parameters.AddWithValue("@p", passHash);
                        cmd.Parameters.AddWithValue("@r", roleHash);
                        cmd.Parameters.AddWithValue("@n", nombre);
                        cmd.Parameters.AddWithValue("@a", apellido);
                        cmd.ExecuteNonQuery();
                    }
                }
                else
                {
                    // UPDATE
                    User u = (User)lstUsuarios.SelectedItem;

                    string updateQuery = @"UPDATE TblUsers
                        SET Nombre=@n, Apellido=@a
                        WHERE UserId=@id";

                    using (var cmd = new SQLiteCommand(updateQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@n", nombre);
                        cmd.Parameters.AddWithValue("@a", apellido);
                        cmd.Parameters.AddWithValue("@id", u.UserId);
                        cmd.ExecuteNonQuery();
                    }

                    if (!string.IsNullOrWhiteSpace(password))
                    {
                        string passHash = HashService.Hash(password);
                        using (var cmd = new SQLiteCommand(
                            "UPDATE TblUsers SET PasswordHash=@p WHERE UserId=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@p", passHash);
                            cmd.Parameters.AddWithValue("@id", u.UserId);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(roleText))
                    {
                        string roleHash = HashService.Hash(roleText);
                        using (var cmd = new SQLiteCommand(
                            "UPDATE TblUsers SET RoleHash=@r WHERE UserId=@id", conn))
                        {
                            cmd.Parameters.AddWithValue("@r", roleHash);
                            cmd.Parameters.AddWithValue("@id", u.UserId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }

            LimpiarCampos();
            CargarUsuarios();
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (lstUsuarios.SelectedItem is not User u)
                return;

            // Leer RoleHash desde la base para saber si es ADMIN
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string queryRole = "SELECT RoleHash FROM TblUsers WHERE UserId=@id";

                using (var cmdRole = new SQLiteCommand(queryRole, conn))
                {
                    cmdRole.Parameters.AddWithValue("@id", u.UserId);
                    string roleHash = cmdRole.ExecuteScalar()?.ToString();

                    string adminHash = HashService.Hash("ADMIN");

                    if (roleHash == adminHash)
                    {
                        MessageBox.Show("El usuario ADMIN no puede borrarse.");
                        return;
                    }
                }

                // Si no es ADMIN → borrar
                string query = "DELETE FROM TblUsers WHERE UserId=@id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", u.UserId);
                    cmd.ExecuteNonQuery();
                }
            }

            LimpiarCampos();
            CargarUsuarios();
        }

        private void LimpiarCampos()
        {
            txtUserId.Text = "";
            txtNombre.Text = "";
            txtApellido.Text = "";
            txtPassword.Password = "";
            cmbRole.SelectedIndex = -1;

            txtUserId.IsEnabled = true;
        }
    }
}
