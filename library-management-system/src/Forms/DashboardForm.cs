using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Database_Proje.DataAccess;

namespace Database_Proje.Forms
{
    public partial class DashboardForm : Form
    {
        // --- GLOBAL SESSION INFORMATION ---
        public static string CurrentSessionUser;
        public static string CurrentSessionRole;

        private string currentUser;
        private string currentRole;

        // UI Controls
        private Panel pnlSidebar, pnlContent;
        private DataGridView gridActivity;
        private DataGridView gridAnnouncements;
        private TableLayoutPanel pnlStatsGrid;
        private System.Windows.Forms.Timer timerClock;

        // Color Palette
        private Color primaryColor = Color.FromArgb(20, 30, 50);
        private Color accentColor = Color.FromArgb(41, 128, 185);
        private Color bgColor = Color.FromArgb(236, 240, 243);

        public DashboardForm(string user, string role)
        {
            currentUser = user;
            currentRole = role;
            CurrentSessionUser = user;
            CurrentSessionRole = role;

            SetupDashboardDesign();

            timerClock = new System.Windows.Forms.Timer();
            timerClock.Interval = 60000;
            timerClock.Tick += (s, e) => { LoadStatistics(); LoadRecentActivity(); LoadAnnouncements(); };
            timerClock.Start();

            this.FormClosed += DashboardForm_FormClosed;
        }

