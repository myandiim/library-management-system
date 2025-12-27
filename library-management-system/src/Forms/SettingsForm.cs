using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Database_Proje.DataAccess;
using System.Globalization; // Added for language settings

namespace Database_Proje.Forms
{
    public partial class SettingsForm : Form
    {
        private Color primaryColor = Color.FromArgb(20, 30, 50);
        private Color accentColor = Color.FromArgb(41, 128, 185);

        private TabControl tabControl;

        // --- TAB 1: GENERAL SETTINGS ---
        private NumericUpDown nudDayLimit, nudFineAmount, nudMaxBookLimit;
        private Button btnSaveSettings;

        // --- TAB 2: ANNOUNCEMENTS ---
        private TextBox txtAnnounceTitle;
        private RichTextBox txtAnnounceContent;
        private DataGridView gridAnnouncements;
        private Button btnAddAnnounce, btnDeleteAnnounce;

        // --- TAB 3: EMPLOYEE MANAGEMENT ---
        private TextBox txtEmpName, txtEmpSurname, txtEmpUser, txtEmpPass;
        private ComboBox cmbEmpRole;
        private DataGridView gridEmployees;
        private Button btnAddEmployee, btnDeleteEmployee;

        public SettingsForm()
        {
            SetupForm();
            LoadCurrentSettings();
            LoadAnnouncements();
            LoadEmployees();
        }

        // =============================================================
        // DATA LOADING (FIXED: DOT/COMMA ERROR RESOLVED)
        // =============================================================
        private void LoadCurrentSettings()
        {
            try
            {
                DataTable dt = SQLHelper.GetTable("SELECT SettingKey, SettingValue FROM TBL_SETTINGS");
                if (dt != null)
                {
                    foreach (DataRow row in dt.Rows)
                    {
                        string key = row["SettingKey"].ToString();
                        string val = row["SettingValue"].ToString();

                        // Safe conversion using TryParse and InvariantCulture
                        // Reads correctly even if database has "5.00" or "5,00"
                        if (decimal.TryParse(val, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                        {
                            if (key == "DailyFineAmount") nudFineAmount.Value = result;
                            if (key == "LoanDayLimit") nudDayLimit.Value = result;
                            if (key == "MaxBookLimit") nudMaxBookLimit.Value = result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // If error occurs, at least see it (for Debug)
                System.Diagnostics.Debug.WriteLine("Error loading settings: " + ex.Message);
            }
        }

        private void BtnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                // Always save in "Dot" (.) format as standard
                // InvariantCulture: Forces "5.50" format regardless of computer language

                UpdateSingleSetting("DailyFineAmount", nudFineAmount.Value.ToString("0.00", CultureInfo.InvariantCulture));
                UpdateSingleSetting("LoanDayLimit", nudDayLimit.Value.ToString("0", CultureInfo.InvariantCulture));
                UpdateSingleSetting("MaxBookLimit", nudMaxBookLimit.Value.ToString("0", CultureInfo.InvariantCulture));

                LogHelper.LogProcess(
                    DashboardForm.CurrentSessionUser,
                    $"System Settings Changed (Fine: {nudFineAmount.Value}, Duration: {nudDayLimit.Value})",
                    "UPDATE_SETTINGS"
                );

                MessageBox.Show("System settings updated successfully.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        // =============================================================
        // OTHER METHODS (CONTINUE AS IS)
        // =============================================================

        private void UpdateSingleSetting(string key, string value)
        {
            string query = "UPDATE TBL_SETTINGS SET SettingValue = @v WHERE SettingKey = @k";
            SQLHelper.ExecuteQuery(query, new SqlParameter("@v", value), new SqlParameter("@k", key));
        }

        // ... (Rest of methods and design code continues the same) ...

        // Form Design (SetupForm etc.) remains the same as previous code, only LoadCurrentSettings and BtnSaveSettings updated.
        // Re-adding design code to maintain integrity:

        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            tabControl = new TabControl();
            tabControl.Dock = DockStyle.Fill;
            tabControl.Font = new Font("Segoe UI", 11);
            tabControl.ItemSize = new Size(160, 40);
            tabControl.SizeMode = TabSizeMode.Fixed;

            TabPage tabSettings = new TabPage("System Rules");
            tabSettings.BackColor = Color.White;
            SetupGeneralSettingsTab(tabSettings);
            tabControl.TabPages.Add(tabSettings);

            TabPage tabAnnounce = new TabPage("Announcement Management");
            tabAnnounce.BackColor = Color.White;
            SetupAnnouncementsTab(tabAnnounce);
            tabControl.TabPages.Add(tabAnnounce);

            TabPage tabEmployee = new TabPage("Employee Management");
            tabEmployee.BackColor = Color.White;
            SetupEmployeeTab(tabEmployee);
            tabControl.TabPages.Add(tabEmployee);

            this.Controls.Add(tabControl);
        }

        private void SetupGeneralSettingsTab(TabPage tab)
        {
            Panel pnlCenter = new Panel { Width = 500, Height = 400, BackColor = Color.WhiteSmoke };
            pnlCenter.Location = new Point(30, 30);
            tab.Controls.Add(pnlCenter);

            Label lblTitle = new Label { Text = "SYSTEM VARIABLES", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = primaryColor, Location = new Point(20, 20), AutoSize = false, Size = new Size(460, 30) };
            pnlCenter.Controls.Add(lblTitle);

            int y = 80;
            AddSettingItem(pnlCenter, "Daily Overdue Fine (TL):", ref nudFineAmount, y); y += 80;
            AddSettingItem(pnlCenter, "Default Loan Duration (Days):", ref nudDayLimit, y); y += 80;
            AddSettingItem(pnlCenter, "Max Books Per Person:", ref nudMaxBookLimit, y); y += 80;

            btnSaveSettings = new Button { Text = "SAVE SETTINGS", Location = new Point(20, y + 10), Size = new Size(460, 50), BackColor = accentColor, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 12, FontStyle.Bold), Cursor = Cursors.Hand };
            btnSaveSettings.Click += BtnSaveSettings_Click;
            pnlCenter.Controls.Add(btnSaveSettings);
        }

        private void SetupAnnouncementsTab(TabPage tab)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tab.Controls.Add(layout);

            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(250, 250, 250) };
            layout.Controls.Add(pnlLeft, 0, 0);

            Label lblNew = new Label { Text = "PUBLISH NEW ANNOUNCEMENT", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = primaryColor, Dock = DockStyle.Top, Height = 40 };
            pnlLeft.Controls.Add(lblNew);

            pnlLeft.Controls.Add(new Label { Text = "Title:", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray });
            txtAnnounceTitle = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 11), Height = 30 };
            pnlLeft.Controls.Add(txtAnnounceTitle);

            pnlLeft.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 20 });

