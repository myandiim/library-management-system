using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Database_Proje.DataAccess
{
    public static class SQLHelper
    {
        // Possible SQL Server Names (Will be tried in order)
        private static string[] possibleServers = { @".\SQLEXPRESS", ".", "(local)", @"(localdb)\MSSQLLocalDB" };
        private static string dbName = "AdvancedLibraryDB";
        private static string validConnectionString = "";

        public static SqlConnection GetConnection()
        {
            // If a valid connection was found before, use it
            if (!string.IsNullOrEmpty(validConnectionString))
                return new SqlConnection(validConnectionString);

            // If connecting for the first time, scan servers
            foreach (string server in possibleServers)
            {
                string connStr = $"Data Source={server};Initial Catalog={dbName};Integrated Security=True";
                try
                {
                    using (SqlConnection con = new SqlConnection(connStr))
                    {
                        con.Open(); // Test connection
                        validConnectionString = connStr; // If it worked, save it
                        return new SqlConnection(validConnectionString);
                    }
                }
                catch { /* This server didn't work, try next one */ }
            }

            // If none worked, show error
            MessageBox.Show("Database server not found!\nPlease make sure SQL Server is running.", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }

        public static DataTable GetTable(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection con = GetConnection())
            {
                if (con == null) return null;
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    SqlDataAdapter da = new SqlDataAdapter(cmd);
                    DataTable dt = new DataTable();
                    try { da.Fill(dt); return dt; }
                    catch (Exception ex) { MessageBox.Show("Data Retrieval Error: " + ex.Message); return null; }
                }
            }
        }

        public static void ExecuteQuery(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection con = GetConnection())
            {
                if (con == null) return;
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    try { con.Open(); cmd.ExecuteNonQuery(); }
                    catch (Exception ex) { MessageBox.Show("Operation Error: " + ex.Message); }
                }
            }
        }

        public static object ExecuteScalar(string query, params SqlParameter[] parameters)
        {
            using (SqlConnection con = GetConnection())
            {
                if (con == null) return null;
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    try { con.Open(); return cmd.ExecuteScalar(); }
                    catch { return null; }
                }
            }
        }
    }
}