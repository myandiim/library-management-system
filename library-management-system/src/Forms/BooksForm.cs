using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using System.Data.SqlClient;
using Database_Proje.DataAccess;

namespace Database_Proje.Forms
{
    public partial class BooksForm : Form
    {
        // =============================================================
        // VARIABLES AND CONTROLS
        // =============================================================
        private Panel pnlLeft, pnlFill;
        private DataGridView gridBooks;
        private DataGridView gridCopies; // Second table to list copies

        // Input Fields
        private TextBox txtTitle, txtISBN, txtLocation, txtPageCount, txtYear, txtSearch;
        private ComboBox cmbCategory, cmbAuthor, cmbPublisher;

        // Buttons
        private Button btnAdd, btnUpdate, btnDelete, btnAddCopy, btnDeleteCopy;
        private Button btnAddCategory, btnAddAuthor, btnAddPublisher; // Add buttons for dropdowns
        private Label lblTotalCopies;
        private RadioButton rdoAvailable, rdoMaintenance, rdoLost; // For status change

        // Color Palette (Compatible with Dashboard)
        private Color primaryColor = Color.FromArgb(20, 30, 50);
        private Color accentColor = Color.FromArgb(41, 128, 185);

        private int selectedBookID = -1;
        private int selectedCopyID = -1; // Selected copy ID

        public BooksForm()
        {
            SetupForm();
            LoadComboBoxes();
            LoadBooks();
        }

        // =============================================================
        // 1. DESIGN (MODERN AND DASHBOARD COMPATIBLE)
        // =============================================================
        private void SetupForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.Dock = DockStyle.Fill;
            this.BackColor = Color.White;

            // --- LEFT PANEL (INPUT AREA) ---
            pnlLeft = new Panel();
            pnlLeft.Dock = DockStyle.Left;
            pnlLeft.Width = 350;
            pnlLeft.BackColor = Color.FromArgb(245, 245, 245);

            Panel pnlBorder = new Panel { Dock = DockStyle.Right, Width = 1, BackColor = Color.LightGray };
            pnlLeft.Controls.Add(pnlBorder);
            this.Controls.Add(pnlLeft);

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "BOOK OPERATIONS";
            lblTitle.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitle.ForeColor = primaryColor;
            lblTitle.Location = new Point(20, 20);
            lblTitle.AutoSize = false;
            lblTitle.Size = new Size(300, 25);
            pnlLeft.Controls.Add(lblTitle);

            // Inputs
            int y = 60;
            AddInput("ISBN (Barcode):", ref txtISBN, y); y += 60;
            AddInput("Book Title:", ref txtTitle, y); y += 60;
            AddDropdownWithAddButton("Category:", ref cmbCategory, ref btnAddCategory, y, "Category"); y += 60;
            AddDropdownWithAddButton("Author:", ref cmbAuthor, ref btnAddAuthor, y, "Author"); y += 60;
            AddDropdownWithAddButton("Publisher:", ref cmbPublisher, ref btnAddPublisher, y, "Publisher"); y += 60;

            AddInputSmall("Pages:", ref txtPageCount, 20, y);
            AddInputSmall("Year:", ref txtYear, 120, y);
            AddInputSmall("Shelf:", ref txtLocation, 220, y);
            y += 70;

            // CRUD Buttons
            btnAdd = CreateButton("SAVE", Color.SeaGreen, y);
            btnAdd.Click += BtnAdd_Click;
            pnlLeft.Controls.Add(btnAdd);

            btnUpdate = CreateButton("UPDATE", Color.Orange, y + 50);
            btnUpdate.Click += BtnUpdate_Click;
            pnlLeft.Controls.Add(btnUpdate);

            btnDelete = CreateButton("DELETE", Color.IndianRed, y + 100);
            btnDelete.Click += BtnDelete_Click;
            pnlLeft.Controls.Add(btnDelete);

            // --- RIGHT PANEL (LIST AND DETAILS) ---
            pnlFill = new Panel();
            pnlFill.Dock = DockStyle.Fill;
            pnlFill.Padding = new Padding(20);
            this.Controls.Add(pnlFill);