        private void DashboardForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Application.OpenForms.Count == 0) Application.Exit();
            else Application.Exit();
        }

        private void PerformSafeLogout()
        {
            DialogResult result = MessageBox.Show("Are you sure you want to log out?", "Logout Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                LogHelper.LogProcess(currentUser, "User logged out.", "LOGOUT");
                LoginForm login = new LoginForm();
                login.Show();
                this.Hide();
            }
        }

        private void SetupDashboardDesign()
        {
            this.Text = "Library Management System";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = bgColor;

            // --- 1. LEFT MENU (SIDEBAR) ---
            pnlSidebar = new Panel();
            pnlSidebar.Dock = DockStyle.Left;
            pnlSidebar.Width = 280;
            pnlSidebar.BackColor = primaryColor;

            Panel pnlShadow = new Panel { Dock = DockStyle.Right, Width = 5, BackColor = ControlPaint.Dark(primaryColor, 0.2f) };
            pnlSidebar.Controls.Add(pnlShadow);
            this.Controls.Add(pnlSidebar);

            // LOGO HEADER (UPDATED)
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 140, BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(pnlHeader);

            // 1. Logo Image (From LogoHelper)
            PictureBox picDashLogo = new PictureBox();
            picDashLogo.Size = new Size(60, 60);
            picDashLogo.SizeMode = PictureBoxSizeMode.Zoom;
            picDashLogo.Location = new Point(30, 30); // Sol üst köşe
            picDashLogo.Image = LogoHelper.GetLogo(75, 75); // Logoyu buradan çekiyoruz
            pnlHeader.Controls.Add(picDashLogo);

            // 2. LMS Text (Yanına)
            Label lblLogo = new Label();
            lblLogo.Text = "LMS";
            lblLogo.Font = new Font("Segoe UI", 32, FontStyle.Bold);
            lblLogo.ForeColor = Color.White;
            lblLogo.AutoSize = true;
            lblLogo.Location = new Point(85, 25); // Resmin sağına
            pnlHeader.Controls.Add(lblLogo);

            // 3. Subtitle
            Label lblSub = new Label();
            lblSub.Text = "Management Panel";
            lblSub.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblSub.ForeColor = Color.Silver;
            lblSub.AutoSize = false;
            lblSub.Size = new Size(280, 25);
            lblSub.TextAlign = ContentAlignment.MiddleLeft;
            lblSub.Padding = new Padding(25, 0, 0, 0); // Hizalama için padding
            lblSub.Location = new Point(25, 95);
            pnlHeader.Controls.Add(lblSub);

            // Menu Buttons
            Panel pnlMenuContainer = new Panel { Dock = DockStyle.Top, Height = 400, BackColor = Color.Transparent };
            pnlSidebar.Controls.Add(pnlMenuContainer);
            pnlMenuContainer.BringToFront();

            AddMenuButton(pnlMenuContainer, "Home", (s, e) => SetupContentPanelDefault());
            AddMenuButton(pnlMenuContainer, "Book Management", (s, e) => LoadFormInPanel(new BooksForm()));
            AddMenuButton(pnlMenuContainer, "Member Operations", (s, e) => LoadFormInPanel(new MembersForm()));
            AddMenuButton(pnlMenuContainer, "Loan/Return", (s, e) => LoadFormInPanel(new LoanForm()));

            if (currentRole == "Admin")
            {
                AddMenuButton(pnlMenuContainer, "System Settings", (s, e) => LoadFormInPanel(new SettingsForm()));
            }

            // --- SAFE EXIT BUTTON ---
            Button btnExit = new Button();
            btnExit.Text = "  LOGOUT";
            btnExit.Dock = DockStyle.Bottom;
            btnExit.Height = 60;
            btnExit.FlatStyle = FlatStyle.Flat;
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.ForeColor = Color.IndianRed;
            btnExit.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnExit.TextAlign = ContentAlignment.MiddleLeft;
            btnExit.Padding = new Padding(30, 0, 0, 0);
            btnExit.Cursor = Cursors.Hand;
            btnExit.Click += (s, e) => PerformSafeLogout();

            btnExit.MouseEnter += (s, e) => btnExit.BackColor = Color.FromArgb(50, 20, 20);
            btnExit.MouseLeave += (s, e) => btnExit.BackColor = Color.Transparent;
            pnlSidebar.Controls.Add(btnExit);

            // Content Area
            pnlContent = new Panel();
            pnlContent.Dock = DockStyle.Fill;
            pnlContent.Padding = new Padding(30);
            this.Controls.Add(pnlContent);

            pnlSidebar.SendToBack();
            pnlContent.BringToFront();

            SetupContentPanelDefault();
        }

        private void SetupContentPanelDefault()
        {
            pnlContent.Controls.Clear();

            // Top Information Bar
            Panel pnlTopBar = new Panel { Dock = DockStyle.Top, Height = 70, BackColor = Color.Transparent };
            pnlContent.Controls.Add(pnlTopBar);

            Label lblWelcome = new Label();
            lblWelcome.Text = $"Welcome, {currentUser}";
            lblWelcome.Font = new Font("Segoe UI", 24, FontStyle.Bold);
            lblWelcome.ForeColor = primaryColor;
            lblWelcome.AutoSize = true;
            lblWelcome.Location = new Point(0, 5);
            pnlTopBar.Controls.Add(lblWelcome);

            Label lblRole = new Label();
            lblRole.Text = $"Role: {currentRole}";
            lblRole.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblRole.ForeColor = Color.Gray;
            lblRole.AutoSize = true;
            lblRole.Location = new Point(5, 50);
            pnlTopBar.Controls.Add(lblRole);

            // Statistics Cards
            pnlStatsGrid = new TableLayoutPanel();
            pnlStatsGrid.Dock = DockStyle.Top;
            pnlStatsGrid.Height = 150;
            pnlStatsGrid.ColumnCount = 4;
            pnlStatsGrid.RowCount = 1;
            pnlStatsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlStatsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlStatsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlStatsGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            pnlStatsGrid.Padding = new Padding(0, 10, 0, 10);
            pnlContent.Controls.Add(pnlStatsGrid);

            // Bottom Section (Split)
            TableLayoutPanel splitBottom = new TableLayoutPanel();
            splitBottom.Dock = DockStyle.Fill;
            splitBottom.ColumnCount = 2;
            splitBottom.RowCount = 1;
            splitBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            splitBottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            pnlContent.Controls.Add(splitBottom);
            splitBottom.BringToFront();

            // Left: Activities
            Panel pnlLeftActivity = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 10, 0) };
            splitBottom.Controls.Add(pnlLeftActivity, 0, 0);

            Label lblTable = new Label();
            lblTable.Text = "Recent Library Activities";
            lblTable.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTable.ForeColor = primaryColor;
            lblTable.Dock = DockStyle.Top;
            lblTable.Height = 40;
            lblTable.TextAlign = ContentAlignment.BottomLeft;
            pnlLeftActivity.Controls.Add(lblTable);

            gridActivity = CreateDashboardGrid();
            gridActivity.CellFormatting += GridActivity_CellFormatting;
            pnlLeftActivity.Controls.Add(gridActivity);
            gridActivity.BringToFront();

            // Right: Announcements
            Panel pnlRightAnnounce = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 10, 0, 0) };
            splitBottom.Controls.Add(pnlRightAnnounce, 1, 0);

            Label lblAnnounce = new Label();
            lblAnnounce.Text = "📢 Announcements";
            lblAnnounce.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblAnnounce.ForeColor = Color.DarkOrange;
            lblAnnounce.Dock = DockStyle.Top;
            lblAnnounce.Height = 40;
            lblAnnounce.TextAlign = ContentAlignment.BottomLeft;
            pnlRightAnnounce.Controls.Add(lblAnnounce);

            gridAnnouncements = CreateDashboardGrid();
            gridAnnouncements.RowTemplate.Height = 50;
            pnlRightAnnounce.Controls.Add(gridAnnouncements);
            gridAnnouncements.BringToFront();

            LoadStatistics();
            LoadRecentActivity();
            LoadAnnouncements();
        }

        private void GridActivity_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (gridActivity.Columns[e.ColumnIndex].Name == "Return Status" && e.Value != null)
            {
                string status = e.Value.ToString();
                if (status.Contains("Pending"))
                {
                    e.CellStyle.ForeColor = Color.DarkOrange;
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                }
                else if (status.Contains("OVERDUE"))
                {
                    e.CellStyle.ForeColor = Color.IndianRed;
                    e.CellStyle.Font = new Font(e.CellStyle.Font, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.SeaGreen;
                }
            }
        }

        private DataGridView CreateDashboardGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.RowHeadersVisible = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ColumnHeadersHeight = 40;
            grid.RowTemplate.Height = 40;
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 245);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.Gray;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(245, 245, 245);
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.Gray;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 9);
            grid.DefaultCellStyle.ForeColor = Color.Black;
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 230, 240);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            grid.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 250, 250);
            return grid;
        }

        private void LoadFormInPanel(Form formToShow)
        {
            if (pnlContent.Controls.Count > 0) pnlContent.Controls.Clear();
            formToShow.TopLevel = false;
            formToShow.FormBorderStyle = FormBorderStyle.None;
            formToShow.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(formToShow);
            pnlContent.Tag = formToShow;
            formToShow.Show();
        }

        private void AddMenuButton(Panel parent, string text, EventHandler onClick)
        {
            Button btn = new Button();
            btn.Text = "  " + text;
            btn.Dock = DockStyle.Top;
            btn.Height = 55;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.ForeColor = Color.LightGray;
            btn.Font = new Font("Segoe UI", 11);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Padding = new Padding(30, 0, 0, 0);
            btn.Cursor = Cursors.Hand;
            btn.MouseEnter += (s, e) => { btn.BackColor = Color.FromArgb(52, 73, 94); btn.ForeColor = Color.White; btn.Padding = new Padding(40, 0, 0, 0); };
            btn.MouseLeave += (s, e) => { btn.BackColor = Color.Transparent; btn.ForeColor = Color.LightGray; btn.Padding = new Padding(30, 0, 0, 0); };
            if (onClick != null) btn.Click += onClick;
            parent.Controls.Add(btn);
            parent.Controls.SetChildIndex(btn, 0);
        }

        private void CreateInfoCard(TableLayoutPanel parent, int colIndex, string title, string value, Color color)
        {
            Panel card = new Panel();
            card.Dock = DockStyle.Fill;
            card.BackColor = color;
            card.Margin = new Padding(0, 0, 15, 0);
            if (colIndex == 3) card.Margin = new Padding(0);

            Panel pnlAccent = new Panel { Dock = DockStyle.Left, Width = 6, BackColor = ControlPaint.Light(color, 0.3f) };
            card.Controls.Add(pnlAccent);

            Label lblVal = new Label();
            lblVal.Text = value;
            lblVal.Font = new Font("Segoe UI", 36, FontStyle.Bold);
            lblVal.ForeColor = Color.White;
            lblVal.AutoSize = true;
            lblVal.Location = new Point(15, 40);
            card.Controls.Add(lblVal);

            Label lblTitle = new Label();
            lblTitle.Text = title.ToUpper();
            lblTitle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(240, 240, 240);
            lblTitle.Location = new Point(15, 10);
            lblTitle.AutoSize = true;
            card.Controls.Add(lblTitle);

            parent.Controls.Add(card, colIndex, 0);
        }

        private void LoadStatistics()
        {
            if (pnlStatsGrid == null) return;
            pnlStatsGrid.Controls.Clear();
            try
            {
                object bookCount = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_BOOK_COPIES");
                CreateInfoCard(pnlStatsGrid, 0, "Total Books", bookCount?.ToString() ?? "0", Color.FromArgb(52, 152, 219));
                object memberCount = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_MEMBERS");
                CreateInfoCard(pnlStatsGrid, 1, "Registered Members", memberCount?.ToString() ?? "0", Color.FromArgb(46, 204, 113));
                object loanCount = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_LOANS WHERE ReturnDate IS NULL");
                CreateInfoCard(pnlStatsGrid, 2, "Active Loans", loanCount?.ToString() ?? "0", Color.FromArgb(243, 156, 18));
                string lateQuery = "SELECT COUNT(*) FROM TBL_LOANS WHERE ReturnDate IS NULL AND DueDate < GETDATE()";
                object lateCount = SQLHelper.ExecuteScalar(lateQuery);
                CreateInfoCard(pnlStatsGrid, 3, "Overdue Returns", lateCount?.ToString() ?? "0", Color.FromArgb(231, 76, 60));
            }
            catch { }
        }

        private void LoadRecentActivity()
        {
            try
            {
                string query = @"SELECT M.FirstName + ' ' + M.Surname AS [Member Name], B.Title AS [Book Title], FORMAT(L.IssueDate, 'dd.MM.yyyy') AS [Issue Date], FORMAT(L.DueDate, 'dd.MM.yyyy') AS [Due Date], CASE WHEN L.ReturnDate IS NULL AND GETDATE() > L.DueDate THEN 'OVERDUE' WHEN L.ReturnDate IS NULL THEN 'Pending...' ELSE FORMAT(L.ReturnDate, 'dd.MM.yyyy') END AS [Return Status] FROM TBL_LOANS L INNER JOIN TBL_MEMBERS M ON L.MemberID = M.MemberID INNER JOIN TBL_BOOK_COPIES BC ON L.CopyID = BC.CopyID INNER JOIN TBL_BOOKS B ON BC.BookID = B.BookID ORDER BY L.IssueDate DESC";
                DataTable dt = SQLHelper.GetTable(query);
                if (gridActivity != null) { gridActivity.DataSource = dt; gridActivity.ClearSelection(); }
            }
            catch { }
        }

        private void LoadAnnouncements()
        {
            try
            {
                string query = @"SELECT TOP 5 Title AS [Title], Content AS [Content], FORMAT(PublishDate, 'dd.MM.yyyy') AS [Date] FROM TBL_ANNOUNCEMENTS ORDER BY PublishDate DESC";
                DataTable dt = SQLHelper.GetTable(query);
                if (gridAnnouncements != null) { gridAnnouncements.DataSource = dt; gridAnnouncements.ClearSelection(); }
            }
            catch { }
        }
    }
}