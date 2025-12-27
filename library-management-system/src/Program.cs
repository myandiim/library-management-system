using System;
using System.Windows.Forms;
using Database_Proje.Forms; // Formlarımızın olduğu klasörü gösteriyoruz

namespace Database_Proje
{
    internal static class Program
    {
        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Programı tasarladığımız LoginForm ile başlatıyoruz.
            Application.Run(new LoginForm());
        }
    }
}