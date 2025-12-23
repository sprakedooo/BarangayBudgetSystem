-- Report Queries for BarangayBudgetSystem

-- Monthly Budget Utilization Report
SELECT
    f.FundCode,
    f.FundName,
    f.Category,
    f.AllocatedAmount AS Appropriation,
    COALESCE(SUM(CASE WHEN t.Status IN ('Approved', 'Completed') THEN t.Amount ELSE 0 END), 0) AS Obligations,
    COALESCE(SUM(CASE WHEN t.Status = 'Completed' THEN t.Amount ELSE 0 END), 0) AS Disbursements,
    f.AllocatedAmount - COALESCE(SUM(CASE WHEN t.Status IN ('Approved', 'Completed') THEN t.Amount ELSE 0 END), 0) AS UnobligatedBalance
FROM Funds f
LEFT JOIN Transactions t ON f.Id = t.FundId
    AND t.TransactionType = 'Expenditure'
    AND strftime('%Y-%m', t.TransactionDate) = @YearMonth
WHERE f.FiscalYear = @FiscalYear AND f.IsActive = 1
GROUP BY f.Id, f.FundCode, f.FundName, f.Category, f.AllocatedAmount
ORDER BY f.Category, f.FundName;

-- Quarterly Summary Report
SELECT
    f.Category,
    SUM(f.AllocatedAmount) AS TotalAppropriation,
    COALESCE(SUM(t_q.Obligations), 0) AS QuarterObligations,
    COALESCE(SUM(t_q.Disbursements), 0) AS QuarterDisbursements,
    COALESCE(SUM(t_ytd.Obligations), 0) AS YTDObligations,
    COALESCE(SUM(t_ytd.Disbursements), 0) AS YTDDisbursements
FROM Funds f
LEFT JOIN (
    SELECT FundId,
        SUM(CASE WHEN Status IN ('Approved', 'Completed') THEN Amount ELSE 0 END) AS Obligations,
        SUM(CASE WHEN Status = 'Completed' THEN Amount ELSE 0 END) AS Disbursements
    FROM Transactions
    WHERE TransactionType = 'Expenditure'
        AND (strftime('%m', TransactionDate) BETWEEN @QuarterStartMonth AND @QuarterEndMonth)
        AND strftime('%Y', TransactionDate) = @Year
    GROUP BY FundId
) t_q ON f.Id = t_q.FundId
LEFT JOIN (
    SELECT FundId,
        SUM(CASE WHEN Status IN ('Approved', 'Completed') THEN Amount ELSE 0 END) AS Obligations,
        SUM(CASE WHEN Status = 'Completed' THEN Amount ELSE 0 END) AS Disbursements
    FROM Transactions
    WHERE TransactionType = 'Expenditure'
        AND strftime('%Y', TransactionDate) = @Year
        AND strftime('%m', TransactionDate) <= @QuarterEndMonth
    GROUP BY FundId
) t_ytd ON f.Id = t_ytd.FundId
WHERE f.FiscalYear = @FiscalYear AND f.IsActive = 1
GROUP BY f.Category
ORDER BY f.Category;

-- Annual Budget Execution Report
SELECT
    f.FundCode,
    f.FundName,
    f.Category,
    f.AllocatedAmount AS OriginalAppropriation,
    0 AS Adjustments,
    f.AllocatedAmount AS AdjustedAppropriation,
    COALESCE(SUM(CASE WHEN t.Status IN ('Approved', 'Completed') THEN t.Amount ELSE 0 END), 0) AS TotalObligations,
    COALESCE(SUM(CASE WHEN t.Status = 'Completed' THEN t.Amount ELSE 0 END), 0) AS TotalDisbursements,
    f.AllocatedAmount - COALESCE(SUM(CASE WHEN t.Status IN ('Approved', 'Completed') THEN t.Amount ELSE 0 END), 0) AS UnobligatedBalance,
    CASE
        WHEN f.AllocatedAmount > 0
        THEN ROUND(COALESCE(SUM(CASE WHEN t.Status IN ('Approved', 'Completed') THEN t.Amount ELSE 0 END), 0) / f.AllocatedAmount * 100, 2)
        ELSE 0
    END AS ObligationRate
FROM Funds f
LEFT JOIN Transactions t ON f.Id = t.FundId
    AND t.TransactionType = 'Expenditure'
    AND strftime('%Y', t.TransactionDate) = @Year
WHERE f.FiscalYear = @FiscalYear AND f.IsActive = 1
GROUP BY f.Id, f.FundCode, f.FundName, f.Category, f.AllocatedAmount
ORDER BY f.Category, f.FundName;

-- Cash Flow Report
SELECT
    strftime('%Y-%m', t.TransactionDate) AS Period,
    SUM(CASE WHEN t.TransactionType = 'Appropriation' THEN t.Amount ELSE 0 END) AS Inflows,
    SUM(CASE WHEN t.TransactionType = 'Expenditure' AND t.Status = 'Completed' THEN t.Amount ELSE 0 END) AS Outflows,
    SUM(CASE WHEN t.TransactionType = 'Appropriation' THEN t.Amount ELSE 0 END) -
    SUM(CASE WHEN t.TransactionType = 'Expenditure' AND t.Status = 'Completed' THEN t.Amount ELSE 0 END) AS NetFlow
FROM Transactions t
WHERE strftime('%Y', t.TransactionDate) = @Year
GROUP BY strftime('%Y-%m', t.TransactionDate)
ORDER BY Period;

-- COA Compliance Report Data
SELECT
    r.ReportNumber,
    r.ReportTitle,
    r.ReportType,
    r.FiscalYear,
    r.PeriodStart,
    r.PeriodEnd,
    r.TotalAppropriation,
    r.TotalObligations,
    r.TotalDisbursements,
    r.UnobligatedBalance,
    r.Status,
    r.GeneratedAt,
    r.SubmittedAt,
    u.FullName AS GeneratedBy
FROM COAReports r
LEFT JOIN Users u ON r.GeneratedByUserId = u.Id
WHERE r.FiscalYear = @FiscalYear
    AND (@ReportType IS NULL OR r.ReportType = @ReportType)
ORDER BY r.GeneratedAt DESC;
