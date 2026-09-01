using AppSoftConsola.Models;

namespace AppSoftConsola
{
    public static class GlobalState
    {
        public static User CurrentUser { get; set; }
        public static string CurrentUserRole { get; set; }
    }
}
