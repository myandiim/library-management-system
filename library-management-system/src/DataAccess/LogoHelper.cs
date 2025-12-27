using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.IO; 

namespace Database_Proje.DataAccess
{
    public static class LogoHelper
    {
        /// <summary>
        /// Uygulamanın logosunu "logo.png" dosyasından yükler.
        /// Dosya yoksa hiçbir şey (null) döndürür veya boş bir şeffaf resim döner.
        /// </summary>
        /// <param name="width">İstenen Genişlik</param>
        /// <param name="height">İstenen Yükseklik</param>
        /// <returns>Logo Resmi veya Boş Resim</returns>
        public static Image GetLogo(int width, int height)
        {
            // PROJENİN ÇALIŞTIĞI KLASÖRE
            string appPath = Application.StartupPath;
            string filePath = Path.Combine(appPath, "logo.png");

            // DOSYA VAR MI KONTROL ET
            if (File.Exists(filePath))
            {
                try
                {
                    // Resmi dosyadan al
                    using (Image imgFromFile = Image.FromFile(filePath))
                    {
                        // İstenen boyutlara göre yeniden ölçekle 
                        Bitmap resizedBmp = new Bitmap(width, height);
                        using (Graphics g = Graphics.FromImage(resizedBmp))
                        {
                            // Yüksek kalite ayarları 
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.SmoothingMode = SmoothingMode.AntiAlias;

                            // Resmi yeni boyutlarda çiz
                            g.DrawImage(imgFromFile, 0, 0, width, height);
                        }
                        return resizedBmp;
                    }
                }
                catch
                {
                    // Dosya bozuksa buraya düşer aşağıdan boş resim döneriz
                }
            }

            // Eğer logo.png yoksa veya bozuksa boş şeffafbir resim dön , Böylece uygulama hata vermez, sadece logo yeri boş kalır.

            return new Bitmap(width, height);
        }
    }
}