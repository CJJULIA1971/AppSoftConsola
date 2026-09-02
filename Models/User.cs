namespace AppSoftConsola.Models
{


    public class User
    {
        public string UserId { get; set; }
        public string PasswordHash { get; set; }
        public string RoleHash { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
    }
      
}