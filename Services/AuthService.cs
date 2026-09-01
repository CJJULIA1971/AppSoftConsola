using System.Data.SQLite;
using AppSoftConsola.Models;

namespace AppSoftConsola.Services
{
    public static class AuthService
    {
        private static string dbPath = "Data/pos10.db";

        public static User GetUserById(string userId)
        {
            using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
            {
                conn.Open();

                string query = @"SELECT UserId, PasswordHash, RoleHash, Nombre, Apellido
                         FROM TblUsers
                         WHERE UserId = @id";

                using (var cmd = new SQLiteCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@id", userId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new User
                            {
                                UserId = reader["UserId"].ToString(),
                                PasswordHash = reader["PasswordHash"].ToString(),
                                RoleHash = reader["RoleHash"].ToString(),
                                Nombre = reader["Nombre"].ToString(),
                                Apellido = reader["Apellido"].ToString()
                            };
                        }
                    }
                }
            }

            return null;
        }
    }
}