            // Z-Order
            pnlLeft.SendToBack();
            pnlFill.BringToFront();

            // Top Bar
            Panel pnlAction = new Panel();
            pnlAction.Dock = DockStyle.Top;
            pnlAction.Height = 60;
            pnlFill.Controls.Add(pnlAction);

            // Search
            txtSearch = new TextBox();
            txtSearch.Font = new Font("Segoe UI", 12);
            txtSearch.Size = new Size(300, 30);
            txtSearch.Location = new Point(0, 15);
            txtSearch.TextChanged += (s, e) => LoadBooks(txtSearch.Text);
            pnlAction.Controls.Add(txtSearch);

            Label lblSearch = new Label { Text = "🔍", Location = new Point(305, 18), Font = new Font("Segoe UI", 12), AutoSize = true };
            pnlAction.Controls.Add(lblSearch);

            // ADD STOCK
            btnAddCopy = new Button();
            btnAddCopy.Text = "+ ADD STOCK";
            btnAddCopy.BackColor = accentColor;
            btnAddCopy.ForeColor = Color.White;
            btnAddCopy.FlatStyle = FlatStyle.Flat;
            btnAddCopy.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btnAddCopy.Size = new Size(150, 35);
            btnAddCopy.Location = new Point(350, 13);
            btnAddCopy.Cursor = Cursors.Hand;
            btnAddCopy.Click += BtnAddCopy_Click;
            pnlAction.Controls.Add(btnAddCopy);

            lblTotalCopies = new Label();
            lblTotalCopies.Text = "Selected Book Stock: -";
            lblTotalCopies.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            lblTotalCopies.ForeColor = Color.Gray;
            lblTotalCopies.Location = new Point(520, 20);
            lblTotalCopies.AutoSize = false;
            lblTotalCopies.Size = new Size(200, 20);
            lblTotalCopies.AutoEllipsis = true;
            pnlAction.Controls.Add(lblTotalCopies);

            // --- GRID SECTION (SPLIT CONTAINER) ---
            // Split screen in two: Books on top, Copies at bottom
            SplitContainer splitGrid = new SplitContainer();
            splitGrid.Dock = DockStyle.Fill;
            splitGrid.Orientation = Orientation.Horizontal;
            splitGrid.SplitterDistance = 350; // Top section height
            splitGrid.SplitterWidth = 5;
            pnlFill.Controls.Add(splitGrid);
            splitGrid.BringToFront();

            // 1. GRID: BOOKS
            gridBooks = CreateCustomGrid();
            gridBooks.CellClick += GridBooks_CellClick;
            splitGrid.Panel1.Controls.Add(gridBooks);

            // 2. GRID: COPIES (BOTTOM PANEL)
            Panel pnlCopiesHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Color.WhiteSmoke };
            splitGrid.Panel2.Controls.Add(pnlCopiesHeader);

            Label lblCopiesTitle = new Label { Text = "SELECTED BOOK COPIES (INVENTORY)", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.DimGray, Location = new Point(5, 10), AutoSize = false, Size = new Size(280, 20) };
            lblCopiesTitle.AutoEllipsis = true;
            pnlCopiesHeader.Controls.Add(lblCopiesTitle);

