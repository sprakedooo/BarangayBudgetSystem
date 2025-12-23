# Entity Relationship Diagram

## Database Schema

```
┌─────────────────────┐       ┌─────────────────────┐
│       Users         │       │  AppropriationFunds │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │       │ Id (PK)             │
│ Username            │       │ FundCode (Unique)   │
│ PasswordHash        │       │ FundName            │
│ FirstName           │       │ Description         │
│ LastName            │       │ AllocatedAmount     │
│ Email               │       │ UtilizedAmount      │
│ Role                │       │ FiscalYear          │
│ IsActive            │       │ Category            │
│ CreatedAt           │       │ IsActive            │
│ LastLoginAt         │       │ CreatedAt           │
└─────────────────────┘       │ UpdatedAt           │
         │                    └─────────────────────┘
         │                             │
         │ CreatedBy                   │ FundId
         │ ApprovedBy                  │
         ▼                             ▼
┌─────────────────────────────────────────────────────┐
│                    Transactions                      │
├─────────────────────────────────────────────────────┤
│ Id (PK)                                             │
│ TransactionNumber (Unique)                          │
│ FundId (FK -> AppropriationFunds.Id)               │
│ TransactionType                                     │
│ Description                                         │
│ Payee                                               │
│ Amount                                              │
│ TransactionDate                                     │
│ Status                                              │
│ PRNumber                                            │
│ PONumber                                            │
│ DVNumber                                            │
│ CheckNumber                                         │
│ CheckDate                                           │
│ Remarks                                             │
│ CreatedByUserId (FK -> Users.Id)                   │
│ ApprovedByUserId (FK -> Users.Id)                  │
│ CreatedAt                                           │
│ UpdatedAt                                           │
│ ApprovedAt                                          │
└─────────────────────────────────────────────────────┘
         │
         │ TransactionId
         ▼
┌─────────────────────┐
│    Attachments      │
├─────────────────────┤
│ Id (PK)             │
│ TransactionId (FK)  │
│ FileName            │
│ OriginalFileName    │
│ FilePath            │
│ ContentType         │
│ FileSize            │
│ AttachmentType      │
│ Description         │
│ UploadedByUserId    │
│ UploadedAt          │
│ IsDeleted           │
│ DeletedAt           │
└─────────────────────┘

┌─────────────────────┐       ┌─────────────────────┐
│     COAReports      │       │  COAReportDetails   │
├─────────────────────┤       ├─────────────────────┤
│ Id (PK)             │◄──────│ Id (PK)             │
│ ReportNumber        │       │ ReportId (FK)       │
│ ReportTitle         │       │ FundId (FK)         │
│ ReportType          │       │ Appropriation       │
│ FiscalYear          │       │ Obligations         │
│ Month               │       │ Disbursements       │
│ Quarter             │       │ Balance             │
│ PeriodStart         │       └─────────────────────┘
│ PeriodEnd           │
│ TotalAppropriation  │
│ TotalObligations    │
│ TotalDisbursements  │
│ UnobligatedBalance  │
│ FilePath            │
│ Status              │
│ GeneratedByUserId   │
│ GeneratedAt         │
│ SubmittedAt         │
│ Notes               │
└─────────────────────┘
```

## Relationships

1. **Users -> Transactions** (One-to-Many)
   - A user can create many transactions
   - A user can approve many transactions

2. **AppropriationFunds -> Transactions** (One-to-Many)
   - A fund can have many transactions
   - Each transaction belongs to one fund

3. **Transactions -> Attachments** (One-to-Many)
   - A transaction can have many attachments
   - Each attachment belongs to one transaction

4. **COAReports -> COAReportDetails** (One-to-Many)
   - A report has many detail lines (one per fund)
   - Each detail line belongs to one report

5. **AppropriationFunds -> COAReportDetails** (One-to-Many)
   - A fund can appear in many report details
   - Each detail references one fund

## Indexes

### AppropriationFunds
- FundCode (Unique)
- FiscalYear
- Category

### Transactions
- TransactionNumber (Unique)
- TransactionDate
- Status
- PRNumber
- PONumber
- DVNumber

### Attachments
- TransactionId
- AttachmentType

### COAReports
- ReportNumber (Unique)
- FiscalYear
- ReportType

### Users
- Username (Unique)
- Email

## Enumerations

### TransactionTypes
- Expenditure
- Appropriation
- Adjustment
- Transfer
- Reversal

### TransactionStatus
- Pending
- For Approval
- Approved
- Rejected
- Cancelled
- Completed

### FundCategories
- General Fund
- Special Education Fund
- Trust Fund
- SK Fund
- Disaster Fund
- Development Fund
- Personnel Services
- MOOE
- Capital Outlay

### AttachmentTypes
- Purchase Request
- Purchase Order
- Disbursement Voucher
- Receipt
- Invoice
- Supporting Document
- COA Report
- Other

### UserRoles
- Administrator
- Treasurer
- Accountant
- Budget Officer
- Encoder
- Viewer

### ReportTypes
- Monthly
- Quarterly
- Annual
- Special

### ReportStatus
- Draft
- Generated
- Reviewed
- Submitted
- Archived
