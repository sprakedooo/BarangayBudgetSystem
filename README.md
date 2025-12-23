# Barangay Budget System

A comprehensive WPF desktop application for managing barangay budgets in the Philippines. This system helps barangay officials track appropriation funds, manage transactions, generate COA-compliant reports, and create official documents.

## Features

- **Dashboard**: Real-time overview of budget status with charts and statistics
- **Fund Management**: Create and manage appropriation funds with automatic utilization tracking
- **Transaction Management**: Record, approve, and track budget transactions
- **Report Generation**: Generate monthly, quarterly, and annual COA reports
- **Document Generation**: Create PR, PO, and DV documents automatically
- **Backup & Restore**: Secure your data with built-in backup functionality

## Technology Stack

- .NET 8.0
- WPF (Windows Presentation Foundation)
- MVVM Architecture
- Entity Framework Core with SQLite
- LiveCharts2 for data visualization
- OpenXML SDK for document generation

## Prerequisites

- Windows 10 or later
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022 (recommended) or VS Code

## Getting Started

### Clone the Repository

```bash
git clone https://github.com/yourusername/BarangayBudgetSystem.git
cd BarangayBudgetSystem
```

### Build the Project

```bash
cd src/BarangayBudgetSystem.App
dotnet restore
dotnet build
```

### Run the Application

```bash
dotnet run
```

Or open `BarangayBudgetSystem.sln` in Visual Studio and press F5.

## Project Structure

```
BarangayBudgetSystem/
├── src/
│   └── BarangayBudgetSystem.App/
│       ├── Models/          # Data models
│       ├── ViewModels/      # MVVM ViewModels
│       ├── Views/           # XAML Views
│       ├── Services/        # Business logic services
│       ├── Data/            # Database context and queries
│       ├── Helpers/         # Utility classes and converters
│       ├── Styles/          # XAML resource dictionaries
│       ├── Components/      # Reusable UI components
│       └── Resources/       # Icons, images, templates
├── storage/
│   ├── attachments/         # Transaction attachments
│   ├── reports/             # Generated reports
│   ├── logs/                # Application logs
│   └── backups/             # Database backups
├── tests/                   # Unit tests
└── docs/                    # Documentation
```

## User Roles

| Role | Permissions |
|------|-------------|
| Administrator | Full access to all features |
| Treasurer | Manage funds, approve transactions, generate reports |
| Accountant | Generate reports, view transactions |
| Budget Officer | Manage funds, approve transactions |
| Encoder | Create and edit transactions |
| Viewer | View-only access |

## Default Login

- **Username**: admin
- **Password**: admin123

*Please change the default password after first login.*

## Fund Categories

- General Fund
- Special Education Fund (SEF)
- Trust Fund
- SK Fund
- Disaster Fund
- Development Fund
- Personnel Services
- MOOE (Maintenance and Other Operating Expenses)
- Capital Outlay

## Generated Documents

The system can generate the following official documents:

1. **Purchase Request (PR)** - Request for procurement
2. **Purchase Order (PO)** - Order to supplier
3. **Disbursement Voucher (DV)** - Payment voucher
4. **COA Reports** - Commission on Audit compliance reports

## Backup

The system stores data locally in SQLite. Regular backups are recommended:

1. Go to **Settings** > **Backup & Restore**
2. Click **Create Backup**
3. Backups are saved to `storage/backups/`

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, please open an issue in the GitHub repository or contact the development team.

## Acknowledgments

- Commission on Audit (COA) for report format guidelines
- Department of Interior and Local Government (DILG) for barangay financial management guidelines
