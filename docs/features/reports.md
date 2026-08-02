# Reports & Analytics Feature Specification

## Purpose
This document specifies report generation endpoints, historical trend queries, cash flow analysis, and export data models in Fingo.

## Business Rules
1. Reports aggregate transaction trends over customizable date ranges (Monthly, Quarterly, Yearly, or Custom start/end dates).
2. Cash flow analysis calculates total inflows vs total outflows per time bucket.
3. Category trend reports calculate month-over-month category spending changes.
4. Export options support CSV data formatting.

## User Stories
- **US-REP-01:** As a user, I want to generate a Cash Flow report showing monthly Income vs Expense comparison.
- **US-REP-02:** As a user, I want to see my Net Worth growth trend line over the past 12 months.
- **US-REP-03:** As a user, I want to export my transaction ledger report to CSV format.

## Screens
- Analytics & Reports Hub (`/reports`)
- Cash Flow Report View (`/reports/cash-flow`)
- Net Worth Report View (`/reports/net-worth`)

## Navigation Flow
```
[ Dashboard ] ---> Click "Reports" ---> [ Reports Hub ]
                                             |
                   +-------------------------+-------------------------+
                   |                                                   |
         Click "Cash Flow"                                   Click "Export Data"
                   v                                                   v
        [ Cash Flow Chart View ]                             [ Download CSV File ]
```

## Database Tables
- Operates as a read-only analytical query engine over `transactions`, `accounts`, and `categories`.

## Relationships
- Analytical projections over existing schema.

## API Endpoints

| HTTP Method | Route | Description | Auth Required |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/reports/cash-flow` | Generates historical monthly cash flow trends | Authorized |
| `GET` | `/api/reports/category-expenses` | Generates category breakdown report over custom date range | Authorized |
| `GET` | `/api/reports/export/transactions` | Exports filtered transaction history as CSV download | Authorized |

## Request DTOs

```csharp
namespace Fingo.BackendApi.Features.Reports.GetCashFlowReport;

public record GetCashFlowReportRequest(
    DateTime StartDate,
    DateTime EndDate
);
```

## Response DTOs

```csharp
namespace Fingo.BackendApi.Features.Reports.GetCashFlowReport;

public record CashFlowReportResponse(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal NetSavings,
    List<MonthlyCashFlowItemDto> MonthlyData
);

public record MonthlyCashFlowItemDto(
    int Year,
    int Month,
    string PeriodLabel, // e.g., "Aug 2026"
    decimal Income,
    decimal Expense,
    decimal NetCashFlow
);
```

## Validation Rules

```csharp
using FluentValidation;

namespace Fingo.BackendApi.Features.Reports.GetCashFlowReport;

public class GetCashFlowReportValidator : AbstractValidator<GetCashFlowReportRequest>
{
    public GetCashFlowReportValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("EndDate must be greater than or equal to StartDate.");
    }
}
```

## Authorization
- All report queries strictly filter by authenticated user ID (`WHERE user_id = @UserId`).

## Business Logic
1. **Get Cash Flow Report Handler:**
   - Executes Dapper query grouping transactions by year and month:
   ```sql
   SELECT 
       EXTRACT(YEAR FROM transaction_date)::int AS Year,
       EXTRACT(MONTH FROM transaction_date)::int AS Month,
       SUM(CASE WHEN transaction_type = 'Income' THEN amount ELSE 0 END) AS Income,
       SUM(CASE WHEN transaction_type = 'Expense' THEN amount ELSE 0 END) AS Expense
   FROM transactions
   WHERE user_id = @UserId 
     AND transaction_date BETWEEN @StartDate AND @EndDate
     AND is_deleted = false
   GROUP BY Year, Month
   ORDER BY Year ASC, Month ASC;
   ```
   - Projects list into `CashFlowReportResponse`.

## Edge Cases
- **Periods with Zero Activity:** Handler generates zero-value placeholder buckets for empty months within the requested date range.

## Error Scenarios
- **Invalid Date Range:** Returns HTTP 400 Bad Request.

## Future Improvements
- PDF summary report generation.

## Checklists

### Definition of Done
- [x] Efficient SQL aggregation queries verified.
- [x] CSV export endpoint returning `text/csv` stream verified.

### Testing Checklist
- [x] Verify date range filtering includes start and end boundaries.

## Dependencies
- Dapper, PostgreSQL, `IDbConnectionFactory`.

## Related Documents
- [Transactions Feature Spec](transactions.md)
- [Dashboard Feature Spec](dashboard.md)
