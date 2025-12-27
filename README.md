# library-management-system
DATABASE SYSTEMS PROJECT: ADVANCED LIBRARY MANAGEMENT SYSTEM

1. PROJECT DESCRIPTION
----------------------
This project is a professional Library Management System developed using C# (.NET) 
and Microsoft SQL Server. It features a relational database architecture 
consisting of 13 interconnected tables. Key features include:
- Automated Book Loaning and Returning via Stored Procedures.
- Real-time Penalty Score calculation and Fine management.
- Individual tracking of physical book copies (Available, OnLoan, Maintenance).
- Member eligibility checks and role-based access for library staff.

2. FOLDER CONTENTS (Directory Structure)
-----------------------------------------
library-management-system/ (Root Folder)
- src/                    : Folder containing source code
    - Daraaccess/
    - Forms/
    - Proporties/
    - program.cs
- docs/                    : Folder containing report of the library management system project
    - repor-page.pdf
- Database/                : Folder containing library database schema sql
    - librarydatabase_schema.sql

3. HOW TO RUN
-------------
IMPORTANT: Follow these steps in order to ensure the application works correctly.

1. DATABASE SETUP:
   a. Open 'Microsoft SQL Server Management Studio (SSMS)'.
   b. Open the 'librarydatabase_schema.sql' file from the root folder.
   c.Use the source codes with visual studio (.sln)

2. APPLICATION EXECUTION:
   a. Open the 'Database_Proje' folder.
   b. Double-click on 'Database_Proje.exe' to launch the application.
   c. If a blue Windows screen appears ("Windows protected your PC"):
      - Click on "More Info" (Ek Bilgi).
      - Click on "Run anyway" (Yine de çalıştır).
   d. The application will start successfully.

4. TECHNICAL FEATURES & MAPPING
-------------------------------
- Relational Mapping: ER diagrams are mapped into 13 tables ensuring 3rd Normal Form (3NF).
- Efficient Logic: Core business logic (SP_IssueBook, SP_ReturnBook) is handled 
  via Stored Procedures on the server-side for maximum efficiency.
