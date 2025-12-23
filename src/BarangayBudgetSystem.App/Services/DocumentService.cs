using System;
using System.IO;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using BarangayBudgetSystem.App.Models;

namespace BarangayBudgetSystem.App.Services
{
    public interface IDocumentService
    {
        Task<string> GeneratePurchaseRequestAsync(Transaction transaction, string barangayName, string outputPath);
        Task<string> GeneratePurchaseOrderAsync(Transaction transaction, string barangayName, string outputPath);
        Task<string> GenerateDisbursementVoucherAsync(Transaction transaction, string barangayName, string outputPath);
        Task<string> GenerateCOAReportDocumentAsync(COAReport report, string barangayName, string outputPath);
    }

    public class DocumentService : IDocumentService
    {
        private readonly string _templatesPath;

        public DocumentService()
        {
            _templatesPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Resources", "Templates");
        }

        public async Task<string> GeneratePurchaseRequestAsync(Transaction transaction, string barangayName, string outputPath)
        {
            return await Task.Run(() =>
            {
                var fileName = $"PR_{transaction.PRNumber ?? transaction.TransactionNumber}_{DateTime.Now:yyyyMMdd}.docx";
                var filePath = Path.Combine(outputPath, fileName);

                using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Header
                AddParagraph(body, $"Republic of the Philippines", true, JustificationValues.Center);
                AddParagraph(body, $"Province of _____________", false, JustificationValues.Center);
                AddParagraph(body, $"Municipality of _____________", false, JustificationValues.Center);
                AddParagraph(body, barangayName, true, JustificationValues.Center);
                AddParagraph(body, "", false, JustificationValues.Center);
                AddParagraph(body, "PURCHASE REQUEST", true, JustificationValues.Center, "28");
                AddParagraph(body, "", false, JustificationValues.Center);

                // PR Details
                AddParagraph(body, $"PR No.: {transaction.PRNumber ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, $"Date: {transaction.TransactionDate:MMMM dd, yyyy}", false, JustificationValues.Left);
                AddParagraph(body, $"Fund: {transaction.Fund?.FundName ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Table header
                AddParagraph(body, "ITEM DESCRIPTION / PURPOSE:", true, JustificationValues.Left);
                AddParagraph(body, transaction.Description, false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Amount
                AddParagraph(body, $"Amount Requested: PHP {transaction.Amount:N2}", true, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Payee
                AddParagraph(body, $"Payee/Supplier: {transaction.Payee ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Remarks
                if (!string.IsNullOrEmpty(transaction.Remarks))
                {
                    AddParagraph(body, $"Remarks: {transaction.Remarks}", false, JustificationValues.Left);
                    AddParagraph(body, "", false, JustificationValues.Left);
                }

                // Signatures
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "Requested by:", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "Signature over Printed Name", false, JustificationValues.Left);
                AddParagraph(body, "Designation: _______________", false, JustificationValues.Left);

                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "Approved by:", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "PUNONG BARANGAY", true, JustificationValues.Left);

                mainPart.Document.Save();
                return filePath;
            });
        }

        public async Task<string> GeneratePurchaseOrderAsync(Transaction transaction, string barangayName, string outputPath)
        {
            return await Task.Run(() =>
            {
                var fileName = $"PO_{transaction.PONumber ?? transaction.TransactionNumber}_{DateTime.Now:yyyyMMdd}.docx";
                var filePath = Path.Combine(outputPath, fileName);

                using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Header
                AddParagraph(body, $"Republic of the Philippines", true, JustificationValues.Center);
                AddParagraph(body, $"Province of _____________", false, JustificationValues.Center);
                AddParagraph(body, $"Municipality of _____________", false, JustificationValues.Center);
                AddParagraph(body, barangayName, true, JustificationValues.Center);
                AddParagraph(body, "", false, JustificationValues.Center);
                AddParagraph(body, "PURCHASE ORDER", true, JustificationValues.Center, "28");
                AddParagraph(body, "", false, JustificationValues.Center);

                // PO Details
                AddParagraph(body, $"PO No.: {transaction.PONumber ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, $"Date: {DateTime.Now:MMMM dd, yyyy}", false, JustificationValues.Left);
                AddParagraph(body, $"PR Reference: {transaction.PRNumber ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Supplier
                AddParagraph(body, "To:", true, JustificationValues.Left);
                AddParagraph(body, transaction.Payee ?? "_______________", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Items
                AddParagraph(body, "Please deliver the following:", true, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "ITEM DESCRIPTION:", true, JustificationValues.Left);
                AddParagraph(body, transaction.Description, false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Amount
                AddParagraph(body, $"Total Amount: PHP {transaction.Amount:N2}", true, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Terms
                AddParagraph(body, "Delivery Terms: _______________", false, JustificationValues.Left);
                AddParagraph(body, "Payment Terms: _______________", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Signatures
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "PUNONG BARANGAY", true, JustificationValues.Left);

                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "Conforme:", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "Supplier's Signature", false, JustificationValues.Left);

                mainPart.Document.Save();
                return filePath;
            });
        }

        public async Task<string> GenerateDisbursementVoucherAsync(Transaction transaction, string barangayName, string outputPath)
        {
            return await Task.Run(() =>
            {
                var fileName = $"DV_{transaction.DVNumber ?? transaction.TransactionNumber}_{DateTime.Now:yyyyMMdd}.docx";
                var filePath = Path.Combine(outputPath, fileName);

                using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Header
                AddParagraph(body, $"Republic of the Philippines", true, JustificationValues.Center);
                AddParagraph(body, $"Province of _____________", false, JustificationValues.Center);
                AddParagraph(body, $"Municipality of _____________", false, JustificationValues.Center);
                AddParagraph(body, barangayName, true, JustificationValues.Center);
                AddParagraph(body, "", false, JustificationValues.Center);
                AddParagraph(body, "DISBURSEMENT VOUCHER", true, JustificationValues.Center, "28");
                AddParagraph(body, "", false, JustificationValues.Center);

                // DV Details
                AddParagraph(body, $"DV No.: {transaction.DVNumber ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, $"Date: {DateTime.Now:MMMM dd, yyyy}", false, JustificationValues.Left);
                AddParagraph(body, $"Fund: {transaction.Fund?.FundName ?? "_______________"}", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Payee
                AddParagraph(body, $"Payee: {transaction.Payee ?? "_______________"}", true, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Particulars
                AddParagraph(body, "PARTICULARS:", true, JustificationValues.Left);
                AddParagraph(body, transaction.Description, false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // References
                if (!string.IsNullOrEmpty(transaction.PRNumber))
                    AddParagraph(body, $"PR No.: {transaction.PRNumber}", false, JustificationValues.Left);
                if (!string.IsNullOrEmpty(transaction.PONumber))
                    AddParagraph(body, $"PO No.: {transaction.PONumber}", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Amount
                AddParagraph(body, $"Amount: PHP {transaction.Amount:N2}", true, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Check details
                if (!string.IsNullOrEmpty(transaction.CheckNumber))
                {
                    AddParagraph(body, $"Check No.: {transaction.CheckNumber}", false, JustificationValues.Left);
                    AddParagraph(body, $"Check Date: {transaction.CheckDate:MMMM dd, yyyy}", false, JustificationValues.Left);
                    AddParagraph(body, "", false, JustificationValues.Left);
                }

                // Certification
                AddParagraph(body, "CERTIFIED:", true, JustificationValues.Left);
                AddParagraph(body, "Expenses/Cash Advance necessary, lawful and incurred under my direct supervision.", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "PUNONG BARANGAY", true, JustificationValues.Left);

                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "ACCOUNTING ENTRY:", true, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "BARANGAY TREASURER", true, JustificationValues.Left);

                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "Received Payment:", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "Payee's Signature / Date", false, JustificationValues.Left);

                mainPart.Document.Save();
                return filePath;
            });
        }

        public async Task<string> GenerateCOAReportDocumentAsync(COAReport report, string barangayName, string outputPath)
        {
            return await Task.Run(() =>
            {
                var fileName = $"COA_{report.ReportNumber}_{DateTime.Now:yyyyMMdd}.docx";
                var filePath = Path.Combine(outputPath, fileName);

                using var document = WordprocessingDocument.Create(filePath, WordprocessingDocumentType.Document);
                var mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document();
                var body = mainPart.Document.AppendChild(new Body());

                // Header
                AddParagraph(body, $"Republic of the Philippines", true, JustificationValues.Center);
                AddParagraph(body, $"COMMISSION ON AUDIT", true, JustificationValues.Center);
                AddParagraph(body, "", false, JustificationValues.Center);
                AddParagraph(body, barangayName, true, JustificationValues.Center);
                AddParagraph(body, "", false, JustificationValues.Center);
                AddParagraph(body, report.ReportTitle, true, JustificationValues.Center, "24");
                AddParagraph(body, "", false, JustificationValues.Center);

                // Report Info
                AddParagraph(body, $"Report No.: {report.ReportNumber}", false, JustificationValues.Left);
                AddParagraph(body, $"Fiscal Year: {report.FiscalYear}", false, JustificationValues.Left);
                AddParagraph(body, $"Period Covered: {report.PeriodStart:MMMM dd, yyyy} to {report.PeriodEnd:MMMM dd, yyyy}", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Summary
                AddParagraph(body, "SUMMARY OF BUDGET UTILIZATION", true, JustificationValues.Center);
                AddParagraph(body, "", false, JustificationValues.Left);

                AddParagraph(body, $"Total Appropriation: PHP {report.TotalAppropriation:N2}", false, JustificationValues.Left);
                AddParagraph(body, $"Total Obligations: PHP {report.TotalObligations:N2}", false, JustificationValues.Left);
                AddParagraph(body, $"Total Disbursements: PHP {report.TotalDisbursements:N2}", false, JustificationValues.Left);
                AddParagraph(body, $"Unobligated Balance: PHP {report.UnobligatedBalance:N2}", false, JustificationValues.Left);

                var utilizationRate = report.TotalAppropriation > 0
                    ? (double)(report.TotalObligations / report.TotalAppropriation) * 100
                    : 0;
                AddParagraph(body, $"Utilization Rate: {utilizationRate:F2}%", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);

                // Details
                if (report.Details.Count > 0)
                {
                    AddParagraph(body, "DETAILED BREAKDOWN BY FUND", true, JustificationValues.Center);
                    AddParagraph(body, "", false, JustificationValues.Left);

                    foreach (var detail in report.Details)
                    {
                        AddParagraph(body, $"Fund: {detail.Fund?.FundName ?? "N/A"}", true, JustificationValues.Left);
                        AddParagraph(body, $"  Appropriation: PHP {detail.Appropriation:N2}", false, JustificationValues.Left);
                        AddParagraph(body, $"  Obligations: PHP {detail.Obligations:N2}", false, JustificationValues.Left);
                        AddParagraph(body, $"  Disbursements: PHP {detail.Disbursements:N2}", false, JustificationValues.Left);
                        AddParagraph(body, $"  Balance: PHP {detail.Balance:N2}", false, JustificationValues.Left);
                        AddParagraph(body, $"  Utilization: {detail.UtilizationRate:F2}%", false, JustificationValues.Left);
                        AddParagraph(body, "", false, JustificationValues.Left);
                    }
                }

                // Signatures
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "Prepared by:", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "BARANGAY TREASURER", true, JustificationValues.Left);

                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "Certified Correct:", false, JustificationValues.Left);
                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, "_______________________________", false, JustificationValues.Left);
                AddParagraph(body, "PUNONG BARANGAY", true, JustificationValues.Left);

                AddParagraph(body, "", false, JustificationValues.Left);
                AddParagraph(body, $"Date Generated: {report.GeneratedAt:MMMM dd, yyyy HH:mm}", false, JustificationValues.Right);

                mainPart.Document.Save();
                return filePath;
            });
        }

        private void AddParagraph(Body body, string text, bool bold, JustificationValues justification, string? fontSize = null)
        {
            var paragraph = new Paragraph();
            var paragraphProperties = new ParagraphProperties
            {
                Justification = new Justification { Val = justification }
            };
            paragraph.AppendChild(paragraphProperties);

            var run = new Run();
            var runProperties = new RunProperties();

            if (bold)
            {
                runProperties.AppendChild(new Bold());
            }

            if (!string.IsNullOrEmpty(fontSize))
            {
                runProperties.AppendChild(new FontSize { Val = fontSize });
            }

            run.AppendChild(runProperties);
            run.AppendChild(new Text(text) { Space = SpaceProcessingModeValues.Preserve });

            paragraph.AppendChild(run);
            body.AppendChild(paragraph);
        }
    }
}
