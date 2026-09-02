using AppSoftConsola.Models;

namespace AppSoftConsola.Services
{
    public static class PrinterConfigService
    {
        public static int GetColumns(PrinterParameters p)
        {
            return p.PAUseCondensed == 1
                ? p.PAColumnsCondensed
                : p.PAColumnsNormal;
        }

        public static string GetSeparator(PrinterParameters p)
        {
            int cols = GetColumns(p);
            return new string('-', cols);
        }
    }
}