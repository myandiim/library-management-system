using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Database_Proje.DataAccess;

namespace Database_Proje.Forms
{
    public partial class MembersForm : Form
    {
        // =============================================================
        // VARIABLES
        // =============================================================
        private Panel pnlLeft, pnlFill;
        private DataGridView gridMembers;
        private DataGridView gridMemberHistory;

        // Input Controls
        private TextBox txtIdentity, txtName, txtSurname, txtPhone, txtEmail, txtSearch;
        private Label lblPenaltyScore;

        private Button btnAdd, btnUpdate, btnDelete;

        // Colors
        private Color primaryColor = Color.FromArgb(20, 30, 50);
        private Color accentColor = Color.FromArgb(41, 128, 185);

        private int selectedMemberID = -1;

        public MembersForm()
        {
            SetupForm();
            LoadMembers();

            // --- SECURITY SETTING ---
            ApplyRoleBasedSecurity();
        }

        // --- SECURITY METHOD ---
        // This method checks the role of the logged-in user
        private void ApplyRoleBasedSecurity()
        {
            // If the logged-in user is NOT "Admin" (e.g., Staff), disable delete button
            if (DashboardForm.CurrentSessionRole != "Admin")
            {
                btnDelete.Enabled = false;
                btnDelete.BackColor = Color.LightGray;
                btnDelete.ForeColor = Color.DarkGray;
                btnDelete.Text = "DELETE (NO PERMISSION)";

                // Optional: Tooltip can be added to explain why it doesn't work when clicked
                ToolTip tt = new ToolTip();
                tt.SetToolTip(btnDelete, "Administrator (Admin) permission is required for delete operation.");
            }
        }

        // =============================================================
        // 1. DESIGN (MODERN & RESPONSIVE)
        // =============================================================
        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;
            this.MinimumSize = new Size(800, 500);

            // --- LEFT PANEL (MEMBER INFORMATION) ---
            pnlLeft = new Panel();
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Width = 350;
            pnlLeft.BackColor = Color.FromArgb(245, 245, 245);
            Panel pnlBorder = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.LightGray };
            pnlLeft.Controls.Add(pnlBorder);
            this.Controls.Add(pnlLeft);

            Label lblTitle = new Label();
            lblTitle.Text = "MEMBER OPERATIONS";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = primaryColor;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = false;
            lblTitle.Size = new Size(300, 25);
            pnlLeft.Controls.Add(lblTitle);

            // Input Fields
            int y = 60;
            AddInput("ID / Student No:", ref txtIdentity, y); y += 60;
            AddInput("First Name:", ref txtName, y); y += 60;
            AddInput("Last Name:", ref txtSurname, y); y += 60;
            AddInput("Phone:", ref txtPhone, y); y += 60;
            AddInput("E-Mail:", ref txtEmail, y); y += 60;

            lblPenaltyScore = new Label();
            lblPenaltyScore.Text = "Penalty Score: 0";
            lblPenaltyScore.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            lblPenaltyScore.ForeColor = Color.IndianRed;
            lblPenaltyScore.Location = new Point(20, y + 10);
            lblPenaltyScore.AutoSize = false;
            lblPenaltyScore.Size = new Size(300, 25);
            pnlLeft.Controls.Add(lblPenaltyScore);
            y += 50;

            // Buttons
            btnAdd = CreateButton("SAVE", Color.SeaGreen, y);
            btnAdd.Click += BtnAdd_Click;
            pnlLeft.Controls.Add(btnAdd);

            btnUpdate = CreateButton("UPDATE", Color.Orange, y + 50);
            btnUpdate.Click += BtnUpdate_Click;
            pnlLeft.Controls.Add(btnUpdate);

            btnDelete = CreateButton("DELETE", Color.IndianRed, y + 100);
            btnDelete.Click += BtnDelete_Click;
            pnlLeft.Controls.Add(btnDelete);

            // --- RIGHT PANEL (LIST & HISTORY) ---
            pnlFill = new Panel();
            pnlFill.Dock = DockStyle.Fill;
            pnlFill.Padding = new Padding(20);
            this.Controls.Add(pnlFill);

            pnlLeft.SendToBack();
            pnlFill.BringToFront();

            // Top Search Bar
            Panel pnlAction = new Panel();
            pnlAction.Dock = DockStyle.Top;
            pnlAction.Height = 50;
            pnlFill.Controls.Add(pnlAction);