            // Copy Delete Button
            btnDeleteCopy = new Button { Text = "Delete Selected Copy", BackColor = Color.IndianRed, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Size = new Size(150, 30), Location = new Point(300, 5), Cursor = Cursors.Hand, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
            btnDeleteCopy.Click += BtnDeleteCopy_Click;
            pnlCopiesHeader.Controls.Add(btnDeleteCopy);

            // Status Radio Buttons
            rdoAvailable = new RadioButton { Text = "Available", Location = new Point(480, 10), AutoSize = false, Size = new Size(80, 20) };
            rdoMaintenance = new RadioButton { Text = "Maintenance", Location = new Point(560, 10), AutoSize = false, Size = new Size(100, 20) };
            rdoLost = new RadioButton { Text = "Lost", Location = new Point(660, 10), AutoSize = false, Size = new Size(60, 20) };

            // Auto-update when status changes
            EventHandler statusChanged = (s, e) => { if (((RadioButton)s).Checked) UpdateCopyStatus(); };
            rdoAvailable.CheckedChanged += statusChanged;
            rdoMaintenance.CheckedChanged += statusChanged;
            rdoLost.CheckedChanged += statusChanged;

            pnlCopiesHeader.Controls.Add(rdoAvailable);
            pnlCopiesHeader.Controls.Add(rdoMaintenance);
            pnlCopiesHeader.Controls.Add(rdoLost);

            gridCopies = CreateCustomGrid();
            gridCopies.CellClick += GridCopies_CellClick;
            splitGrid.Panel2.Controls.Add(gridCopies);
            gridCopies.BringToFront();
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
            grid.AllowUserToAddRows = false; // Prevent empty rows
            grid.AllowUserToDeleteRows = false; // Prevent row deletion
            grid.ColumnHeadersHeight = 45;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = primaryColor;
            grid.EnableHeadersVisualStyles = false;
            grid.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 240, 250);
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            return grid;
        }

        // =============================================================
        // 2. DATA LOADING
        // =============================================================
        private void LoadComboBoxes()
        {
            try
            {
                DataTable dtCat = SQLHelper.GetTable("SELECT CategoryID, Name FROM TBL_CATEGORIES");
                cmbCategory.DataSource = dtCat; cmbCategory.DisplayMember = "Name"; cmbCategory.ValueMember = "CategoryID";

                DataTable dtAuth = SQLHelper.GetTable("SELECT AuthorID, FirstName + ' ' + Surname AS FullName FROM TBL_AUTHORS");
                cmbAuthor.DataSource = dtAuth; cmbAuthor.DisplayMember = "FullName"; cmbAuthor.ValueMember = "AuthorID";

                DataTable dtPub = SQLHelper.GetTable("SELECT PublisherID, Name FROM TBL_PUBLISHERS");
                cmbPublisher.DataSource = dtPub; cmbPublisher.DisplayMember = "Name"; cmbPublisher.ValueMember = "PublisherID";
            }
            catch { }
        }

        private void LoadBooks(string search = "")
        {
            string query = @"
                SELECT 
                    B.BookID, B.ISBN, B.Title AS [Book Title], 
                    C.Name AS [Category], A.FirstName + ' ' + A.Surname AS [Author], 
                    P.Name AS [Publisher], B.PageCount AS [Pages], B.PublicationYear AS [Year], B.Location AS [Location],
                    (SELECT COUNT(*) FROM TBL_BOOK_COPIES WHERE BookID = B.BookID) AS [Stock]
                FROM TBL_BOOKS B
                LEFT JOIN TBL_CATEGORIES C ON B.CategoryID = C.CategoryID
                LEFT JOIN TBL_AUTHORS A ON B.AuthorID = A.AuthorID
                LEFT JOIN TBL_PUBLISHERS P ON B.PublisherID = P.PublisherID
            ";

            if (!string.IsNullOrEmpty(search)) query += " WHERE B.Title LIKE @search OR B.ISBN LIKE @search";
            query += " ORDER BY B.BookID DESC";

            SqlParameter[] p = { new SqlParameter("@search", "%" + search + "%") };
            DataTable dt = SQLHelper.GetTable(query, p);

            if (dt != null)
            {
                gridBooks.DataSource = dt;
                if (gridBooks.Columns["BookID"] != null) gridBooks.Columns["BookID"].Visible = false;
            }
        }

        // Load copies of selected book
        private void LoadCopies(int bookId)
        {
            string query = "SELECT CopyID AS [Barcode No], Status AS [Status] FROM TBL_BOOK_COPIES WHERE BookID = @id";
            DataTable dt = SQLHelper.GetTable(query, new SqlParameter("@id", bookId));
            gridCopies.DataSource = dt;
            selectedCopyID = -1; // Reset selection
            ResetRadioButtons();
        }

        private void GridBooks_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Prevent error when clicking on empty rows or header
            if (e.RowIndex < 0 || e.RowIndex >= gridBooks.Rows.Count) return;
            
            DataGridViewRow row = gridBooks.Rows[e.RowIndex];
            
            // Check if row is empty or has null values
            if (row.Cells["BookID"].Value == null || row.Cells["BookID"].Value == DBNull.Value) return;
            
            try
            {
                selectedBookID = Convert.ToInt32(row.Cells["BookID"].Value);

                txtISBN.Text = row.Cells["ISBN"].Value?.ToString() ?? "";
                txtTitle.Text = row.Cells["Book Title"].Value?.ToString() ?? "";
                txtPageCount.Text = row.Cells["Pages"].Value?.ToString() ?? "";
                txtYear.Text = row.Cells["Year"].Value?.ToString() ?? "";
                txtLocation.Text = row.Cells["Location"].Value?.ToString() ?? "";
                cmbCategory.Text = row.Cells["Category"].Value?.ToString() ?? "";
                cmbAuthor.Text = row.Cells["Author"].Value?.ToString() ?? "";
                cmbPublisher.Text = row.Cells["Publisher"].Value?.ToString() ?? "";

                lblTotalCopies.Text = "Selected Book Stock: " + (row.Cells["Stock"].Value?.ToString() ?? "0");

                // Load copies
                LoadCopies(selectedBookID);
            }
            catch (Exception ex)
            {
                // Silently handle errors (empty rows, etc.)
                System.Diagnostics.Debug.WriteLine("Error loading book data: " + ex.Message);
            }
        }

        private void GridCopies_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            // Prevent error when clicking on empty rows or header
            if (e.RowIndex < 0 || e.RowIndex >= gridCopies.Rows.Count) return;
            
            DataGridViewRow row = gridCopies.Rows[e.RowIndex];
            
            // Check if row is empty or has null values
            if (row.Cells["Barcode No"].Value == null || row.Cells["Barcode No"].Value == DBNull.Value) return;
            
            try
            {
                selectedCopyID = Convert.ToInt32(row.Cells["Barcode No"].Value);
                string status = row.Cells["Status"].Value?.ToString() ?? "Available";

                // Select radio button based on status
                if (status == "Available") rdoAvailable.Checked = true;
                else if (status == "Maintenance") rdoMaintenance.Checked = true;
                else if (status == "Lost") rdoLost.Checked = true;
            }
            catch (Exception ex)
            {
                // Silently handle errors (empty rows, etc.)
                System.Diagnostics.Debug.WriteLine("Error loading copy data: " + ex.Message);
            }
        }

        // =============================================================
        // 3. OPERATIONS
        // =============================================================
        private void BtnAdd_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtTitle.Text) || string.IsNullOrEmpty(txtISBN.Text))
            {
                MessageBox.Show("Please fill in ISBN and Book Title fields!", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning); return;
            }

            // Check for duplicate ISBN
            object existingISBN = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_BOOKS WHERE ISBN=@isbn", new SqlParameter("@isbn", txtISBN.Text));
            if (existingISBN != null && Convert.ToInt32(existingISBN) > 0)
            {
                MessageBox.Show("This ISBN number is already in use! Please enter a different ISBN.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string query = @"INSERT INTO TBL_BOOKS (ISBN, Title, CategoryID, AuthorID, PublisherID, PageCount, PublicationYear, Location) VALUES (@isbn, @title, @cat, @auth, @pub, @page, @year, @loc)";
                ExecuteBookCommand(query);
                MessageBox.Show("Book saved successfully.", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while adding the book: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            if (selectedBookID == -1) return;
            string query = @"UPDATE TBL_BOOKS SET ISBN=@isbn, Title=@title, CategoryID=@cat, AuthorID=@auth, PublisherID=@pub, PageCount=@page, PublicationYear=@year, Location=@loc WHERE BookID=@bookId";
            object pageVal = string.IsNullOrEmpty(txtPageCount.Text) ? (object)DBNull.Value : Convert.ToInt32(txtPageCount.Text);
            object yearVal = string.IsNullOrEmpty(txtYear.Text) ? (object)DBNull.Value : Convert.ToInt32(txtYear.Text);

            SqlParameter[] p = {
                new SqlParameter("@isbn", txtISBN.Text), new SqlParameter("@title", txtTitle.Text),
                new SqlParameter("@cat", cmbCategory.SelectedValue ?? DBNull.Value), new SqlParameter("@auth", cmbAuthor.SelectedValue ?? DBNull.Value),
                new SqlParameter("@pub", cmbPublisher.SelectedValue ?? DBNull.Value), new SqlParameter("@page", pageVal),
                new SqlParameter("@year", yearVal), new SqlParameter("@loc", txtLocation.Text),
                new SqlParameter("@bookId", selectedBookID)
            };
            SQLHelper.ExecuteQuery(query, p);
            LoadBooks(); ClearInputs();
            MessageBox.Show("Book updated successfully.", "Success");
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (selectedBookID == -1) return;
            if (MessageBox.Show("Are you sure you want to delete this book and ALL its stock?", "Critical Operation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                // First check if any copies are currently on loan
                object activeLoanCount = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_LOANS L INNER JOIN TBL_BOOK_COPIES BC ON L.CopyID = BC.CopyID WHERE BC.BookID = @id AND L.ReturnDate IS NULL", new SqlParameter("@id", selectedBookID));
                if (activeLoanCount != null && Convert.ToInt32(activeLoanCount) > 0)
                {
                    MessageBox.Show("Some copies of this book are currently on loan! Returns must be processed first.", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SQLHelper.ExecuteQuery("DELETE FROM TBL_BOOK_COPIES WHERE BookID=@id", new SqlParameter("@id", selectedBookID));
                SQLHelper.ExecuteQuery("DELETE FROM TBL_BOOKS WHERE BookID=@id", new SqlParameter("@id", selectedBookID));
                LoadBooks(); ClearInputs();
                gridCopies.DataSource = null; // Clear copy list as well
                MessageBox.Show("Record deleted.", "Information");
            }
        }

        private void BtnAddCopy_Click(object sender, EventArgs e)
        {
            if (selectedBookID == -1) { MessageBox.Show("Please select a book from the list first!", "Warning"); return; }
            SQLHelper.ExecuteQuery("INSERT INTO TBL_BOOK_COPIES (BookID, Status) VALUES (@id, 'Available')", new SqlParameter("@id", selectedBookID));
            LoadBooks(); // Update main list (stock count increases)
            LoadCopies(selectedBookID); // Update copy list
        }

        // Copy Deletion (Stock Reduction)
        private void BtnDeleteCopy_Click(object sender, EventArgs e)
        {
            if (selectedCopyID == -1) { MessageBox.Show("Please select a copy from the list below to delete.", "No Selection"); return; }

            // First check if this copy is currently on loan
            object activeLoan = SQLHelper.ExecuteScalar("SELECT COUNT(*) FROM TBL_LOANS WHERE CopyID=@id AND ReturnDate IS NULL", new SqlParameter("@id", selectedCopyID));
            if (activeLoan != null && Convert.ToInt32(activeLoan) > 0)
            {
                MessageBox.Show("This copy is currently on loan! Return must be processed first.", "Blocked", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show($"Are you sure you want to delete copy with Barcode No: {selectedCopyID}?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                SQLHelper.ExecuteQuery("DELETE FROM TBL_BOOK_COPIES WHERE CopyID=@id", new SqlParameter("@id", selectedCopyID));
                LoadBooks(); // Update main list (stock decreases)
                LoadCopies(selectedBookID); // Update copy list
                MessageBox.Show("Copy deleted.", "Information");
            }
        }

        // Copy Status Update (Available -> Maintenance etc.)
        private void UpdateCopyStatus()
        {
            if (selectedCopyID == -1) return;

            string newStatus = "Available";
            if (rdoMaintenance.Checked) newStatus = "Maintenance";
            else if (rdoLost.Checked) newStatus = "Lost";

            SQLHelper.ExecuteQuery("UPDATE TBL_BOOK_COPIES SET Status=@st WHERE CopyID=@id",
                new SqlParameter("@st", newStatus), new SqlParameter("@id", selectedCopyID));

            // Refresh grid (selection preservation logic can be added later, for now directly loading)
            LoadCopies(selectedBookID);
        }

        private void ExecuteBookCommand(string query)
        {
            // This method is now only used for INSERT, UPDATE has a separate method
            object pageVal = string.IsNullOrEmpty(txtPageCount.Text) ? (object)DBNull.Value : Convert.ToInt32(txtPageCount.Text);
            object yearVal = string.IsNullOrEmpty(txtYear.Text) ? (object)DBNull.Value : Convert.ToInt32(txtYear.Text);

            SqlParameter[] p = {
                new SqlParameter("@isbn", txtISBN.Text), new SqlParameter("@title", txtTitle.Text),
                new SqlParameter("@cat", cmbCategory.SelectedValue ?? DBNull.Value), new SqlParameter("@auth", cmbAuthor.SelectedValue ?? DBNull.Value),
                new SqlParameter("@pub", cmbPublisher.SelectedValue ?? DBNull.Value), new SqlParameter("@page", pageVal),
                new SqlParameter("@year", yearVal), new SqlParameter("@loc", txtLocation.Text)
            };
            SQLHelper.ExecuteQuery(query, p);
            LoadBooks(); ClearInputs();
        }

        private void ClearInputs()
        {
            txtISBN.Clear(); txtTitle.Clear(); txtPageCount.Clear(); txtYear.Clear(); txtLocation.Clear();
            selectedBookID = -1; lblTotalCopies.Text = "Selected Book Stock: -";
            gridCopies.DataSource = null; // Clear detail table
        }

        private void ResetRadioButtons()
        {
            rdoAvailable.Checked = false; rdoMaintenance.Checked = false; rdoLost.Checked = false;
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

        private void AddDropdown(string label, ref ComboBox cmb, int y)
        {
            Label lbl = new Label { Text = label, Location = new Point(20, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = true };
            pnlLeft.Controls.Add(lbl);
            cmb = new ComboBox { Location = new Point(20, y + 20), Size = new Size(300, 28), Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
            pnlLeft.Controls.Add(cmb);
        }

        // Add dropdown with + button for adding new items
        private void AddDropdownWithAddButton(string label, ref ComboBox cmb, ref Button btnAdd, int y, string type)
        {
            Label lbl = new Label { Text = label, Location = new Point(20, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = false, Size = new Size(260, 20) };
            pnlLeft.Controls.Add(lbl);
            
            // ComboBox with reduced width to make room for + button
            cmb = new ComboBox { Location = new Point(20, y + 20), Size = new Size(260, 28), Font = new Font("Segoe UI", 10), DropDownStyle = ComboBoxStyle.DropDownList };
            pnlLeft.Controls.Add(cmb);
            
            // + Button next to ComboBox
            btnAdd = new Button
            {
                Text = "+",
                Location = new Point(285, y + 20),
                Size = new Size(35, 28),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Tag = type // Store type for event handler
            };
            
            // Create event handler based on type
            if (type == "Category")
                btnAdd.Click += (s, e) => BtnAddNewItem_Click(s, e, "Category");
            else if (type == "Author")
                btnAdd.Click += (s, e) => BtnAddNewItem_Click(s, e, "Author");
            else if (type == "Publisher")
                btnAdd.Click += (s, e) => BtnAddNewItem_Click(s, e, "Publisher");
                
            pnlLeft.Controls.Add(btnAdd);
        }

        private void AddInputSmall(string label, ref TextBox txt, int x, int y)
        {
            Label lbl = new Label { Text = label, Location = new Point(x, y), Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.Gray, AutoSize = false, Size = new Size(90, 20) };
            pnlLeft.Controls.Add(lbl);
            txt = new TextBox { Location = new Point(x, y + 20), Size = new Size(90, 28), Font = new Font("Segoe UI", 10), BorderStyle = BorderStyle.FixedSingle };
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

        // =============================================================
        // 5. ADD NEW ITEM DIALOGS (Category, Author, Publisher)
        // =============================================================
        private void BtnAddNewItem_Click(object sender, EventArgs e, string type)
        {
            Form dialog = new Form
            {
                Text = $"Add New {type}",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            Panel pnlMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };
            dialog.Controls.Add(pnlMain);

            Label lblName = new Label { Text = $"{type} Name:", Location = new Point(10, 20), AutoSize = false, Size = new Size(350, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
            pnlMain.Controls.Add(lblName);

            TextBox txtName = new TextBox { Location = new Point(10, 45), Size = new Size(350, 30), Font = new Font("Segoe UI", 11) };
            pnlMain.Controls.Add(txtName);
            txtName.Focus(); // Focus on first textbox

            // For Author, add First Name and Surname fields
            TextBox txtSurname = null;
            if (type == "Author")
            {
                lblName.Text = "First Name:";
                Label lblSurname = new Label { Text = "Surname:", Location = new Point(10, 85), AutoSize = false, Size = new Size(350, 20), Font = new Font("Segoe UI", 10, FontStyle.Bold) };
                pnlMain.Controls.Add(lblSurname);
                txtSurname = new TextBox { Location = new Point(10, 110), Size = new Size(350, 30), Font = new Font("Segoe UI", 11) };
                pnlMain.Controls.Add(txtSurname);
                dialog.Height = 250;
                
                // When Enter is pressed in first name, move to surname
                txtName.KeyDown += (s, ke) => { if (ke.KeyCode == Keys.Enter) { txtSurname.Focus(); ke.Handled = true; } };
            }

            Button btnSave = new Button
            {
                Text = "Save",
                Location = new Point(200, type == "Author" ? 150 : 90),
                Size = new Size(80, 35),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            pnlMain.Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(290, type == "Author" ? 150 : 90),
                Size = new Size(80, 35),
                BackColor = Color.Gray,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.Cancel
            };
            pnlMain.Controls.Add(btnCancel);

            dialog.AcceptButton = btnSave;
            dialog.CancelButton = btnCancel;
            
            // Set focus to first textbox when dialog opens
            dialog.Shown += (s, evt) => txtName.Focus();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    if (type == "Category")
                    {
                        if (string.IsNullOrWhiteSpace(txtName.Text))
                        {
                            MessageBox.Show("Please enter a category name.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        SQLHelper.ExecuteQuery("INSERT INTO TBL_CATEGORIES (Name) VALUES (@name)", new SqlParameter("@name", txtName.Text.Trim()));
                        MessageBox.Show("Category added successfully.", "Success");
                    }
                    else if (type == "Author")
                    {
                        if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtSurname.Text))
                        {
                            MessageBox.Show("Please enter both first name and surname.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        SQLHelper.ExecuteQuery("INSERT INTO TBL_AUTHORS (FirstName, Surname) VALUES (@fn, @sn)", 
                            new SqlParameter("@fn", txtName.Text.Trim()), 
                            new SqlParameter("@sn", txtSurname.Text.Trim()));
                        MessageBox.Show("Author added successfully.", "Success");
                    }
                    else if (type == "Publisher")
                    {
                        if (string.IsNullOrWhiteSpace(txtName.Text))
                        {
                            MessageBox.Show("Please enter a publisher name.", "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                        SQLHelper.ExecuteQuery("INSERT INTO TBL_PUBLISHERS (Name) VALUES (@name)", new SqlParameter("@name", txtName.Text.Trim()));
                        MessageBox.Show("Publisher added successfully.", "Success");
                    }

                    // Reload combo boxes to show new item
                    LoadComboBoxes();
                    
                    // Select the newly added item
                    if (type == "Category" && cmbCategory.Items.Count > 0)
                        cmbCategory.SelectedIndex = cmbCategory.Items.Count - 1;
                    else if (type == "Author" && cmbAuthor.Items.Count > 0)
                        cmbAuthor.SelectedIndex = cmbAuthor.Items.Count - 1;
                    else if (type == "Publisher" && cmbPublisher.Items.Count > 0)
                        cmbPublisher.SelectedIndex = cmbPublisher.Items.Count - 1;

                    LogHelper.LogProcess(DashboardForm.CurrentSessionUser, $"New {type} Added: {txtName.Text}", "INSERT");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred while adding {type.ToLower()}: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}