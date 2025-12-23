-- Fund Queries for BarangayBudgetSystem
-- These queries can be used with Dapper or raw ADO.NET if needed

-- Get all funds for a fiscal year
SELECT * FROM Funds
WHERE FiscalYear = @FiscalYear AND IsActive = 1
ORDER BY Category, FundName;

-- Get fund summary by category
SELECT
    Category,
    COUNT(*) AS FundCount,
    SUM(AllocatedAmount) AS TotalAllocated,
    SUM(UtilizedAmount) AS TotalUtilized,
    SUM(AllocatedAmount - UtilizedAmount) AS TotalRemaining
FROM Funds
WHERE FiscalYear = @FiscalYear AND IsActive = 1
GROUP BY Category
ORDER BY Category;

-- Get fund utilization report
SELECT
    f.Id,
    f.FundCode,
    f.FundName,
    f.Category,
    f.AllocatedAmount,
    f.UtilizedAmount,
    (f.AllocatedAmount - f.UtilizedAmount) AS RemainingBalance,
    CASE
        WHEN f.AllocatedAmount > 0
        THEN ROUND((f.UtilizedAmount / f.AllocatedAmount) * 100, 2)
        ELSE 0
    END AS UtilizationPercentage
FROM Funds f
WHERE f.FiscalYear = @FiscalYear AND f.IsActive = 1
ORDER BY f.Category, f.FundName;

-- Get funds with low balance (less than 20% remaining)
SELECT * FROM Funds
WHERE FiscalYear = @FiscalYear
    AND IsActive = 1
    AND AllocatedAmount > 0
    AND ((AllocatedAmount - UtilizedAmount) / AllocatedAmount) < 0.20
ORDER BY ((AllocatedAmount - UtilizedAmount) / AllocatedAmount);

-- Update fund utilization based on transactions
UPDATE Funds
SET UtilizedAmount = (
    SELECT COALESCE(SUM(t.Amount), 0)
    FROM Transactions t
    WHERE t.FundId = Funds.Id
        AND t.TransactionType = 'Expenditure'
        AND t.Status IN ('Approved', 'Completed')
),
UpdatedAt = datetime('now')
WHERE Id = @FundId;

-- Get total budget summary
SELECT
    SUM(AllocatedAmount) AS TotalBudget,
    SUM(UtilizedAmount) AS TotalExpenses,
    SUM(AllocatedAmount - UtilizedAmount) AS TotalRemaining,
    CASE
        WHEN SUM(AllocatedAmount) > 0
        THEN ROUND((SUM(UtilizedAmount) / SUM(AllocatedAmount)) * 100, 2)
        ELSE 0
    END AS OverallUtilization
FROM Funds
WHERE FiscalYear = @FiscalYear AND IsActive = 1;