            txtSearch = new TextBox();
            txtSearch.Font = new Font("Segoe UI", 12);
            txtSearch.Size = new Size(300, 30);
            txtSearch.Location = new Point(0, 10);
            txtSearch.TextChanged += (s, e) => LoadMembers(txtSearch.Text);
            pnlAction.Controls.Add(txtSearch);

            Label lblSearch = new Label { Text = "🔍 Search by Name or ID/No", Location = new Point(310, 13), Font = new Font("Segoe UI", 10), ForeColor = Color.Gray, AutoSize = false, Size = new Size(250, 20) };
            lblSearch.AutoEllipsis = true;
            pnlAction.Controls.Add(lblSearch);

            // Split Container
            SplitContainer splitGrid = new SplitContainer();
            splitGrid.Dock = DockStyle.Fill;
            splitGrid.Orientation = Orientation.Horizontal;
            splitGrid.SplitterDistance = 350;
            splitGrid.SplitterWidth = 5;
            pnlFill.Controls.Add(splitGrid);
            splitGrid.BringToFront();

            // 1. Grid: Members
            gridMembers = CreateCustomGrid();
            gridMembers.CellClick += GridMembers_CellClick;
            splitGrid.Panel1.Controls.Add(gridMembers);

            // 2. Grid: Member's Activities
            Panel pnlHistoryHeader = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.WhiteSmoke };
            splitGrid.Panel2.Controls.Add(pnlHistoryHeader);

            Label lblHistory = new Label { Text = "SELECTED MEMBER'S CURRENT BOOKS & HISTORY", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.DimGray, Location = new Point(5, 7), AutoSize = true };
            pnlHistoryHeader.Controls.Add(lblHistory);

            gridMemberHistory = CreateCustomGrid();
            splitGrid.Panel2.Controls.Add(gridMemberHistory);
            gridMemberHistory.BringToFront();
        }

        private DataGridView CreateCustomGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.ReadOnly = true;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.ColumnHeadersHeight = 40;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            grid.EnableHeadersVisualStyles = false;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            return grid;
        }

        // =============================================================
        // 2. DATA LOADING
        // =============================================================
        private void LoadMembers(string search = "")
        {
            string query = @"
                SELECT 
                    MemberID, 
                    IdentityNo AS [ID / Student No], 
                    FirstName AS [First Name], 
                    Surname AS [Last Name], 
                    Phone AS [Phone], 
                    Email, 
                    PenaltyScore AS [Penalty Score],
                    RegistrationDate AS [Registration Date]
                FROM TBL_MEMBERS";

            if (!string.IsNullOrEmpty(search))
            {
                query += " WHERE FirstName LIKE @s OR Surname LIKE @s OR IdentityNo LIKE @s";
            }
            query += " ORDER BY MemberID DESC";

            DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@s", "%" + search + "%"));

            if (dt != null)
            {
                gridMembers.DataSource = dt;
                if (gridMembers.Columns["MemberID"] != null)
                    gridMembers.Columns["MemberID"].Visible = false;
            }
        }

        private void LoadMemberHistory(int memberId)
        {
            string query = @"
                SELECT 
                    B.Title AS [Book Title],
                    L.IssueDate AS [Issue Date],
                    L.DueDate AS [Due Date],
                    L.ReturnDate AS [Return Date],
                    CASE 
                        WHEN L.ReturnDate IS NULL AND GETDATE() > L.DueDate THEN 'OVERDUE'
                        WHEN L.ReturnDate IS NULL THEN 'ON HAND'
                        ELSE 'RETURNED'
                    END AS [Status]
                FROM TBL_LOANS L
                INNER JOIN TBL_BOOK_COPIES BC ON L.CopyID = BC.CopyID
                INNER JOIN TBL_BOOKS B ON BC.BookID = B.BookID
                WHERE L.MemberID = @mid
                ORDER BY L.IssueDate DESC";

            DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@mid", memberId));
            gridMemberHistory.DataSource = dt;
        }

        // =============================================================
        // 3. OPERATIONS (CRUD) AND LOGGING
        // =============================================================
        private void GridMembers_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= gridMembers.Rows.Count) return;

            DataGridViewRow row = gridMembers.Rows[e.RowIndex];
            if (row.Cells["MemberID"].Value == null) return;

            selectedMemberID = Convert.ToInt32(row.Cells["MemberID"].Value);

            txtIdentity.Text = row.Cells["ID / Student No"].Value?.ToString();
            txtName.Text = row.Cells["First Name"].Value?.ToString();
            txtSurname.Text = row.Cells["Last Name"].Value?.ToString();
            txtPhone.Text = row.Cells["Phone"].Value?.ToString();
            txtEmail.Text = row.Cells["Email"].Value?.ToString();
            lblPenaltyScore.Text = "Penalty Score: " + (row.Cells["Penalty Score"].Value?.ToString() ?? "0");

            LoadMemberHistory(selectedMemberID);
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtIdentity.Text) || string.IsNullOrEmpty(txtName.Text) || string.IsNullOrEmpty(txtSurname.Text))
            {
                MessageBox.Show("ID/Student No, First Name and Last Name fields are required.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Check for duplicate ID/Student No
            object existingIdentity = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_MEMBERS WHERE IdentityNo=@idno", new SqlParameter("@idno", txtIdentity.Text));
            if (existingIdentity != null && Convert.ToInt32(existingIdentity) > 0)
            {
                MessageBox.Show("This ID/Student No is already registered! Please check.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string query = "INSERT INTO TBL_MEMBERS (IdentityNo, FirstName, Surname, Phone, Email, PenaltyScore, RegistrationDate) VALUES (@idno, @n, @s, @p, @e, 0, GETDATE())";
                ExecuteCommand(query);

                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"New Member Added: {txtName.Text} {txtSurname.Text} (ID: {txtIdentity.Text})", "INSERT");
                MessageBox.Show("Member saved successfully.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while adding the member: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedMemberID == -1) { MessageBox.Show("Please select a member from the list.", "No Selection"); return; }

            string query = "UPDATE TBL_MEMBERS SET IdentityNo=@idno, FirstName=@n, Surname=@s, Phone=@p, Email=@e WHERE MemberID=@id";
            ExecuteCommand(query);

            LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Member Information Updated: ID {selectedMemberID} ({txtName.Text} {txtSurname.Text})", "UPDATE");
            MessageBox.Show("Member information updated.", "Success");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            // --- ADDITIONAL SECURITY CHECK (Double Check) ---
            if (DashboardForm.CurrentSessionRole != "Admin")
            {
                MessageBox.Show("You do not have permission for this operation!", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (selectedMemberID == -1) { MessageBox.Show("Please select a member from the list.", "No Selection"); return; }

            // Critical Check: Does the member have any books on hand?
            object activeLoanCount = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_LOANS WHERE MemberID=@id AND ReturnDate IS NULL", new SqlParameter("@id", selectedMemberID));
            if (activeLoanCount != null && Convert.ToInt32(activeLoanCount) > 0)
            {
                MessageBox.Show("This member has books that have not been returned yet! Record cannot be deleted.", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (MessageBox.Show("Are you sure you want to delete this member and all their history?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                string deletedMemberName = $"{txtName.Text} {txtSurname.Text}";

                string query = "DELETE FROM TBL_MEMBERS WHERE MemberID=@id";
                SQLHelper.ExecuteQuery(query, new SqlParameter("@id", selectedMemberID));

                LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"Member Deleted: ID {selectedMemberID} - {deletedMemberName}", "DELETE");

                LoadMembers();
                ClearInputs();
                MessageBox.Show("Member deleted.", "Information");
            }
        }

        private void ExecuteCommand(string query)
        {
            SqlParameter[] p = {
                new SqlParameter("@idno", txtIdentity.Text),
                new SqlParameter("@n", txtName.Text),
                new SqlParameter("@s", txtSurname.Text),
                new SqlParameter("@p", txtPhone.Text),
                new SqlParameter("@e", txtEmail.Text),
                new SqlParameter("@id", selectedMemberID)
            };
            SQLHelper.ExecuteQuery(query, p);
            LoadMembers();
            ClearInputs();
        }

        private void ClearInputs()
        {
            txtIdentity.Clear(); txtName.Clear(); txtSurname.Clear(); txtPhone.Clear(); txtEmail.Clear();
            selectedMemberID = -1;
            lblPenaltyScore.Text = "Penalty Score: 0";
            gridMemberHistory.DataSource = null;
        }

        // =============================================================
        // 4. UI HELPERS
        // =============================================================
        private void AddInput(string label, ref TextBox txt, int y)
        {
            Label lbl = new Label { Text = label, Location = new Point(20, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = false, Size = new Size(300, 20) };
            pnlLeft.Controls.Add(lbl);
            txt = new TextBox { Location = new Point(20, y + 20), Size = new Size(300, 28), Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
            pnlLeft.Controls.Add(txt);
        }

        private Button CreateButton(string text, Color color, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(20, y),
                Size = new Size(300, 40),
                BackColor = color,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
        }
    }
}