using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Database_Proje.DataAccess; // LogHelper ve SQLHelper

namespace Database_Proje.Forms
{
    public partial class LoanForm : Form
    {
        private TabControl tabControl;

        // Color Palette
        private Color primaryColor = Color.FromArgb(20, 30, 50);
        private Color accentColor = Color.FromArgb(41, 128, 185);
        private Color successColor = Color.SeaGreen;
        private Color errorColor = Color.IndianRed;

        // --- TAB 1: LOAN (ISSUE) ---
        private TextBox txtSearchMember;
        private DataGridView gridMembers;
        private TextBox txtSearchBook;
        private DataGridView gridAvailableCopies;
        private Label lblSelectedMemberName, lblSelectedBookInfo;
        private Button btnConfirmIssue;
        private NumericUpDown nudLoanDays;
        private int selectedMemberID = -1;
        private int selectedCopyID = -1;

        // --- TAB 2: RETURN ---
        private TextBox txtReturnCopyBarcode;
        private Label lblReturnBookInfo, lblReturnMemberInfo, lblFineInfo;
        private Button btnCheckReturn, btnConfirmReturn;
        private Panel pnlReturnDetail;
        private int currentLoanID = -1;

        // Additional Components for Return Screen
        private DataGridView gridActiveLoans;
        private RadioButton rdoReturnNormal, rdoReturnMaintenance, rdoReturnLost;

        // --- TAB 3: RESERVATION ---
        private DataGridView gridReservations;
        private TextBox txtResMember, txtResBook;
        private Button btnAddReservation, btnCancelReservation;

        // --- TAB 4: STATUS MANAGEMENT (INVENTORY STATUS) ---
        private TextBox txtStatusBarcode;
        private Label lblStatusCurrent;
        private Label lblStatusTitle;
        private RadioButton rdoStAvailable, rdoStMaintenance, rdoStLost;
        private Button btnUpdateStatus;
        private DataGridView gridNonAvailable;
        private int statusCopyID = -1;

        public LoanForm()
        {
            SetupForm();
            // Update loan period when form becomes visible
            this.VisibleChanged += LoanForm_VisibleChanged;
        }

        // Load loan day limit from database
        private int LoadLoanDayLimit()
        {
            try
            {
                object result = SQLHelper.ExecuteScalar("SELECT SettingValue FROM TBL_SETTINGS WHERE SettingKey = 'LoanDayLimit'");
                if (result != null)
                {
                    int dayLimit = Convert.ToInt32(result);
                    return dayLimit > 0 ? dayLimit : 15; // Default 15 days
                }
            }
            catch { }
            return 15; // Default value
        }

        // Update loan period when form becomes visible
        private void LoanForm_VisibleChanged(object sender, EventArgs e)
        {
            if (this.Visible && nudLoanDays != null)
            {
                nudLoanDays.Value = LoadLoanDayLimit();
            }
        }

        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            tabControl.ItemSize = new Size(140, 45);
            tabControl.SizeMode = TabSizeMode.Fixed;

            // 1. Tab: Issue Loan
            TabPage tabIssue = new TabPage("Issue Loan");
            tabIssue.BackColor = Color.White;
            SetupIssueTab(tabIssue);
            tabControl.TabPages.Add(tabIssue);

            // 2. Tab: Return
            TabPage tabReturn = new TabPage("Return");
            tabReturn.BackColor = Color.White;
            SetupReturnTab(tabReturn);
            tabControl.TabPages.Add(tabReturn);

            // 3. Tab: Reservation
            TabPage tabRes = new TabPage("Reservations");
            tabRes.BackColor = Color.White;
            SetupReservationTab(tabRes);
            tabControl.TabPages.Add(tabRes);

            // 4. Tab: Status Management
            TabPage tabStatus = new TabPage("Status Management");
            tabStatus.BackColor = Color.White;
            SetupStatusTab(tabStatus);
            tabControl.TabPages.Add(tabStatus);

            this.Controls.Add(tabControl);
        }

        // =============================================================
        // TAB 1: ISSUE LOAN INTERFACE
        // =============================================================
        private void SetupIssueTab(TabPage tab)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            tab.Controls.Add(layout);

