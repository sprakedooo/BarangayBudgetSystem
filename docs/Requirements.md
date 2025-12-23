# Barangay Budget System - Requirements Documentation

## Overview

The Barangay Budget System is a comprehensive desktop application designed to help Philippine barangays manage their budget, track expenditures, and generate COA-compliant reports.

## System Requirements

### Hardware Requirements
- Processor: Intel Core i3 or equivalent (minimum)
- RAM: 4 GB (minimum), 8 GB (recommended)
- Storage: 500 MB free disk space for application, additional space for data
- Display: 1366x768 resolution (minimum), 1920x1080 (recommended)

### Software Requirements
- Operating System: Windows 10 or later
- .NET 8.0 Runtime
- Microsoft Word (optional, for document viewing)

## Functional Requirements

### 1. Fund Management (FR-001)
- Create, read, update, and delete appropriation funds
- Track fund allocation and utilization
- Support multiple fund categories (General Fund, SK Fund, etc.)
- Calculate remaining balance automatically
- Visual indicators for fund utilization percentage

### 2. Transaction Management (FR-002)
- Record budget transactions (expenditures, appropriations, adjustments)
- Support transaction workflow (Pending -> For Approval -> Approved -> Completed)
- Link transactions to specific funds
- Track PR, PO, and DV numbers
- Support file attachments for supporting documents

### 3. Report Generation (FR-003)
- Generate monthly budget utilization reports
- Generate quarterly summary reports
- Generate annual budget execution reports
- COA-compliant report formats
- Export reports to Word documents

### 4. Document Generation (FR-004)
- Generate Purchase Request (PR) documents
- Generate Purchase Order (PO) documents
- Generate Disbursement Voucher (DV) documents
- Auto-generate document numbers
- Customizable barangay information

### 5. User Management (FR-005)
- Role-based access control
- Support for multiple user roles:
  - Administrator
  - Treasurer
  - Accountant
  - Budget Officer
  - Encoder
  - Viewer

### 6. Backup and Recovery (FR-006)
- Manual backup creation
- Automatic backup scheduling
- Backup restoration
- Backup cleanup/retention policies

## Non-Functional Requirements

### Performance (NFR-001)
- Application startup: < 5 seconds
- Transaction save: < 2 seconds
- Report generation: < 10 seconds
- Search results: < 1 second

### Security (NFR-002)
- Password encryption for user accounts
- Role-based permissions
- Audit trail for sensitive operations

### Usability (NFR-003)
- Intuitive navigation with sidebar menu
- Responsive UI with loading indicators
- Clear error messages
- Keyboard navigation support

### Reliability (NFR-004)
- Automatic database backup
- Data validation on all inputs
- Graceful error handling
- Transaction rollback on failures

### Maintainability (NFR-005)
- MVVM architecture for separation of concerns
- Dependency injection for loose coupling
- Comprehensive logging
- Modular service design

## Data Model

### AppropriationFund
- FundCode (unique identifier)
- FundName
- Description
- AllocatedAmount
- UtilizedAmount
- FiscalYear
- Category
- IsActive

### Transaction
- TransactionNumber
- FundId (FK)
- TransactionType
- Description
- Payee
- Amount
- TransactionDate
- Status
- PRNumber, PONumber, DVNumber
- CheckNumber, CheckDate

### Attachment
- FileName
- FilePath
- ContentType
- FileSize
- AttachmentType
- TransactionId (FK)

### COAReport
- ReportNumber
- ReportTitle
- ReportType (Monthly/Quarterly/Annual)
- FiscalYear
- PeriodStart, PeriodEnd
- TotalAppropriation
- TotalObligations
- TotalDisbursements

### User
- Username
- PasswordHash
- FirstName, LastName
- Role
- IsActive

## User Interface

### Main Navigation
1. Dashboard - Overview with charts and statistics
2. Transactions - Transaction management
3. Funds - Fund management
4. Reports - Report generation
5. Documents - Document generation and attachments
6. Settings - Application settings and backup

### Dashboard Features
- Total budget summary cards
- Budget distribution pie chart
- Monthly expenses bar chart
- Low balance funds alerts
- Recent transactions list
- Pending approvals count

## Technology Stack

- **Framework**: .NET 8.0
- **UI**: WPF (Windows Presentation Foundation)
- **Architecture**: MVVM (Model-View-ViewModel)
- **Database**: SQLite with Entity Framework Core
- **Document Generation**: OpenXML SDK
- **Charts**: LiveCharts2
- **DI Container**: Microsoft.Extensions.DependencyInjection

## Future Enhancements

1. Multi-user network support
2. Cloud backup integration
3. Mobile companion app
4. Email notifications
5. Dashboard customization
6. Report templates editor
7. Audit log viewer
8. Data import/export (Excel)
