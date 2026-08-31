using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.SQLite;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AppSoftConsola.Models;





namespace AppSoftConsola.ViewModels
{

  


    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ObservableCollection<Producto> Productos { get; set; }
        public ObservableCollection<CartItem> Carrito { get; set; }

        public int ItemsCount => Carrito.Sum(x => x.Quantity);

        private decimal _total;
        public decimal Total
        {
            get => _total;
            set { _total = value; OnPropertyChanged(); }
        }

        public string PtoVtaFormatted => PtoVta.ToString("0000");
        public string TicketNroFormatted => TicketNro.ToString("00000000");

        public ICommand AgregarProductoCommand { get; }
        public ICommand QuitarProductoCommand { get; }
        public ICommand ProcesarPagoCommand { get; }
        public ICommand LimpiarTicketCommand { get; }
        public ICommand ShowCashCommand { get; }


        private decimal _cajaEF;
        public decimal CajaEF
        {
            get => _cajaEF;
            set { _cajaEF = value; OnPropertyChanged(); }
        }

        private decimal _cajaMP;
        public decimal CajaMP
        {
            get => _cajaMP;
            set { _cajaMP = value; OnPropertyChanged(); }
        }

        private int _ptoVta;
        public int PtoVta
        {
            get => _ptoVta;
            set { _ptoVta = value; OnPropertyChanged(); OnPropertyChanged(nameof(PtoVtaFormatted)); }
        }

        private int _ticketNro;
        public int TicketNro
        {
            get => _ticketNro;
            set { _ticketNro = value; OnPropertyChanged(); OnPropertyChanged(nameof(TicketNroFormatted)); }
        }