            // LEFT: Member Selection
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.FromArgb(245, 245, 245) };
            layout.Controls.Add(pnlLeft, 0, 0);

            Label lblStep1 = new Label { Text = "STEP 1: SELECT MEMBER", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = primaryColor, Dock = DockStyle.Top, Height = 30, AutoSize = false };
            lblStep1.AutoEllipsis = true;
            pnlLeft.Controls.Add(lblStep1);

            txtSearchMember = new TextBox { Font = new Font("Segoe UI", 11), Dock = DockStyle.Top };
            txtSearchMember.TextChanged += (s, e) => LoadMembers(txtSearchMember.Text);
            pnlLeft.Controls.Add(txtSearchMember);
            Label lblSearchHint = new Label { Text = "Name or ID No:", ForeColor = Color.Gray, Font = new Font("Segoe UI", 9), Dock = DockStyle.Top, Height = 20, AutoSize = false };
            lblSearchHint.AutoEllipsis = true;
            pnlLeft.Controls.Add(lblSearchHint);

            pnlLeft.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });

            gridMembers = CreateGrid();
            gridMembers.CellClick += GridMembers_CellClick;
            pnlLeft.Controls.Add(gridMembers);
            gridMembers.BringToFront();

            // RIGHT: Book Selection and Confirmation
            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(15), BackColor = Color.White };
            layout.Controls.Add(pnlRight, 1, 0);

            Panel pnlFooter = new Panel { Dock = DockStyle.Bottom, Height = 180, BackColor = Color.FromArgb(240, 245, 250), Padding = new Padding(10) };
            pnlRight.Controls.Add(pnlFooter);

            lblSelectedMemberName = new Label { Text = "• No Member Selected", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.DimGray };
            pnlFooter.Controls.Add(lblSelectedMemberName);
            lblSelectedBookInfo = new Label { Text = "• No Book Selected", Dock = DockStyle.Top, Height = 30, Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.DimGray };
            pnlFooter.Controls.Add(lblSelectedBookInfo);

            Panel pnlDays = new Panel { Dock = DockStyle.Top, Height = 40 };
            pnlFooter.Controls.Add(pnlDays);
            pnlDays.Controls.Add(new Label { Text = "Duration (Days):", Location = new Point(0, 8), AutoSize = false, Size = new Size(100, 20) });
            nudLoanDays = new NumericUpDown { Location = new Point(100, 5), Value = LoadLoanDayLimit(), Maximum = 365, Font = new Font("Segoe UI", 12), Width = 100 };
            pnlDays.Controls.Add(nudLoanDays);

            btnConfirmIssue = new Button { Text = "COMPLETE LOAN", Dock = DockStyle.Bottom, Height = 50, BackColor = Color.Gray, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold), Enabled = false, Cursor = Cursors.Hand };
            btnConfirmIssue.Click += BtnConfirmIssue_Click;
            pnlFooter.Controls.Add(btnConfirmIssue);

            // Book Search Header
            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 70 };
            pnlRight.Controls.Add(pnlHeader);
            Label lblStep2 = new Label { Text = "STEP 2: SELECT BOOK (ISBN/Title)", Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = primaryColor, Dock = DockStyle.Top, Height = 30, AutoSize = false };
            lblStep2.AutoEllipsis = true;
            pnlHeader.Controls.Add(lblStep2);
            txtSearchBook = new TextBox { Font = new Font("Segoe UI", 12), Dock = DockStyle.Bottom };
            txtSearchBook.TextChanged += (s, e) => LoadAvailableCopies(txtSearchBook.Text);
            pnlHeader.Controls.Add(txtSearchBook);

            gridAvailableCopies = CreateGrid();
            gridAvailableCopies.CellClick += GridCopies_CellClick;
            pnlRight.Controls.Add(gridAvailableCopies);
            gridAvailableCopies.BringToFront();

            LoadMembers("");
            LoadAvailableCopies("");
        }

        // =============================================================
        // TAB 2: RETURN INTERFACE (LEFT ALIGNED)
        // =============================================================
        private void SetupReturnTab(TabPage tab)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tab.Controls.Add(layout);

            // --- LEFT PANEL: RETURN OPERATION ---
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            layout.Controls.Add(pnlLeft, 0, 0);

            Panel pnlActionBox = new Panel { Location = new Point(20, 30), Size = new Size(400, 500) };
            pnlLeft.Controls.Add(pnlActionBox);

            Label lblHeader = new Label { Text = "BOOK RETURN OPERATION", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = primaryColor, Location = new Point(0, 0), AutoSize = false, Size = new Size(400, 30) };
            pnlActionBox.Controls.Add(lblHeader);

            Label lblBar = new Label { Text = "Enter Book Barcode (CopyID):", Location = new Point(0, 60), AutoSize = false, Size = new Size(400, 20), Font = new Font("Segoe UI", 10) };
            pnlActionBox.Controls.Add(lblBar);

            txtReturnCopyBarcode = new TextBox { Location = new Point(0, 85), Size = new Size(250, 32), Font = new Font("Segoe UI", 14) };
            txtReturnCopyBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnCheckReturn_Click(null, null); };
            pnlActionBox.Controls.Add(txtReturnCopyBarcode);

            btnCheckReturn = new Button { Text = "QUERY", Location = new Point(260, 83), Size = new Size(130, 36), BackColor = accentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCheckReturn.Click += BtnCheckReturn_Click;
            pnlActionBox.Controls.Add(btnCheckReturn);

            pnlReturnDetail = new Panel { Location = new Point(0, 140), Size = new Size(400, 350), Visible = false };
            pnlActionBox.Controls.Add(pnlReturnDetail);

            lblReturnBookInfo = new Label { Text = "Book: ...", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.Black, Location = new Point(0, 0), AutoSize = false, Size = new Size(400, 25) };
            lblReturnBookInfo.AutoEllipsis = true;
            pnlReturnDetail.Controls.Add(lblReturnBookInfo);

            lblReturnMemberInfo = new Label { Text = "Member: ...", Font = new Font("Segoe UI", 11), ForeColor = Color.DimGray, Location = new Point(0, 30), AutoSize = false, Size = new Size(400, 25) };
            lblReturnMemberInfo.AutoEllipsis = true;
            pnlReturnDetail.Controls.Add(lblReturnMemberInfo);

            lblFineInfo = new Label { Text = "", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = successColor, Location = new Point(0, 60), AutoSize = false, Size = new Size(400, 25) };
            lblFineInfo.AutoEllipsis = true;
            pnlReturnDetail.Controls.Add(lblFineInfo);

            GroupBox grpStatus = new GroupBox { Text = "Book Return Status", Location = new Point(0, 100), Size = new Size(390, 80), Font = new Font("Segoe UI", 9) };
            pnlReturnDetail.Controls.Add(grpStatus);

            rdoReturnNormal = new RadioButton { Text = "Good (Normal)", Location = new Point(15, 30), AutoSize = false, Size = new Size(120, 20), Checked = true, ForeColor = successColor };
            rdoReturnMaintenance = new RadioButton { Text = "Damaged (Repair)", Location = new Point(140, 30), AutoSize = false, Size = new Size(130, 20), ForeColor = Color.Orange };
            rdoReturnLost = new RadioButton { Text = "Lost", Location = new Point(275, 30), AutoSize = false, Size = new Size(100, 20), ForeColor = errorColor };

            grpStatus.Controls.Add(rdoReturnNormal);
            grpStatus.Controls.Add(rdoReturnMaintenance);
            grpStatus.Controls.Add(rdoReturnLost);

            btnConfirmReturn = new Button { Text = "CONFIRM RETURN", Location = new Point(0, 200), Size = new Size(390, 50), BackColor = Color.Orange, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };
            btnConfirmReturn.Click += BtnConfirmReturn_Click;
            pnlReturnDetail.Controls.Add(btnConfirmReturn);

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            layout.Controls.Add(pnlRight, 1, 0);

            Label lblListTitle = new Label { Text = "CURRENTLY LOANED BOOKS", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = Color.DimGray, Dock = DockStyle.Top, Height = 30, AutoSize = false };
            lblListTitle.AutoEllipsis = true;
            pnlRight.Controls.Add(lblListTitle);

            gridActiveLoans = CreateGrid();
            pnlRight.Controls.Add(gridActiveLoans);
            gridActiveLoans.BringToFront();

            LoadActiveLoans();
        }

        // =============================================================
        // TAB 3: RESERVATION INTERFACE
        // =============================================================
        private void SetupReservationTab(TabPage tab)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tab.Controls.Add(layout);

            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            layout.Controls.Add(pnlLeft, 0, 0);

            Label lblResTitle = new Label { Text = "ACTIVE RESERVATIONS", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = primaryColor, Dock = DockStyle.Top, Height = 40, AutoSize = false };
            lblResTitle.AutoEllipsis = true;
            pnlLeft.Controls.Add(lblResTitle);

            gridReservations = CreateGrid();
            pnlLeft.Controls.Add(gridReservations);
            gridReservations.BringToFront();

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.WhiteSmoke };
            layout.Controls.Add(pnlRight, 1, 0);

            Label lblNewRes = new Label { Text = "NEW RESERVATION", Font = new Font("Segoe UI", 11, FontStyle.Bold), ForeColor = accentColor, Dock = DockStyle.Top, Height = 40, AutoSize = false };
            lblNewRes.AutoEllipsis = true;
            pnlRight.Controls.Add(lblNewRes);

            int y = 60;
            pnlRight.Controls.Add(new Label { Text = "Member No / ID:", Location = new Point(20, y), AutoSize = false, Size = new Size(200, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) });

            txtResMember = new TextBox { Location = new Point(20, y + 20), Width = 200, Font = new Font("Segoe UI", 10) };
            txtResMember.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtResMember.AutoCompleteSource = AutoCompleteSource.CustomSource;
            pnlRight.Controls.Add(txtResMember);
            y += 70;

            pnlRight.Controls.Add(new Label { Text = "Book Title / ISBN / Barcode:", Location = new Point(20, y), AutoSize = false, Size = new Size(200, 20), Font = new Font("Segoe UI", 9, FontStyle.Bold) });

            txtResBook = new TextBox { Location = new Point(20, y + 20), Width = 200, Font = new Font("Segoe UI", 10) };
            txtResBook.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            txtResBook.AutoCompleteSource = AutoCompleteSource.CustomSource;
            pnlRight.Controls.Add(txtResBook);
            y += 70;

            btnAddReservation = new Button { Text = "RESERVE", Location = new Point(20, y), Size = new Size(200, 40), BackColor = successColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddReservation.Click += BtnAddReservation_Click;
            pnlRight.Controls.Add(btnAddReservation);
            y += 80;

            pnlRight.Controls.Add(new Label { Text = "SELECTED OPERATION", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = errorColor, Location = new Point(20, y), AutoSize = false, Size = new Size(200, 25) });
            y += 30;

            btnCancelReservation = new Button { Text = "CANCEL / DELETE", Location = new Point(20, y), Size = new Size(200, 40), BackColor = errorColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCancelReservation.Click += BtnCancelReservation_Click;
            pnlRight.Controls.Add(btnCancelReservation);

            LoadReservations();
            LoadAutoCompleteData();
        }

        // =============================================================
        // TAB 4: STATUS MANAGEMENT (NEW)
        // =============================================================
        private void SetupStatusTab(TabPage tab)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tab.Controls.Add(layout);

            // --- LEFT: STATUS CHANGE PANEL ---
            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.WhiteSmoke };
            layout.Controls.Add(pnlLeft, 0, 0);

            Panel pnlBox = new Panel { Location = new Point(20, 30), Size = new Size(400, 500) };
            pnlLeft.Controls.Add(pnlBox);

            Label lblH = new Label { Text = "INVENTORY STATUS CONTROL", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = primaryColor, Location = new Point(0, 0), AutoSize = false, Size = new Size(400, 30) };
            pnlBox.Controls.Add(lblH);

            Label lblDesc = new Label { Text = "For updating the status of books on shelf or lost.\nUse 'Return' tab for books on loan.", Font = new Font("Segoe UI", 9, FontStyle.Italic), ForeColor = Color.DimGray, Location = new Point(0, 35), AutoSize = false, Size = new Size(400, 40) };
            pnlBox.Controls.Add(lblDesc);

            Label lblB = new Label { Text = "Scan Book Barcode:", Location = new Point(0, 80), AutoSize = false, Size = new Size(400, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlBox.Controls.Add(lblB);

            txtStatusBarcode = new TextBox { Location = new Point(0, 105), Size = new Size(250, 32), Font = new Font("Segoe UI", 14) };
            txtStatusBarcode.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnCheckStatus_Click(null, null); };
            pnlBox.Controls.Add(txtStatusBarcode);

            Button btnCheck = new Button { Text = "GET", Location = new Point(260, 103), Size = new Size(130, 36), BackColor = accentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 10, FontStyle.Bold), Cursor = Cursors.Hand };
            btnCheck.Click += BtnCheckStatus_Click;
            pnlBox.Controls.Add(btnCheck);

            Panel pnlDetails = new Panel { Location = new Point(0, 160), Size = new Size(400, 300), Name = "pnlStatusDetail", Visible = false };
            pnlBox.Controls.Add(pnlDetails);

            lblStatusTitle = new Label { Text = "Book: ...", Font = new Font("Segoe UI", 12, FontStyle.Bold), AutoSize = false, Size = new Size(390, 25) };
            lblStatusTitle.AutoEllipsis = true;
            pnlDetails.Controls.Add(lblStatusTitle);

            lblStatusCurrent = new Label { Text = "Current Status: ...", Location = new Point(0, 30), Font = new Font("Segoe UI", 11), AutoSize = false, Size = new Size(390, 25), ForeColor = primaryColor };
            lblStatusCurrent.AutoEllipsis = true;
            pnlDetails.Controls.Add(lblStatusCurrent);

            GroupBox grpChange = new GroupBox { Text = "Select New Status", Location = new Point(0, 70), Size = new Size(390, 80), Font = new Font("Segoe UI", 9) };
            pnlDetails.Controls.Add(grpChange);

            rdoStAvailable = new RadioButton { Text = "Available (On Shelf)", Location = new Point(15, 30), AutoSize = false, Size = new Size(110, 20), ForeColor = successColor };
            rdoStMaintenance = new RadioButton { Text = "Maintenance / Repair", Location = new Point(130, 30), AutoSize = false, Size = new Size(140, 20), ForeColor = Color.Orange };
            rdoStLost = new RadioButton { Text = "Lost", Location = new Point(275, 30), AutoSize = false, Size = new Size(100, 20), ForeColor = errorColor };

            grpChange.Controls.Add(rdoStAvailable); grpChange.Controls.Add(rdoStMaintenance); grpChange.Controls.Add(rdoStLost);

            btnUpdateStatus = new Button { Text = "UPDATE STATUS", Location = new Point(0, 170), Size = new Size(390, 50), BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };
            btnUpdateStatus.Click += BtnUpdateStatus_Click;
            pnlDetails.Controls.Add(btnUpdateStatus);

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            layout.Controls.Add(pnlRight, 1, 0);

            Label lblList = new Label { Text = "DAMAGED / LOST BOOKS IN LIBRARY", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = errorColor, Dock = DockStyle.Top, Height = 30 };
            pnlRight.Controls.Add(lblList);

            gridNonAvailable = CreateGrid();
            pnlRight.Controls.Add(gridNonAvailable);
            gridNonAvailable.BringToFront();

            LoadNonAvailableList();
        }

        // =============================================================
        // METHODS AND EVENT HANDLERS
        // =============================================================

        // --- STATUS MANAGEMENT OPERATIONS ---
        private void LoadNonAvailableList()
        {
            try
            {
                string query = @"
                    SELECT BC.CopyID AS [Barcode], B.Title AS [Book Title], BC.Status AS [Status], B.Location AS [Location]
                    FROM TBL_BOOK_COPIES BC
                    INNER JOIN TBL_BOOKS B ON BC.BookID = B.BookID
                    WHERE BC.Status IN ('Lost', 'Maintenance')";

                DataTable dt = SQLHelper.GetTable(query);
                if (gridNonAvailable != null) gridNonAvailable.DataSource = dt;
            }
            catch { }
        }

        private void BtnCheckStatus_Click(object sender, EventArgs e)
        {
            string bar = txtStatusBarcode.Text.Trim();
            if (string.IsNullOrEmpty(bar)) return;

            try
            {
                object isLoaned = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_LOANS WHERE CopyID=@c AND ReturnDate IS NULL", new SqlParameter("@c", bar));
                if (isLoaned != null && Convert.ToInt32(isLoaned) > 0)
                {
                    MessageBox.Show("This book is currently on loan!\nPlease use the 'Return' tab to change status.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string query = "SELECT B.Title, BC.Status FROM TBL_BOOK_COPIES BC INNER JOIN TBL_BOOKS B ON BC.BookID = B.BookID WHERE BC.CopyID=@c";
                DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@c", bar));

                if (dt != null && dt.Rows.Count > 0)
                {
                    if (!int.TryParse(bar, out int copyId))
                    {
                        MessageBox.Show("Invalid barcode format!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    statusCopyID = copyId;
                    string title = dt.Rows[0]["Title"]?.ToString() ?? "Unknown";
                    string status = dt.Rows[0]["Status"]?.ToString() ?? "Available";

                    lblStatusTitle.Text = "Book: " + title;
                    lblStatusCurrent.Text = "Current Status: " + (status == "Available" ? "Available" : (status == "Lost" ? "Lost" : "Maintenance"));

                    Control[] foundPanels = tabControl.TabPages[3].Controls[0].Controls[0].Controls[0].Controls.Find("pnlStatusDetail", true);
                    if (foundPanels.Length > 0)
                    {
                        foundPanels[0].Visible = true;
                    }

                    if (status == "Available") rdoStAvailable.Checked = true;
                    else if (status == "Maintenance") rdoStMaintenance.Checked = true;
                    else if (status == "Lost") rdoStLost.Checked = true;
                }
                else { MessageBox.Show("Barcode not found.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdateStatus_Click(object sender, EventArgs e)
        {
            if (statusCopyID == -1) return;

            string newStatus = rdoStAvailable.Checked ? "Available" : (rdoStMaintenance.Checked ? "Maintenance" : "Lost");

            SQLHelper.ExecuteQuery("UPDATE TBL_BOOK_COPIES SET Status=@s WHERE CopyID=@id", new SqlParameter("@s", newStatus), new SqlParameter("@id", statusCopyID));

            LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Manual Status Update: Copy {statusCopyID} -> {newStatus}", "MANUAL_STATUS");

            MessageBox.Show("Status updated.");
            txtStatusBarcode.Clear();
            Control pnl = tabControl.TabPages[3].Controls[0].Controls[0].Controls[0].Controls.Find("pnlStatusDetail", true)[0];
            pnl.Visible = false;
            LoadNonAvailableList();
            LoadActiveLoans();
            LoadAvailableCopies(txtSearchBook.Text);
        }

        // --- LIST CURRENTLY LOANED BOOKS ---
        private void LoadActiveLoans()
        {
            try
            {
                string query = @"
                    SELECT 
                        L.CopyID AS [Barcode],
                        B.Title AS [Book Title],
                        M.FirstName + ' ' + M.Surname AS [Member],
                        FORMAT(L.DueDate, 'dd.MM.yyyy') AS [Due Date]
                    FROM TBL_LOANS L
                    INNER JOIN TBL_BOOK_COPIES BC ON L.CopyID = BC.CopyID
                    INNER JOIN TBL_BOOKS B ON BC.BookID = B.BookID
                    INNER JOIN TBL_MEMBERS M ON L.MemberID = M.MemberID
                    WHERE L.ReturnDate IS NULL
                    ORDER BY L.DueDate ASC";

                DataTable dt = SQLHelper.GetTable(query);
                if (gridActiveLoans != null) gridActiveLoans.DataSource = dt;
            }
            catch { }
        }

        private void LoadAutoCompleteData()
        {
            try
            {
                AutoCompleteStringCollection memData = new AutoCompleteStringCollection();
                DataTable dtM = SQLHelper.GetTable("SELECT IdentityNo, FirstName + ' ' + Surname AS FullName FROM TBL_MEMBERS");
                if (dtM != null)
                {
                    foreach (DataRow r in dtM.Rows)
                    {
                        memData.Add(r["FullName"].ToString());
                        memData.Add(r["IdentityNo"].ToString());
                    }
                }
                txtResMember.AutoCompleteCustomSource = memData;

                AutoCompleteStringCollection bookData = new AutoCompleteStringCollection();
                DataTable dtB = SQLHelper.GetTable("SELECT ISBN, Title FROM TBL_BOOKS");
                if (dtB != null)
                {
                    foreach (DataRow r in dtB.Rows)
                    {
                        bookData.Add(r["Title"].ToString());
                        bookData.Add(r["ISBN"].ToString());
                    }
                }
                txtResBook.AutoCompleteCustomSource = bookData;
            }
            catch (Exception) { }
        }

        private void LoadMembers(string search)
        {
            string query = "SELECT MemberID, IdentityNo AS [ID No], FirstName + ' ' + Surname AS [Full Name] FROM TBL_MEMBERS";
            if (!string.IsNullOrEmpty(search)) query += " WHERE (FirstName LIKE @s OR Surname LIKE @s OR IdentityNo LIKE @s)";
            DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@s", "%" + search + "%"));
            if (dt != null) { gridMembers.DataSource = dt; if (gridMembers.Columns["MemberID"] != null) gridMembers.Columns["MemberID"].Visible = false; }
        }

        private void LoadAvailableCopies(string search)
        {
            string query = @"SELECT B.ISBN, BC.CopyID AS [Barcode], B.Title AS [Book Title], B.Location AS [Shelf] FROM TBL_BOOK_COPIES BC INNER JOIN TBL_BOOKS B ON BC.BookID = B.BookID WHERE BC.Status = 'Available'";
            if (!string.IsNullOrEmpty(search)) query += " AND (B.Title LIKE @s OR B.ISBN LIKE @s)";
            DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@s", "%" + search + "%"));
            if (dt != null) gridAvailableCopies.DataSource = dt;
        }

        private void LoadReservations()
        {
            string query = @"
                SELECT R.ReservationID, M.FirstName + ' ' + M.Surname AS [Member], B.Title AS [Book], R.RequestDate AS [Request Date], R.Status AS [Status]
                FROM TBL_RESERVATIONS R
                INNER JOIN TBL_MEMBERS M ON R.MemberID = M.MemberID
                INNER JOIN TBL_BOOKS B ON R.BookID = B.BookID
                WHERE R.Status = 'Pending'
                ORDER BY R.RequestDate DESC";
            DataTable dt = SQLHelper.GetTable(query);
            gridReservations.DataSource = dt;
            if (gridReservations.Columns["ReservationID"] != null) gridReservations.Columns["ReservationID"].Visible = false;
        }

        private void GridMembers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            selectedMemberID = Convert.ToInt32(gridMembers.Rows[e.RowIndex].Cells["MemberID"].Value);
            lblSelectedMemberName.Text = "• Member: " + gridMembers.Rows[e.RowIndex].Cells["Full Name"].Value.ToString();
            lblSelectedMemberName.ForeColor = successColor;
            CheckReadyToIssue();
        }

        private void GridCopies_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            selectedCopyID = Convert.ToInt32(gridAvailableCopies.Rows[e.RowIndex].Cells["Barcode"].Value);
            lblSelectedBookInfo.Text = "• Book: " + gridAvailableCopies.Rows[e.RowIndex].Cells["Book Title"].Value.ToString();
            lblSelectedBookInfo.ForeColor = successColor;
            CheckReadyToIssue();
        }

        private void CheckReadyToIssue()
        {
            btnConfirmIssue.Enabled = (selectedMemberID != -1 && selectedCopyID != -1);
            btnConfirmIssue.BackColor = btnConfirmIssue.Enabled ? successColor : Color.Gray;
        }

        // --- UPDATED LOAN ISSUE METHOD (WITH LIMIT CONTROL) ---
        private void BtnConfirmIssue_Click(object sender, EventArgs e)
        {
            try
            {
                // 1. PENALTY SCORE CHECK - Members with high penalty score cannot borrow books
                object penaltyObj = SQLHelper.ExecuteScalar("SELECT PenaltyScore FROM TBL_MEMBERS WHERE MemberID = @mid", new SqlParameter("@mid", selectedMemberID));
                int penaltyScore = (penaltyObj != null) ? Convert.ToInt32(penaltyObj) : 0;
                
                if (penaltyScore >= 50) // 50 points and above cannot borrow
                {
                    MessageBox.Show($"This member's penalty score is too high ({penaltyScore})! They must pay their fines first.", "High Penalty Score", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // 2. MAXIMUM BOOK LIMIT CHECK
                object limitObj = SQLHelper.ExecuteScalar("SELECT SettingValue FROM TBL_SETTINGS WHERE SettingKey = 'MaxBookLimit'");
                // If setting not found, default to 3
                int maxLimit = (limitObj != null && int.TryParse(limitObj.ToString(), out int lim)) ? lim : 3;

                // Find the member's current book count
                object countObj = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_LOANS WHERE MemberID = @mid AND ReturnDate IS NULL", new SqlParameter("@mid", selectedMemberID));
                int currentCount = (countObj != null) ? Convert.ToInt32(countObj) : 0;

                // Check
                if (currentCount >= maxLimit)
                {
                    MessageBox.Show($"This member has reached the maximum book limit ({maxLimit})! They currently have {currentCount} books.", "Limit Exceeded", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return; // Stop the operation
                }

                // 3. LOAN ISSUE OPERATION (Continue if limit not exceeded)
                int days = (int)nudLoanDays.Value;
                SqlParameter[] p = { new SqlParameter("@MemberID", selectedMemberID), new SqlParameter("@CopyID", selectedCopyID), new SqlParameter("@DayLimit", days) };
                SQLHelper.ExecuteQuery("EXEC SP_IssueBook @MemberID, @CopyID, @DayLimit", p);

                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Loan Issued: Member {selectedMemberID}, Copy {selectedCopyID}", "ISSUE");
                MessageBox.Show("Book issued.");
                LoadAvailableCopies(txtSearchBook.Text);
                selectedCopyID = -1; lblSelectedBookInfo.Text = "• No Book Selected"; CheckReadyToIssue();

                LoadActiveLoans();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCheckReturn_Click(object sender, EventArgs e)
        {
            string copyId = txtReturnCopyBarcode.Text.Trim();
            if (string.IsNullOrEmpty(copyId)) return;

            string query = @"SELECT L.LoanID, B.Title, M.FirstName + ' ' + M.Surname AS FullName, L.DueDate, M.PenaltyScore FROM TBL_LOANS L INNER JOIN TBL_BOOK_COPIES BC ON L.CopyID=BC.CopyID INNER JOIN TBL_BOOKS B ON BC.BookID=B.BookID INNER JOIN TBL_MEMBERS M ON L.MemberID=M.MemberID WHERE L.CopyID=@c AND L.ReturnDate IS NULL";
            DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@c", copyId));

            if (dt != null && dt.Rows.Count > 0)
            {
                currentLoanID = Convert.ToInt32(dt.Rows[0]["LoanID"]);
                lblReturnBookInfo.Text = "Book: " + dt.Rows[0]["Title"];
                lblReturnMemberInfo.Text = "Member: " + dt.Rows[0]["FullName"] + " (Penalty Score: " + dt.Rows[0]["PenaltyScore"] + ")";
                DateTime dueDate = Convert.ToDateTime(dt.Rows[0]["DueDate"]);
                int lateDays = (DateTime.Now > dueDate) ? (DateTime.Now - dueDate).Days : 0;
                lblFineInfo.Text = lateDays > 0 ? $"⚠️ OVERDUE: {lateDays} Days (Fine: {lateDays * 5} TL)" : "✅ On Time";

                rdoReturnNormal.Checked = true;

                pnlReturnDetail.Visible = true;
            }
            else { MessageBox.Show("Active loan not found."); pnlReturnDetail.Visible = false; }
        }

        private void BtnConfirmReturn_Click(object sender, EventArgs e)
        {
            if (currentLoanID == -1) return;
            try
            {
                string copyBarcode = txtReturnCopyBarcode.Text.Trim();

                SQLHelper.ExecuteQuery("EXEC SP_ReturnBook @LoanID", new SqlParameter("@LoanID", currentLoanID));

                string newStatus = "Available";
                string statusLog = "Normal";

                if (rdoReturnMaintenance.Checked) { newStatus = "Maintenance"; statusLog = "Damaged (Sent for Repair)"; }
                else if (rdoReturnLost.Checked) { newStatus = "Lost"; statusLog = "Lost"; }

                if (newStatus != "Available")
                {
                    string updateQuery = "UPDATE TBL_BOOK_COPIES SET Status = @st WHERE CopyID = @id";
                    SQLHelper.ExecuteQuery(updateQuery, new SqlParameter("@st", newStatus), new SqlParameter("@id", copyBarcode));
                }

                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Return Processed ({statusLog}): Copy {copyBarcode}", "RETURN");
                MessageBox.Show($"Book returned and marked as '{statusLog}'.");

                DataTable dtFine = SQLHelper.GetTable("SELECT Amount, FineID FROM TBL_FINES WHERE LoanID=@loanId AND IsPaid=0", new SqlParameter("@loanId", currentLoanID));
                if (dtFine != null && dtFine.Rows.Count > 0)
                {
                    if (MessageBox.Show($"Fine Amount: {dtFine.Rows[0]["Amount"]} TL. Collect payment?", "Fine", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        SQLHelper.ExecuteQuery("UPDATE TBL_FINES SET IsPaid=1 WHERE FineID=@fineId", new SqlParameter("@fineId", dtFine.Rows[0]["FineID"]));
                        LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Fine Collected: {dtFine.Rows[0]["Amount"]} TL", "PAYMENT");
                        MessageBox.Show("Fine collected.");
                    }
                }

                pnlReturnDetail.Visible = false; currentLoanID = -1; txtReturnCopyBarcode.Clear();
                LoadActiveLoans();
                LoadAvailableCopies(txtSearchBook.Text);
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnAddReservation_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtResMember.Text) || string.IsNullOrEmpty(txtResBook.Text))
            {
                MessageBox.Show("Please enter Member No/ID and Book Title/ISBN/Barcode."); return;
            }

            try
            {
                string searchMem = "%" + txtResMember.Text.Trim() + "%";
                string searchBook = "%" + txtResBook.Text.Trim() + "%";

                string memQuery = @"SELECT TOP 1 MemberID FROM TBL_MEMBERS 
                                    WHERE IdentityNo LIKE @v OR FirstName LIKE @v OR Surname LIKE @v OR (FirstName + ' ' + Surname) LIKE @v";
                object memId = SQLHelper.ExecuteScalar(memQuery, new SqlParameter("@v", searchMem));

                string bookQuery = "SELECT TOP 1 BookID FROM TBL_BOOKS WHERE Title LIKE @v OR ISBN LIKE @v";
                object bookId = SQLHelper.ExecuteScalar(bookQuery, new SqlParameter("@v", searchBook));

                if (memId == null) { MessageBox.Show("Member not found."); return; }
                if (bookId == null) { MessageBox.Show("Book not found."); return; }

                string query = "INSERT INTO TBL_RESERVATIONS (MemberID, BookID, RequestDate, Status) VALUES (@m, @b, GETDATE(), 'Pending')";
                SQLHelper.ExecuteQuery(query, new SqlParameter("@m", memId), new SqlParameter("@b", bookId));

                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Reservation: Member {memId} -> Book {bookId}", "RESERVE");
                MessageBox.Show("Reservation created.");

                txtResMember.Clear(); txtResBook.Clear(); LoadReservations();
            }
            catch (Exception ex) { MessageBox.Show("Error: " + ex.Message); }
        }

        private void BtnCancelReservation_Click(object sender, EventArgs e)
        {
            if (gridReservations.SelectedRows.Count == 0) return;
            int resId = Convert.ToInt32(gridReservations.SelectedRows[0].Cells["ReservationID"].Value);

            if (MessageBox.Show("Do you want to delete this reservation?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SQLHelper.ExecuteQuery("DELETE FROM TBL_RESERVATIONS WHERE ReservationID=@id", new SqlParameter("@id", resId));
                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Reservation Cancelled: ID {resId}", "RESERVE_CANCEL");
                LoadReservations();
            }
        }

        private DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.EnableHeadersVisualStyles = false;
            return grid;
        }
    }
}