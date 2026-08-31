using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using AppSoftConsola.Models;

namespace AppSoftConsola.Services
{
    public static class ThermalTicketPrinter
    {
        public static void PrintReceipt(string printerName,
                                        string ptoVtaFormatted,
                                        string ticketNroFormatted,
                                        DateTime fechaHora,
                                        List<CartItem> items,
                                        decimal total,
                                        string medioPago,
                                        string line1,
                                        string line2,
                                        string line3)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // Reset impresora
            bw.Write(new byte[] { 0x1B, 0x40 });

            // Título grande
            bw.Write(new byte[] { 0x1D, 0x21, 0x10 });

            if (!string.IsNullOrEmpty(line1))
                bw.Write(Encoding.ASCII.GetBytes(line1 + "\n"));
            if (!string.IsNullOrEmpty(line2))
                bw.Write(Encoding.ASCII.GetBytes(line2 + "\n"));
            if (!string.IsNullOrEmpty(line3))
                bw.Write(Encoding.ASCII.GetBytes(line3 + "\n"));

            // Tamaño normal
            bw.Write(new byte[] { 0x1D, 0x21, 0x00 });
            bw.Write(Encoding.ASCII.GetBytes($"Ticket Nro {ptoVtaFormatted}-{ticketNroFormatted}\n"));
            bw.Write(Encoding.ASCII.GetBytes("Fecha: " + fechaHora.ToString("dd/MM/yyyy HH:mm:ss") + "\n"));

            // Items
            bw.Write(new byte[] { 0x1D, 0x21, 0x10 });
            foreach (var item in items)
                bw.Write(Encoding.ASCII.GetBytes(item.Product.Description + "\n"));

            // Separador
            bw.Write(new byte[] { 0x1D, 0x21, 0x00 });
            bw.Write(Encoding.ASCII.GetBytes("-----------------------------\n"));

            // Total
            bw.Write(new byte[] { 0x1D, 0x21, 0x10 });
            bw.Write(Encoding.ASCII.GetBytes("TOTAL " + total.ToString("C") + "\n"));
            bw.Write(Encoding.ASCII.GetBytes($"PAGO: {medioPago}\n\n"));

            // Corte parcial
            bw.Write(new byte[] { 0x1D, 0x56, 0x41, 0x10 });

            // Enviar a impresora
            var bytes = ms.ToArray();
            RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
        }
    }
}
