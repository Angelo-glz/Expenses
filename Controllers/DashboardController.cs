using Gastos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Gastos.Controllers
{
  public class DashboardController : Controller
  {

    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
      _context = context;
    }

    public async Task<ActionResult> Index()
    {
      //last 7 days
      DateTime StartDate = DateTime.Today.AddDays(-6); 
      DateTime EndDate = DateTime.Now; 

      List<Transaction> SelectedTransactions = await _context.Transactions 
        .Include(x => x.Category)
        .Where(y => y.Date >= StartDate && y.Date <= EndDate)
        .ToListAsync();


      //Total Income 
      int TotalIncome = SelectedTransactions 
        .Where(i => i.Category.Type == "Income") 
        .Sum(a => a.Amount); 
      ViewBag.TotalIncome = TotalIncome.ToString("c2");

      //Total Expense
      int TotalExpense = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .Sum(a => a.Amount);
      ViewBag.TotalExpense = TotalExpense.ToString("c2");

      //Balance
      int Balance = TotalIncome - TotalExpense;
      CultureInfo culture = CultureInfo.CreateSpecificCulture("en-US");
      culture.NumberFormat.CurrencyNegativePattern = 1;
      ViewBag.Balance = String.Format(culture, "{0:C}", Balance);

      //Doughnut chart - expense by category
      ViewBag.DoughnutChartData = SelectedTransactions
        .Where(x => x.Category.Type == "Expense")
        .GroupBy(y => y.Category.CategoryId)
        .Select(k => new
        {
          categoryTitleWithIcon = k.First().Category.Icon + " " + k.First().Category.Title,
          amount = k.Sum(x => x.Amount),
          formattedAmount = k.Sum(x => x.Amount).ToString("c2"),
        })
        .OrderByDescending(l => l.amount)
        .ToList();


      //Spline Chart - Income and Expense
      //Income
      List<SplineChartData> IncomeSummary = SelectedTransactions
        .Where(i => i.Category.Type == "Income")
        .GroupBy(j => j.Date)
        .Select(k => new SplineChartData()
        {
          day = k.First().Date.ToString("dd-MMM"),
          income = k.Sum(l => l.Amount)
        }).ToList();

      //Expense
      List<SplineChartData> ExpenseSummary = SelectedTransactions
        .Where(i => i.Category.Type == "Expense")
        .GroupBy(j => j.Date)
        .Select(k => new SplineChartData()
        {
          day = k.First().Date.ToString("dd-MMM"),
          expense = k.Sum(l => l.Amount)
        }).ToList();

      string[] Last7Days = Enumerable.Range(0, 7)
        .Select(i => StartDate.AddDays(i).ToString("dd-MMM"))
        .ToArray();

      ViewBag.SplineChartData = from day in Last7Days
                                join income in IncomeSummary on day equals income.day into dayIncomeJoined
                                from income in dayIncomeJoined.DefaultIfEmpty()
                                join expense in ExpenseSummary on day equals expense.day into expenseJoined
                                from expense in expenseJoined.DefaultIfEmpty()
                                select new
                                {
                                  day = day,
                                  income = income == null ? 0 : income.income,
                                  expense = expense == null ? 0 : expense.expense,
                                };

      //Recent Transactions
      ViewBag.RecentTransactions = await _context.Transactions
        .Include(i => i.Category)
        .OrderByDescending(j => j.Date)
        .Take(7)
        .ToListAsync();


      return View();

    }
  }

  public class SplineChartData
  {
    public string day;
    public int income;
    public int expense;
  }


}
