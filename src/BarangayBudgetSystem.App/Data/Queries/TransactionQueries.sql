-- Transaction Queries for BarangayBudgetSystem

-- Get transactions with fund details
SELECT
    t.*,
    f.FundCode,
    f.FundName,
    f.Category AS FundCategory,
    u1.FullName AS CreatedByName,
    u2.FullName AS ApprovedByName
FROM Transactions t
LEFT JOIN Funds f ON t.FundId = f.Id
LEFT JOIN Users u1 ON t.CreatedByUserId = u1.Id
LEFT JOIN Users u2 ON t.ApprovedByUserId = u2.Id
WHERE (@Status IS NULL OR t.Status = @Status)
    AND (@FundId IS NULL OR t.FundId = @FundId)
    AND (@StartDate IS NULL OR t.TransactionDate >= @StartDate)
    AND (@EndDate IS NULL OR t.TransactionDate <= @EndDate)
ORDER BY t.TransactionDate DESC, t.CreatedAt DESC;

-- Get transaction summary by month
SELECT
    strftime('%Y-%m', TransactionDate) AS Month,
    TransactionType,
    COUNT(*) AS TransactionCount,
    SUM(Amount) AS TotalAmount
FROM Transactions
WHERE FundId = @FundId
    AND strftime('%Y', TransactionDate) = @Year
    AND Status IN ('Approved', 'Completed')
GROUP BY strftime('%Y-%m', TransactionDate), TransactionType
ORDER BY Month;

-- Get pending transactions for approval
SELECT
    t.*,
    f.FundCode,
    f.FundName,
    u.FullName AS CreatedByName
FROM Transactions t
LEFT JOIN Funds f ON t.FundId = f.Id
LEFT JOIN Users u ON t.CreatedByUserId = u.Id
WHERE t.Status = 'For Approval'
ORDER BY t.CreatedAt ASC;

-- Get recent transactions (dashboard)
SELECT
    t.Id,
    t.TransactionNumber,
    t.TransactionType,
    t.Description,
    t.Amount,
    t.TransactionDate,
    t.Status,
    f.FundName
FROM Transactions t
LEFT JOIN Funds f ON t.FundId = f.Id
ORDER BY t.CreatedAt DESC
LIMIT 10;

-- Get transaction statistics
SELECT
    COUNT(*) AS TotalTransactions,
    COUNT(CASE WHEN Status = 'Pending' THEN 1 END) AS PendingCount,
    COUNT(CASE WHEN Status = 'For Approval' THEN 1 END) AS ForApprovalCount,
    COUNT(CASE WHEN Status = 'Approved' THEN 1 END) AS ApprovedCount,
    COUNT(CASE WHEN Status = 'Completed' THEN 1 END) AS CompletedCount,
    SUM(CASE WHEN TransactionType = 'Expenditure' AND Status IN ('Approved', 'Completed') THEN Amount ELSE 0 END) AS TotalExpenditures
FROM Transactions
WHERE strftime('%Y', TransactionDate) = @Year;

-- Generate next transaction number
SELECT
    'TXN-' || strftime('%Y%m', 'now') || '-' ||
    printf('%04d', COALESCE(MAX(CAST(substr(TransactionNumber, -4) AS INTEGER)), 0) + 1)
AS NextNumber
FROM Transactions
WHERE TransactionNumber LIKE 'TXN-' || strftime('%Y%m', 'now') || '-%';

-- Search transactions
SELECT
    t.*,
    f.FundCode,
    f.FundName
FROM Transactions t
LEFT JOIN Funds f ON t.FundId = f.Id
WHERE t.TransactionNumber LIKE '%' || @SearchTerm || '%'
    OR t.Description LIKE '%' || @SearchTerm || '%'
    OR t.Payee LIKE '%' || @SearchTerm || '%'
    OR t.PRNumber LIKE '%' || @SearchTerm || '%'
    OR t.PONumber LIKE '%' || @SearchTerm || '%'
    OR t.DVNumber LIKE '%' || @SearchTerm || '%'
ORDER BY t.TransactionDate DESC
LIMIT 50;
