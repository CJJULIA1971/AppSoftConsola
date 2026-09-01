using System.Data.SQLite;
using System.Windows;



namespace AppSoftConsola.Views
{
    public partial class ParametrosWindow : Window
    {
        private string dbPath = "Data/pos10.db";

        public ParametrosWindow()
        {
            InitializeComponent();
            CargarParametros();
        }

        private void CargarParametros()
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string query = "SELECT * FROM TblParameters LIMIT 1";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        txtPAPuntoVenta.Text = reader["PAPuntoVenta"].ToString();
                        txtPAPortUSB.Text = reader["PAPortUSB"].ToString();
                        txtPANameTicketLine1.Text = reader["PANameTicketLine1"].ToString();
                        txtPANameTicketLine2.Text = reader["PANameTicketLine2"].ToString();
                        txtPANombreComercio.Text = reader["PANombreComercio"].ToString();
                    }
                }
            }
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string query = @"
                UPDATE TblParameters SET
                PAPuntoVenta=@pv,
                PAPortUSB=@usb,
                PANameTicketLine1=@l1,
                PANameTicketLine2=@l2,
                PANombreComercio=@nc                        
        ";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@pv", txtPAPuntoVenta.Text.Trim());
                    cmd.Parameters.AddWithValue("@usb", txtPAPortUSB.Text.Trim());
                    cmd.Parameters.AddWithValue("@l1", txtPANameTicketLine1.Text.Trim());
                    cmd.Parameters.AddWithValue("@l2", txtPANameTicketLine2.Text.Trim());
                    cmd.Parameters.AddWithValue("@nc", txtPANombreComercio.Text.Trim());                                      
                    cmd.ExecuteNonQuery();
                }
            }

            //ACA VAN LAS ACCIONES PARA INICIALIZAR LOS PARAMETROS 
            ((MainWindow)Application.Current.MainWindow).NotifyMainWindowToRefresh();
            MessageBox.Show("Parámetros guardados correctamente.");
            this.Close();
        }

                private void Inicializar_Click(object sender, RoutedEventArgs e)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                // ⭐ 1. BORRAR HISTORIAL DE TICKETS
                string deleteHistory = "DELETE FROM TblProductTicketHistory";
                using (var cmd = new SQLiteCommand(deleteHistory, conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // ⭐ 2. REINICIAR NÚMERO DE TICKET EN TblParameters
                string resetTicket = "UPDATE TblParameters SET PALastTicketNumber = 1";
                using (var cmd = new SQLiteCommand(resetTicket, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

            //ACA VAN LAS ACCIONES PARA INICIALIZAR LOS PARAMETROS 
            ((MainWindow)Application.Current.MainWindow).NotifyMainWindowToRefresh();
            MessageBox.Show("Parámetros guardados correctamente.");
            this.Close();
        }


        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
