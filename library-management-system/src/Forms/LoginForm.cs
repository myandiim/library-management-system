using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Database_Proje.DataAccess;

namespace Database_Proje.Forms
{
    public partial class LoginForm : Form
    {
        
        // WINDOW DRAGGING API
        
        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")]
        private extern static void ReleaseCapture();
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam);

        // UI Controls
        private TextBox txtUser, txtPass;
        private Button btnLogin, btnClose;
        private Panel pnlLeft, pnlRight;
        private CheckBox chkShowPass;
        private Panel pnlUserLine, pnlPassLine;

        // Color Palet
        private Color primaryColor = Color.FromArgb(20, 30, 50);
        private Color accentColor = Color.FromArgb(41, 128, 185);

        public LoginForm()
        {
            SetupProfessionalDesign();
        }

        private void SetupProfessionalDesign()
        {
            // FORM GENERAL SETTINGS 
            this.FormBorderStyle = FormBorderStyle.None;
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;

            // LEFT PANEL 
            pnlLeft = new Panel();
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Width = 320;
            pnlLeft.BackColor = primaryColor;
            pnlLeft.MouseDown += Form_MouseDown;
            this.Controls.Add(pnlLeft);

            // LOGO 
            PictureBox picLogo = new PictureBox();
            picLogo.Size = new Size(170, 170); // Size controller 
            picLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picLogo.Location = new Point((pnlLeft.Width - 170) / 2, 70);

            // We are using logo helper in here
            picLogo.Image = LogoHelper.GetLogo(140, 140);

            pnlLeft.Controls.Add(picLogo);

            // LOGO SUBTITLE
            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Library Management\nSystem";
            lblSubtitle.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            lblSubtitle.ForeColor = Color.White;
            lblSubtitle.AutoSize = false;
            lblSubtitle.Size = new Size(pnlLeft.Width, 80);
            lblSubtitle.TextAlign = ContentAlignment.MiddleCenter;
            lblSubtitle.Location = new Point(0, 220);
            pnlLeft.Controls.Add(lblSubtitle);

            // Footer (belki kaldırırız)
            Label lblVersion = new Label();
            lblVersion.Text = "CENG301 PROJECT"; 
            lblVersion.Font = new Font("Segoe UI", 9, FontStyle.Italic);
            lblVersion.ForeColor = Color.Silver;
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(10, 470);
            pnlLeft.Controls.Add(lblVersion);

            // RIGHT PANEL (LOGIN FORM)
            pnlRight = new Panel();
            pnlRight.Dock = DockStyle.Fill;
            pnlRight.BackColor = Color.WhiteSmoke;
            pnlRight.MouseDown += Form_MouseDown;
            this.Controls.Add(pnlRight);

            pnlLeft.SendToBack();
            pnlRight.BringToFront();

            int contentWidth = 300;
            int leftMargin = (800 - 320 - contentWidth) / 2;

            // Close Button
            btnClose = CreateControlBoxButton("X", 440, 0, Color.IndianRed);
            btnClose.Click += (s, e) => Application.Exit();
            pnlRight.Controls.Add(btnClose);

            // Title LOGIN
            Label lblLoginTitle = new Label();
            lblLoginTitle.Text = "LOGIN";
            lblLoginTitle.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblLoginTitle.ForeColor = primaryColor;
            lblLoginTitle.AutoSize = true;
            lblLoginTitle.Location = new Point(leftMargin, 60);
            pnlRight.Controls.Add(lblLoginTitle);

            // USER NAME
            pnlUserLine = new Panel { BackColor = Color.Silver, Size = new Size(contentWidth, 2), Location = new Point(leftMargin, 180) };
            pnlRight.Controls.Add(pnlUserLine);

            txtUser = new TextBox();
            txtUser.BorderStyle = BorderStyle.None;
            txtUser.BackColor = Color.WhiteSmoke;
            txtUser.Font = new Font("Segoe UI", 12);
            txtUser.ForeColor = Color.DimGray;
            txtUser.Text = "Username";
            txtUser.Location = new Point(leftMargin, 150);
            txtUser.Size = new Size(contentWidth, 25);

            txtUser.Enter += (s, e) => {
                if (txtUser.Text == "Username") { txtUser.Text = ""; txtUser.ForeColor = Color.Black; }
                pnlUserLine.BackColor = primaryColor;
            };
            txtUser.Leave += (s, e) => {
                if (txtUser.Text == "") { txtUser.Text = "Username"; txtUser.ForeColor = Color.DimGray; }
                pnlUserLine.BackColor = Color.Silver;
            };
            pnlRight.Controls.Add(txtUser);

            // PASSWORD 
            pnlPassLine = new Panel { BackColor = Color.Silver, Size = new Size(contentWidth, 2), Location = new Point(leftMargin, 250) };
            pnlRight.Controls.Add(pnlPassLine);

            txtPass = new TextBox();
            txtPass.BorderStyle = BorderStyle.None;
            txtPass.BackColor = Color.WhiteSmoke;
            txtPass.Font = new Font("Segoe UI", 12);
            txtPass.ForeColor = Color.DimGray;
            txtPass.Text = "Password";
            txtPass.Location = new Point(leftMargin, 220);
            txtPass.Size = new Size(contentWidth, 25);

            txtPass.Enter += (s, e) => {
                if (txtPass.Text == "Password") { txtPass.Text = ""; txtPass.PasswordChar = '●'; txtPass.ForeColor = Color.Black; }
                pnlPassLine.BackColor = primaryColor;
            };
            txtPass.Leave += (s, e) => {
                if (txtPass.Text == "") { txtPass.Text = "Password"; txtPass.PasswordChar = '\0'; txtPass.ForeColor = Color.DimGray; }
                pnlPassLine.BackColor = Color.Silver;
            };
            pnlRight.Controls.Add(txtPass);

            // Show Password
            chkShowPass = new CheckBox();
            chkShowPass.Text = "Show Password";
            chkShowPass.Font = new Font("Segoe UI", 9);
            chkShowPass.ForeColor = Color.Gray;
            chkShowPass.Location = new Point(leftMargin, 260);
            chkShowPass.AutoSize = true;
            chkShowPass.Cursor = Cursors.Hand;
            chkShowPass.CheckedChanged += (s, e) => {
                if (txtPass.Text != "Password")
                    txtPass.PasswordChar = chkShowPass.Checked ? '\0' : '●';
            };
            pnlRight.Controls.Add(chkShowPass);

            // LOGIN BUTTON 
            btnLogin = new Button();
            btnLogin.Text = "LOGIN";
            btnLogin.BackColor = primaryColor;
            btnLogin.ForeColor = Color.White;
            btnLogin.FlatStyle = FlatStyle.Flat;
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnLogin.Size = new Size(contentWidth, 45);
            btnLogin.Location = new Point(leftMargin, 320);
            btnLogin.Cursor = Cursors.Hand;
            btnLogin.Click += BtnLogin_Click;
            btnLogin.MouseEnter += (s, e) => btnLogin.BackColor = Color.FromArgb(40, 50, 70);
            btnLogin.MouseLeave += (s, e) => btnLogin.BackColor = primaryColor;
            pnlRight.Controls.Add(btnLogin);
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            ReleaseCapture();
            SendMessage(this.Handle, 0x112, 0xf012, 0);
        }

        private Button CreateControlBoxButton(string text, int x, int y, Color hoverColor)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btn.Size = new Size(40, 30);
            btn.Location = new Point(x, y);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.Gray;
            btn.Cursor = Cursors.Hand;
            btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btn.MouseEnter += (s, e) => { btn.BackColor = hoverColor; btn.ForeColor = Color.White; };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Color.Gray; };
            return btn;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            string u = txtUser.Text.Trim();
            string p = txtPass.Text.Trim();

            if (u == "Username" || p == "Password" || string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
            {
                MessageBox.Show("Please enter your username and password.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                string query = "SELECT * FROM TBL_EMPLOYEES WHERE Username=@u AND Password=@p";
                SqlParameter[] paramsList = { new SqlParameter("@u", u), new SqlParameter("@p", p) };
                DataTable dt = SQLHelper.GetTable(query, paramsList);

                if (dt != null && dt.Rows.Count > 0)
                {
                    string role = dt.Rows[0]["Role"].ToString();
                    DashboardForm dash = new DashboardForm(u, role);
                    dash.Show();
                    this.Hide();
                }
                else
                {
                    MessageBox.Show("Invalid username or password!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred during login: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}