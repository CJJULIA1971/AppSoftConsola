using System;
using System.Collections.Generic;
using AppSoftConsola.Models;
using System.Linq;
using AppSoftConsola.ViewModels;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.SQLite;

namespace AppSoftConsola.Views
{

     public partial class CrudProductosWindow : Window
    {
        private string dbPath = "Data/pos10.db";
        private List<Producto> productos = new List<Producto>();

        public CrudProductosWindow()
        {
            InitializeComponent();
            CargarProductos();
        }

        // Load products from DB
        private void CargarProductos()
        {
            productos.Clear();

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string query = "SELECT PLCode, PLDescription, PLAmount, PLOrderShow FROM TblProductList ORDER BY PLOrderShow";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        productos.Add(new Producto(
                            Convert.ToInt32(reader["PLCode"]),
                            reader["PLDescription"].ToString(),
                            Convert.ToDecimal(reader["PLAmount"]),
                            Convert.ToInt32(reader["PLOrderShow"])));
                    }
                }
            }

            MostrarLista(productos);
        }







        private void MostrarLista(IEnumerable<Producto> lista)
        {
            lstProductos.ItemsSource = null;
            lstProductos.ItemsSource = lista.ToList();
        }

        // Search filter
        private void txtBuscar_TextChanged(object sender, TextChangedEventArgs e)
        {
            string q = txtBuscar.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(q))
            {
                MostrarLista(productos);
                return;
            }

            var filtered = productos.Where(p => p.Description?.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            MostrarLista(filtered);
        }

        private void lstProductos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstProductos.SelectedItem is Producto p)
            {
                txtCode.Text = p.Code.ToString();
                txtDesc.Text = p.Description;
                txtAmount.Text = p.Amount.ToString("0.##");
                txtOrder.Text = p.OrderShow.ToString();
                // When an existing product is selected, make the Code field read-only and adjust color
                txtCode.IsReadOnly = true;
                txtCode.Foreground = System.Windows.SystemColors.GrayTextBrush;
            }
        }
                private void Agregar_Click(object sender, RoutedEventArgs e)
        {
            txtCode.Text = string.Empty;
            txtDesc.Text = string.Empty;
            txtAmount.Text = string.Empty;
            txtOrder.Text = string.Empty;
            lstProductos.SelectedItem = null;
            // Creating a new product: allow editing the Code field
            txtCode.IsReadOnly = false;
            txtCode.Foreground = System.Windows.Media.Brushes.DodgerBlue;
            // Update list views after action
            NotifyMainWindowToRefresh();
        }

        private void Guardar_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("¿Desea guardar este producto?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            if (!int.TryParse(txtCode.Text, out int code))
            {
                MessageBox.Show("Ingrese un Código numérico válido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string desc = txtDesc.Text ?? string.Empty;
            if (string.IsNullOrWhiteSpace(desc))
            {
                MessageBox.Show("Ingrese una Descripción.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(txtAmount.Text, out decimal amount))
            {
                MessageBox.Show("Ingrese un Precio válido (número).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtOrder.Text, out int order))
            {
                MessageBox.Show("Ingrese un Orden válido (número entero).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    // If creating a new product (no selection), ensure code is unique
                    bool isCreatingNew = lstProductos.SelectedItem == null;
                    if (isCreatingNew)
                    {
                        using (var chk = new SQLiteCommand("SELECT COUNT(1) FROM TblProductList WHERE PLCode = @code", conn))
                        {
                            chk.Parameters.AddWithValue("@code", code);
                            var exists = Convert.ToInt32(chk.ExecuteScalar()) > 0;
                            if (exists)
                            {
                                MessageBox.Show("El código ya existe. Ingrese otro código.", "Código duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    // Ensure PLCode has a uniqueness constraint at DB level to avoid duplicates
                    using (var idxCmd = new SQLiteCommand("CREATE UNIQUE INDEX IF NOT EXISTS idx_TblProductList_PLCode ON TblProductList(PLCode);", conn))
                    {
                        try { idxCmd.ExecuteNonQuery(); } catch { /* ignore if index creation fails */ }
                    }

                    // Use INSERT for new records and UPDATE for existing ones to avoid accidental duplicate rows
                    if (isCreatingNew)
                    {
                        string insertQuery = "INSERT INTO TblProductList (PLCode, PLDescription, PLAmount, PLOrderShow) VALUES (@code, @desc, @amount, @order);";
                        using (var cmd = new SQLiteCommand(insertQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@code", code);
                            cmd.Parameters.AddWithValue("@desc", desc);
                            cmd.Parameters.AddWithValue("@amount", amount);
                            cmd.Parameters.AddWithValue("@order", order);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        string updateQuery = "UPDATE TblProductList SET PLDescription=@desc, PLAmount=@amount, PLOrderShow=@order WHERE PLCode=@code;";
                        using (var cmd = new SQLiteCommand(updateQuery, conn))
                        {
                            cmd.Parameters.AddWithValue("@desc", desc);
                            cmd.Parameters.AddWithValue("@amount", amount);
                            cmd.Parameters.AddWithValue("@order", order);
                            cmd.Parameters.AddWithValue("@code", code);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show("Producto guardado correctamente.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarProductos();
                // Notify main window to refresh its product list
                NotifyMainWindowToRefresh();
                // After saving, keep code field non-editable since selection will be cleared or reloaded
                txtCode.IsReadOnly = true;
                txtCode.Foreground = System.Windows.SystemColors.GrayTextBrush;
            }
            catch (SQLiteException ex)
            {
                if (ex.Message.Contains("UNIQUE constraint failed") || ex.Message.Contains("constraint failed"))
                {
                    MessageBox.Show("El código del producto ya existe. Ingrese otro código.", "Código duplicado", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Error al guardar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Eliminar_Click(object sender, RoutedEventArgs e)
        {
            if (lstProductos.SelectedItem is not Producto p)
            {
                MessageBox.Show("Seleccione un producto para eliminar.");
                return;
            }

            if (MessageBox.Show("¿Eliminar este producto?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string query = "DELETE FROM TblProductList WHERE PLCode = @code";
                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", p.Code);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Producto eliminado.", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
                CargarProductos();
                NotifyMainWindowToRefresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al eliminar: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NotifyMainWindowToRefresh()
        {
            try
            {
                var main = System.Windows.Application.Current.Windows.OfType<System.Windows.Window>().FirstOrDefault(w => w.GetType().Name == "MainWindow");
                if (main != null && main.DataContext is MainViewModel vm)
                {
                    vm.CargarProductosDesdeDB();
                }
            }
            catch
            {
                // ignore errors when attempting to notify
            }
        }

        // Sorting helpers
        private void OrdenarCodigo(object sender, RoutedEventArgs e)
        {
            MostrarLista(productos.OrderBy(p => p.Code));
        }

        private void OrdenarDescripcion(object sender, RoutedEventArgs e)
        {
            MostrarLista(productos.OrderBy(p => p.Description));
        }

        private void OrdenarPrecio(object sender, RoutedEventArgs e)
        {
            MostrarLista(productos.OrderBy(p => p.Amount));
        }

        private void OrdenarOrden(object sender, RoutedEventArgs e)
        {
            MostrarLista(productos.OrderBy(p => p.OrderShow));
        }

        // Numeric input validation (integers)
        private void NumericInteger_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        // Decimal validation (allow digits and one dot)
        private void NumericDecimal_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox tb)
            {
                string proposed = tb.Text.Substring(0, tb.SelectionStart) + e.Text + tb.Text.Substring(tb.SelectionStart + tb.SelectionLength);
                // allow only digits and at most one dot
                e.Handled = !Regex.IsMatch(proposed, "^\\d*(\\.\\d*)?$");
            }
            else
            {
                e.Handled = !Regex.IsMatch(e.Text, "^[0-9\\.]$");
            }
        }

        // Pasting handlers
        private void IntegerPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^[0-9]+$"))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void DecimalPasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!Regex.IsMatch(text, "^\\d*(\\.\\d*)?$"))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
