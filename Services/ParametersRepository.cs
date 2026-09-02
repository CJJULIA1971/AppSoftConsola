using System;
using System.Data.SQLite;
using AppSoftConsola.Models;

namespace AppSoftConsola.Services
{
    public class ParametersRepository
    {
        private readonly string _connectionString;

        public ParametersRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public PrinterParameters Load()
        {
            var p = new PrinterParameters();

            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand("SELECT * FROM TblParameters LIMIT 1", conn);
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                p.PAPrinterName = reader["PAPrinterName"].ToString();
                p.PAPaperWidthMM = Convert.ToInt32(reader["PAPaperWidthMM"]);
                p.PAColumnsNormal = Convert.ToInt32(reader["PAColumnsNormal"]);
                p.PAColumnsCondensed = Convert.ToInt32(reader["PAColumnsCondensed"]);
                p.PAModel = reader["PAModel"].ToString();
                p.PAUseCondensed = Convert.ToInt32(reader["PAUseCondensed"]);
                p.PANombreComercio = reader["PANombreComercio"].ToString();
                p.PACUIT = reader["PACUIT"].ToString();
                p.PADireccionFull = reader["PADireccionFull"].ToString();                
            }
            return p;
        }
        public void Save(PrinterParameters p)
        {
            using var conn = new SQLiteConnection(_connectionString);
            conn.Open();

            using var cmd = new SQLiteCommand(@"
                UPDATE TblParameters SET
                    PAPrinterName = @printer,
                    PAPaperWidthMM = @width,
                    PAColumnsNormal = @normal,
                    PAColumnsCondensed = @condensed,
                    PAModel = @model,
                    PAUseCondensed = @useCondensed
            ", conn);

            cmd.Parameters.AddWithValue("@printer", p.PAPrinterName);
            cmd.Parameters.AddWithValue("@width", p.PAPaperWidthMM);
            cmd.Parameters.AddWithValue("@normal", p.PAColumnsNormal);
            cmd.Parameters.AddWithValue("@condensed", p.PAColumnsCondensed);
            cmd.Parameters.AddWithValue("@model", p.PAModel);
            cmd.Parameters.AddWithValue("@useCondensed", p.PAUseCondensed);

            cmd.ExecuteNonQuery();
        }
    }
}
