using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AppSoftConsola.Models;

namespace AppSoftConsola.Services
{
    public static class ThermalTicketPrinter
    {
        public static void PrintReceipt(
            string pComercio,
            string pCUIT,
            string pDireccion,
            string pPtoVtaFormatted,
            string pticketNroFormatted,
            DateTime pfechaHora,
            List<CartItem> items,
            decimal ptotal,
            string pmedioPago,
            string pline1,
            string pline2,
            string pline3)
    {
        // ============================================================
        // 1) CARGAR PARÁMETROS DESDE SQLITE
        // ============================================================

        string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "pos10.db");
        var repo = new ParametersRepository($"Data Source={dbPath};Version=3;");
        var parametros = repo.Load();

        int columns = PrinterConfigService.GetColumns(parametros);
        string separator = PrinterConfigService.GetSeparator(parametros);

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        // Reset impresora
        bw.Write(new byte[] { 0x1B, 0x40 });

        // ============================================================
        // ENCABEZADO
        // ============================================================
        bw.Write(new byte[] { 0x1D, 0x21, 0x01 }); // 1.5 doble ancho

        if (!string.IsNullOrEmpty(pComercio))
            bw.Write(Encoding.ASCII.GetBytes(pComercio + "\n"));
        if (!string.IsNullOrEmpty(pCUIT))
            bw.Write(Encoding.ASCII.GetBytes(pCUIT + "\n"));
        if (!string.IsNullOrEmpty(pDireccion))
            bw.Write(Encoding.ASCII.GetBytes(pDireccion + "\n"));

        // Separador
        bw.Write(new byte[] { 0x1D, 0x21, 0x00 }); // tamaño normal
        bw.Write(Encoding.ASCII.GetBytes(separator + "\n"));

        // ============================================================
        // FECHA Y NÚMERO DE TICKET
        // ============================================================
        bw.Write(Encoding.ASCII.GetBytes($"Fecha: {pfechaHora:dd/MM/yyyy HH:mm}\n"));
        bw.Write(Encoding.ASCII.GetBytes($"Ticket: {pPtoVtaFormatted}-{pticketNroFormatted}\n"));
        bw.Write(Encoding.ASCII.GetBytes(separator + "\n"));

        // ============================================================
        // ITEMS
        // ============================================================
        foreach (var item in items)
        {
            string descripcion = item.Product.Description;
            string price = item.SubTotal.ToString("C");

            // Alineación dinámica según columnas configuradas
            string line = descripcion.PadRight(columns - price.Length) + price;

            bw.Write(Encoding.ASCII.GetBytes(line + "\n"));
        }

        bw.Write(Encoding.ASCII.GetBytes(separator + "\n"));

        // ============================================================
        // TOTAL (DOBLE ANCHO)
        // ============================================================
        bw.Write(new byte[] { 0x1D, 0x21, 0x10 }); // doble ancho
        bw.Write(Encoding.ASCII.GetBytes("TOTAL " + ptotal.ToString("C") + "\n"));

        // Medio de pago
        bw.Write(new byte[] { 0x1D, 0x21, 0x00 }); // normal
        bw.Write(Encoding.ASCII.GetBytes($"PAGO: {pmedioPago}\n"));

        bw.Write(Encoding.ASCII.GetBytes("\n"));

        // ============================================================
        // CORTE PARCIAL
        // ============================================================
        bw.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 });

        // Enviar a impresora usando el nombre configurado
        RawPrinterHelper.SendBytesToPrinter(parametros.PAPrinterName, ms.ToArray());
    }

}

}