            pnlLeft.Controls.Add(new Label { Text = "Content:", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray });
            txtAnnounceContent = new RichTextBox { Dock = DockStyle.Top, Height = 200, Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
            pnlLeft.Controls.Add(txtAnnounceContent);

            pnlLeft.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 20 });

            btnAddAnnounce = new Button { Text = "PUBLISH", Dock = DockStyle.Top, Height = 45, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddAnnounce.Click += BtnAddAnnounce_Click;
            pnlLeft.Controls.Add(btnAddAnnounce);

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            layout.Controls.Add(pnlRight, 1, 0);

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40 };
            pnlRight.Controls.Add(pnlHeader);

            Label lblAnnounceHeader = new Label { Text = "ACTIVE ANNOUNCEMENTS", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.DimGray, Location = new Point(0, 0), AutoSize = false, Size = new Size(300, 25) };
            lblAnnounceHeader.AutoEllipsis = true;
            pnlHeader.Controls.Add(lblAnnounceHeader);

            btnDeleteAnnounce = new Button { Text = "Delete Selected", BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(100, 30), Location = new Point(300, 0), Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnDeleteAnnounce.Click += BtnDeleteAnnounce_Click;
            pnlHeader.Controls.Add(btnDeleteAnnounce);

            gridAnnouncements = new DataGridView();
            gridAnnouncements.Dock = DockStyle.Fill;
            gridAnnouncements.BackgroundColor = Color.White;
            gridAnnouncements.BorderStyle = BorderStyle.None;
            gridAnnouncements.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAnnouncements.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridAnnouncements.ReadOnly = true;
            gridAnnouncements.RowHeadersVisible = false;
            gridAnnouncements.AllowUserToAddRows = false;
            gridAnnouncements.ColumnHeadersHeight = 40;
            gridAnnouncements.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            gridAnnouncements.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            gridAnnouncements.EnableHeadersVisualStyles = false;
            pnlRight.Controls.Add(gridAnnouncements);
        }

        private void SetupEmployeeTab(TabPage tab)
        {
            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 2;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            tab.Controls.Add(layout);

            Panel pnlLeft = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20), BackColor = Color.FromArgb(250, 250, 250) };
            layout.Controls.Add(pnlLeft, 0, 0);

            Label lblHead = new Label { Text = "ADD NEW EMPLOYEE", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = primaryColor, Dock = DockStyle.Top, Height = 40 };
            pnlLeft.Controls.Add(lblHead);

            EventHandler autoFill = (s, e) => {
                if (!string.IsNullOrEmpty(txtEmpName.Text) && !string.IsNullOrEmpty(txtEmpSurname.Text))
                {
                    string suggestion = (txtEmpName.Text + txtEmpSurname.Text).ToLower().Replace(" ", "");
                    if (string.IsNullOrEmpty(txtEmpUser.Text) || txtEmpUser.Tag != null)
                    {
                        txtEmpUser.Text = suggestion;
                        txtEmpUser.Tag = "auto";
                    }
                    if (string.IsNullOrEmpty(txtEmpPass.Text))
                    {
                        txtEmpPass.Text = txtEmpName.Text.ToLower().Replace(" ", "") + "1234";
                    }
                }
            };

            AddInput(pnlLeft, "First Name:", ref txtEmpName); txtEmpName.TextChanged += autoFill;
            AddInput(pnlLeft, "Last Name:", ref txtEmpSurname); txtEmpSurname.TextChanged += autoFill;
            AddInput(pnlLeft, "Username (Auto):", ref txtEmpUser);
            AddInput(pnlLeft, "Password (Default: name1234):", ref txtEmpPass);

            pnlLeft.Controls.Add(new Label { Text = "Role:", Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray });
            cmbEmpRole = new ComboBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 11), Height = 30, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbEmpRole.Items.AddRange(new object[] { "Staff", "Admin" });
            cmbEmpRole.SelectedIndex = 0;
            pnlLeft.Controls.Add(cmbEmpRole);

            pnlLeft.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 20 });

            btnAddEmployee = new Button { Text = "SAVE EMPLOYEE", Dock = DockStyle.Top, Height = 45, BackColor = Color.SeaGreen, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 11, FontStyle.Bold), Cursor = Cursors.Hand };
            btnAddEmployee.Click += BtnAddEmployee_Click;
            pnlLeft.Controls.Add(btnAddEmployee);

            Panel pnlRight = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            layout.Controls.Add(pnlRight, 1, 0);

            Panel pnlHeader = new Panel { Dock = DockStyle.Top, Height = 40 };
            pnlRight.Controls.Add(pnlHeader);

            Label lblEmployeeHeader = new Label { Text = "EMPLOYEE LIST", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.DimGray, Location = new Point(0, 0), AutoSize = false, Size = new Size(300, 25) };
            lblEmployeeHeader.AutoEllipsis = true;
            pnlHeader.Controls.Add(lblEmployeeHeader);

            btnDeleteEmployee = new Button { Text = "Delete Selected", BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(100, 30), Location = new Point(300, 0), Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnDeleteEmployee.Click += BtnDeleteEmployee_Click;
            pnlHeader.Controls.Add(btnDeleteEmployee);

            gridEmployees = CreateGrid();
            pnlRight.Controls.Add(gridEmployees);
            gridEmployees.BringToFront();
        }

        private void LoadEmployees()
        {
            try
            {
                DataTable dt = SQLHelper.GetTable("SELECT EmployeeID, Username AS [Username], Role AS [Role] FROM TBL_EMPLOYEES");
                gridEmployees.DataSource = dt;
                if (gridEmployees.Columns["EmployeeID"] != null) gridEmployees.Columns["EmployeeID"].Visible = false;
            }
            catch { }
        }

        private void BtnAddEmployee_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtEmpUser.Text) || string.IsNullOrEmpty(txtEmpPass.Text))
            {
                MessageBox.Show("Username and Password are required."); return;
            }

            try
            {
                string query = "INSERT INTO TBL_EMPLOYEES (Username, Password, Role) VALUES (@u, @p, @r)";
                SqlParameter[] p = {
                    new SqlParameter("@u", txtEmpUser.Text),
                    new SqlParameter("@p", txtEmpPass.Text),
                    new SqlParameter("@r", cmbEmpRole.SelectedItem.ToString())
                };

                SQLHelper.ExecuteQuery(query, p);
                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"New Employee Added: {txtEmpUser.Text} ({cmbEmpRole.SelectedItem})", "INSERT_EMPLOYEE");

                txtEmpName.Clear(); txtEmpSurname.Clear(); txtEmpUser.Clear(); txtEmpPass.Clear();
                LoadEmployees();
                MessageBox.Show("Employee added.");
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("UNIQUE")) MessageBox.Show("This username is already in use!");
                else MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void BtnDeleteEmployee_Click(object sender, EventArgs e)
        {
            if (gridEmployees.SelectedRows.Count == 0) return;

            int id = Convert.ToInt32(gridEmployees.SelectedRows[0].Cells["EmployeeID"].Value);
            string user = gridEmployees.SelectedRows[0].Cells["Username"].Value.ToString();

            if (user == DashboardForm.CurrentSessionUser)
            {
                MessageBox.Show("You cannot delete your own account!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete user '{user}'?", "Confirmation", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                SQLHelper.ExecuteQuery("DELETE FROM TBL_EMPLOYEES WHERE EmployeeID = @id", new SqlParameter("@id", id));
                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Employee Deleted: {user}", "DELETE_EMPLOYEE");
                LoadEmployees();
            }
        }

        private void LoadAnnouncements()
        {
            DataTable dt = SQLHelper.GetTable("SELECT AnnouncementID, Title AS [Title], PublishDate AS [Date], Content FROM TBL_ANNOUNCEMENTS ORDER BY PublishDate DESC");
            gridAnnouncements.DataSource = dt;
            if (gridAnnouncements.Columns["AnnouncementID"] != null) gridAnnouncements.Columns["AnnouncementID"].Visible = false;
            if (gridAnnouncements.Columns["Content"] != null) gridAnnouncements.Columns["Content"].Visible = false;
        }

        private void BtnAddAnnounce_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtAnnounceTitle.Text) || string.IsNullOrEmpty(txtAnnounceContent.Text)) return;
            try
            {
                object empId = SQLHelper.ExecuteScalar("SELECT EmployeeID FROM TBL_EMPLOYEES WHERE Username=@u", new SqlParameter("@u", DashboardForm.CurrentSessionUser));
                string query = "INSERT INTO TBL_ANNOUNCEMENTS (Title, Content, PublishDate, EmployeeID) VALUES (@t, @c, GETDATE(), @eid)";
                SQLHelper.ExecuteQuery(query, new SqlParameter("@t", txtAnnounceTitle.Text), new SqlParameter("@c", txtAnnounceContent.Text), new SqlParameter("@eid", empId ?? DBNull.Value));
                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Announcement: {txtAnnounceTitle.Text}", "INSERT_ANNOUNCE");
                txtAnnounceTitle.Clear(); txtAnnounceContent.Clear(); LoadAnnouncements();
            }
            catch { }
        }

        private void BtnDeleteAnnounce_Click(object sender, EventArgs e)
        {
            if (gridAnnouncements.SelectedRows.Count == 0) return;
            int id = Convert.ToInt32(gridAnnouncements.SelectedRows[0].Cells["AnnouncementID"].Value);
            SQLHelper.ExecuteQuery("DELETE FROM TBL_ANNOUNCEMENTS WHERE AnnouncementID = @id", new SqlParameter("@id", id));
            LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Announcement Deleted: {id}", "DELETE_ANNOUNCE");
            LoadAnnouncements();
        }

        private void AddSettingItem(Panel p, string title, ref NumericUpDown nud, int y)
        {
            Label lbl = new Label { Text = title, Location = new Point(20, y), Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = false, Size = new Size(270, 25) };
            p.Controls.Add(lbl);
            nud = new NumericUpDown { Location = new Point(300, y - 3), Size = new Size(180, 30), Font = new Font("Segoe UI", 12), TextAlign = HorizontalAlignment.Center, Maximum = 1000, DecimalPlaces = 2 };
            p.Controls.Add(nud);
        }

        private void AddInput(Panel p, string label, ref TextBox txt)
        {
            p.Controls.Add(new Label { Text = label, Dock = DockStyle.Top, Height = 25, Font = new Font("Segoe UI", 10, FontStyle.Bold), ForeColor = Color.Gray });
            txt = new TextBox { Dock = DockStyle.Top, Font = new Font("Segoe UI", 11), Height = 30 };
            p.Controls.Add(txt);
            p.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 10 });
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