        private string GetDbPath()
        {
            return System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Data",
                "pos10.db"
            );
        }

        public MainViewModel()
        {
            Productos = new ObservableCollection<Producto>();
            Carrito = new ObservableCollection<CartItem>();

            CargarParametros();
            CargarProductosDesdeDB();
            CargarTotalesCaja();   // ← AGREGAR ESTO

            AgregarProductoCommand = new RelayCommand<Producto>(AgregarProducto);
            QuitarProductoCommand = new RelayCommand<CartItem>(QuitarProducto);
            ProcesarPagoCommand = new RelayCommand<string>(ProcesarPago);
            LimpiarTicketCommand = new RelayCommand<object>(_ => LimpiarTicket());            
        }

        private void CargarParametros()
        {




            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Data","pos10.db");

            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                           
            {
                conn.Open();

                string query = "SELECT PAPuntoVenta, PALastTicketNumber FROM TblParameters";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        PtoVta = Convert.ToInt32(reader["PAPuntoVenta"]);
                        TicketNro = Convert.ToInt32(reader["PALastTicketNumber"]);
                    }
                }
            }

            OnPropertyChanged(nameof(PtoVta));
            OnPropertyChanged(nameof(TicketNro));
        }

        private void CargarTotalesCaja()
        {
            using (var conn = new SQLiteConnection($"Data Source={GetDbPath()};Version=3;"))
            {
                conn.Open();

                string queryEF = @"SELECT SUM(PTHAmount) 
                           FROM TblProductTicketHistory 
                           WHERE PTHTicketCobro = 'EF'";

                string queryMP = @"SELECT SUM(PTHAmount) 
                           FROM TblProductTicketHistory 
                           WHERE PTHTicketCobro = 'MP'";

                using (var cmd = new SQLiteCommand(queryEF, conn))
                {
                    var result = cmd.ExecuteScalar();
                    CajaEF = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }

                using (var cmd = new SQLiteCommand(queryMP, conn))
                {
                    var result = cmd.ExecuteScalar();
                    CajaMP = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }
            }

            OnPropertyChanged(nameof(CajaEF));
            OnPropertyChanged(nameof(CajaMP));
        }

        public void CargarProductosDesdeDB()
        {
            // refresh product list
            Productos.Clear();
            using (var conn = new SQLiteConnection($"Data Source={GetDbPath()};Version=3;"))
            {
                conn.Open();

                string query = "SELECT PLCode, PLDescription, PLAmount FROM TblProductList ORDER BY PLOrderShow";

                using (var cmd = new SQLiteCommand(query, conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int code = Convert.ToInt32(reader["PLCode"]);
                        string desc = reader["PLDescription"].ToString();
                        decimal amount = Convert.ToDecimal(reader["PLAmount"]);

                        Productos.Add(new Producto(code, desc, amount));
                    }
                }
            }
        }

        private void AgregarProducto(Producto p)
        {
            var item = Carrito.FirstOrDefault(x => x.Product.Code == p.Code);

            if (item == null)
            {
                Carrito.Add(new CartItem(p, 1));
            }
            else
            {
                item.Quantity++;
            }

            ActualizarTotales();
        }

        private void QuitarProducto(CartItem item)
        {
            Carrito.Remove(item);
            ActualizarTotales();
        }

        private void ActualizarTotales()
        {
            Total = Carrito.Sum(x => x.SubTotal);
            OnPropertyChanged(nameof(Total));
            OnPropertyChanged(nameof(ItemsCount));
        }

        private void ProcesarPago(string medio)
        {
            if (Carrito.Count == 0)
                return;

            // 1) Imprimir
            ImprimirTicket(medio);

            // 2) Guardar ticket
            GuardarTicketEnHistorial(medio);

            // 3) Incrementar número
            IncrementarNumeroTicket();

            // 4) Actualizar caja
            switch (medio)
            {
                case "EF":
                    CajaEF += Total;
                    break;

                case "MP":
                    CajaMP += Total;
                    break;
            }

            OnPropertyChanged(nameof(CajaEF));
            OnPropertyChanged(nameof(CajaMP));

            // 5) Limpiar ticket
            LimpiarTicket();
        }

        private void GuardarTicketEnHistorial(string medio)
        {
            using (var conn = new SQLiteConnection($"Data Source={GetDbPath()};Version=3;"))
            {
                conn.Open();

                foreach (var item in Carrito)
                {
                    string query = @"INSERT INTO TblProductTicketHistory
                            (PTHCode, PTHDescription, PTHAmount, PTHDate, PTHTicketNumber, PTHTicketPtaVta, PTHTicketCobro, PTHQuantity)
                            VALUES (@code, @desc, @amount, @date, @ticket, @pto, @medio, @quantity)";

                    using (var cmd = new SQLiteCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@code", item.Product.Code);
                        cmd.Parameters.AddWithValue("@desc", item.Product.Description);
                        cmd.Parameters.AddWithValue("@amount", item.SubTotal);
                        cmd.Parameters.AddWithValue("@date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ticket", TicketNro);
                        cmd.Parameters.AddWithValue("@pto", PtoVta);
                        cmd.Parameters.AddWithValue("@medio", medio);
                        cmd.Parameters.AddWithValue("@quantity", item.Quantity);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        private void IncrementarNumeroTicket()
        {
            TicketNro++;

            using (var conn = new SQLiteConnection($"Data Source={GetDbPath()};Version=3;"))
            {
                conn.Open();

                string query = "UPDATE TblParameters SET PALastTicketNumber = @nro";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@nro", TicketNro);
                    cmd.ExecuteNonQuery();
                }
            }

            OnPropertyChanged(nameof(TicketNro));
            OnPropertyChanged(nameof(TicketNroFormatted));
        }

        private void LimpiarTicket()
        {
            Carrito.Clear();
            Total = 0;
            OnPropertyChanged(nameof(ItemsCount));
            OnPropertyChanged(nameof(Total));
        }
              
        private void ImprimirTicket(string medio)
        {
            Console.WriteLine($"Ticket impreso con medio: {medio}");
        }
    }
}
