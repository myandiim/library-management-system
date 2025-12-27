using System;
using System.Data.SqlClient;

// NOTE: Placed in the same namespace as SQLHelper
// This way they can see each other directly
namespace Database_Proje.DataAccess
{
    public static class LogHelper
    {
        // Call this method after insert, delete, and update operations to keep records
        // Usage: LogHelper.LogProcess(DashboardForm.CurrentSessionUser, "New member added", "INSERT");
        public static void LogProcess(string username, string description, string processType)
        {
            try
            {
                // Step 1: Find EmployeeID from username
                // SQLHelper class is in the same namespace so we can call it directly
                object empIdObj = SQLHelper.ExecuteScalar("SELECT EmployeeID FROM TBL_EMPLOYEES WHERE Username = @u",
                                                          new SqlParameter("@u", username));

                // If user found, log the process
                if (empIdObj != null)
                {
                    int empId = Convert.ToInt32(empIdObj);

                    // Step 2: Insert log record into TBL_PROCESS_LOGS table
                    string query = @"INSERT INTO TBL_PROCESS_LOGS (EmployeeID, ProcessDate, Description, ProcessType) 
                                     VALUES (@empId, GETDATE(), @desc, @type)";

                    SqlParameter[] p = {
                        new SqlParameter("@empId", empId),
                        new SqlParameter("@desc", description),
                        new SqlParameter("@type", processType)
                    };

                    SQLHelper.ExecuteQuery(query, p);
                }
            }
            catch (Exception ex)
            {
                // Errors during logging should not stop the program,
                // so we only write to the "Output" window
                System.Diagnostics.Debug.WriteLine("Logging Error: " + ex.Message);
            }
        }
    }
}
