using doanwebnangcao.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OfficeOpenXml; // Thư viện EPPlus
using OfficeOpenXml.Style; // Để định dạng Excel
using System.IO;

namespace doanwebnangcao.Controllers
{
    public class ThongKeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThongKeController()
        {
            _context = new ApplicationDbContext();
        }

        // GET: thongke
        public ActionResult ThongKe(int? month = null)
        {
            // Kiểm tra session
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("DangNhap", "Home");
            }

            // Tạo danh sách tất cả 12 tháng
            var months = Enumerable.Range(1, 12).Select(m => new { Value = m, Text = $"Tháng {m}" }).ToList();
            ViewBag.Months = new SelectList(months, "Value", "Text", month);

            // Aggregate order statistics by month
            var orderStats = _context.Orders
                .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                .Select(g => new OrderStatsViewModel
                {
                    MonthYear = g.Key.Month + "/" + g.Key.Year,
                    PlacedOrders = g.Count(o => o.Status != OrderStatus.Cancelled),
                    CancelledOrders = g.Count(o => o.Status == OrderStatus.Cancelled),
                    TotalOrders = g.Count()
                })
                .OrderBy(s => s.MonthYear)
                .ToList();

            // Aggregate revenue statistics by month
            var revenueStats = _context.Payments
                .GroupBy(p => new { Year = p.PaymentDate.Year, Month = p.PaymentDate.Month })
                .Select(g => new OrderStatsViewModel
                {
                    MonthYear = g.Key.Month + "/" + g.Key.Year,
                    PaidRevenue = g.Where(p => p.Status == "Đã thanh toán").Sum(p => (decimal?)p.Amount) ?? 0,
                    UnpaidRevenue = g.Where(p => p.Status == "Chưa thanh toán").Sum(p => (decimal?)p.Amount) ?? 0,
                    TotalRevenue = 0
                })
                .OrderBy(s => s.MonthYear)
                .ToList();

            foreach (var stat in revenueStats)
            {
                stat.TotalRevenue = stat.PaidRevenue + stat.UnpaidRevenue;
            }

            // Filter by selected month if provided
            if (month.HasValue)
            {
                orderStats = orderStats.Where(s => int.Parse(s.MonthYear.Split('/')[0]) == month.Value).ToList();
                revenueStats = revenueStats.Where(s => int.Parse(s.MonthYear.Split('/')[0]) == month.Value).ToList();
            }

            // Prepare data for revenue bar chart
            var currentYear = DateTime.Now.Year;
            var revenueChartData = new List<object>();
            if (!month.HasValue)
            {
                // All months of current year
                for (int m = 1; m <= 12; m++)
                {
                    var stat = revenueStats.FirstOrDefault(s => int.Parse(s.MonthYear.Split('/')[0]) == m && s.MonthYear.Contains(currentYear.ToString()));
                    revenueChartData.Add(new
                    {
                        Month = m,
                        PaidRevenue = stat?.PaidRevenue ?? 0,
                        UnpaidRevenue = stat?.UnpaidRevenue ?? 0
                    });
                }
            }
            else
            {
                // Only selected month
                var stat = revenueStats.FirstOrDefault(s => int.Parse(s.MonthYear.Split('/')[0]) == month.Value);
                revenueChartData.Add(new
                {
                    Month = month.Value,
                    PaidRevenue = stat?.PaidRevenue ?? 0,
                    UnpaidRevenue = stat?.UnpaidRevenue ?? 0
                });
            }

            // Prepare data for order status pie chart
            var orderChartData = new
            {
                PlacedCount = month.HasValue
                    ? _context.Orders
                        .Where(o => o.OrderDate.Month == month.Value)
                        .Count(o => o.Status != OrderStatus.Cancelled)
                    : _context.Orders.Count(o => o.Status != OrderStatus.Cancelled),
                CancelledCount = month.HasValue
                    ? _context.Orders
                        .Where(o => o.OrderDate.Month == month.Value)
                        .Count(o => o.Status == OrderStatus.Cancelled)
                    : _context.Orders.Count(o => o.Status == OrderStatus.Cancelled)
            };

            // Pass data to view
            ViewBag.OrderStats = orderStats;
            ViewBag.RevenueStats = revenueStats;
            ViewBag.RevenueChartData = revenueChartData;
            ViewBag.OrderChartData = orderChartData;
            ViewBag.SelectedMonth = month;

            return View();
        }

        // Action để xuất file Excel
        public ActionResult ExportToExcel(string type, int? year)
        {
            // Kiểm tra session
            if (Session["Role"] == null || Session["Role"].ToString() != "Admin")
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập trang này.";
                return RedirectToAction("DangNhap", "Home");
            }

            // Nếu không chọn năm, mặc định là năm hiện tại
            year = year ?? DateTime.Now.Year;

            // Lấy dữ liệu đơn hàng
            var orderStatsQuery = _context.Orders
                .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                .Select(g => new OrderStatsViewModel
                {
                    MonthYear = g.Key.Month + "/" + g.Key.Year,
                    PlacedOrders = g.Count(o => o.Status != OrderStatus.Cancelled),
                    CancelledOrders = g.Count(o => o.Status == OrderStatus.Cancelled),
                    TotalOrders = g.Count()
                })
                .Where(s => s.MonthYear.Contains(year.ToString()))
                .OrderBy(s => s.MonthYear)
                .ToList();

            // Lấy dữ liệu doanh thu
            var revenueStatsQuery = _context.Payments
                .GroupBy(p => new { Year = p.PaymentDate.Year, Month = p.PaymentDate.Month })
                .Select(g => new OrderStatsViewModel
                {
                    MonthYear = g.Key.Month + "/" + g.Key.Year,
                    PaidRevenue = g.Where(p => p.Status == "Đã thanh toán").Sum(p => (decimal?)p.Amount) ?? 0,
                    UnpaidRevenue = g.Where(p => p.Status == "Chưa thanh toán").Sum(p => (decimal?)p.Amount) ?? 0,
                    TotalRevenue = 0
                })
                .Where(s => s.MonthYear.Contains(year.ToString()))
                .OrderBy(s => s.MonthYear)
                .ToList();

            foreach (var stat in revenueStatsQuery)
            {
                stat.TotalRevenue = stat.PaidRevenue + stat.UnpaidRevenue;
            }

            // Tạo file Excel
            using (var package = new ExcelPackage())
            {
                // Tạo sheet cho đơn hàng
                var orderWorksheet = package.Workbook.Worksheets.Add("Thống kê Đơn hàng");
                orderWorksheet.Cells[1, 1].Value = $"Thống kê Đơn hàng Theo Tháng (Năm {year})";
                orderWorksheet.Cells[1, 1].Style.Font.Size = 16;
                orderWorksheet.Cells[1, 1].Style.Font.Bold = true;
                orderWorksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                orderWorksheet.Cells[1, 1, 1, 4].Merge = true;
                orderWorksheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                orderWorksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkBlue);
                orderWorksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                orderWorksheet.Cells[3, 1].Value = "Thời gian";
                orderWorksheet.Cells[3, 2].Value = "Đơn đã đặt";
                orderWorksheet.Cells[3, 3].Value = "Đơn hủy";
                orderWorksheet.Cells[3, 4].Value = "Tổng số đơn";
                orderWorksheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;
                orderWorksheet.Cells[3, 1, 3, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                orderWorksheet.Cells[3, 1, 3, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                orderWorksheet.Cells[3, 1, 3, 4].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                orderWorksheet.Cells[3, 1, 3, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                orderWorksheet.Cells[3, 1, 3, 4].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                orderWorksheet.Cells[3, 1, 3, 4].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                int row = 4;
                foreach (var stat in orderStatsQuery)
                {
                    orderWorksheet.Cells[row, 1].Value = stat.MonthYear;
                    orderWorksheet.Cells[row, 2].Value = stat.PlacedOrders;
                    orderWorksheet.Cells[row, 3].Value = stat.CancelledOrders;
                    orderWorksheet.Cells[row, 4].Value = stat.TotalOrders;
                    orderWorksheet.Cells[row, 1, row, 4].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    orderWorksheet.Cells[row, 1, row, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    orderWorksheet.Cells[row, 1, row, 4].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    orderWorksheet.Cells[row, 1, row, 4].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    orderWorksheet.Cells[row, 1, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row++;
                }
                orderWorksheet.Cells[3, 1, row - 1, 4].AutoFitColumns();

                // Tạo sheet cho doanh thu
                var revenueWorksheet = package.Workbook.Worksheets.Add("Thống kê Doanh thu");
                revenueWorksheet.Cells[1, 1].Value = $"Thống kê Doanh thu Theo Tháng (Năm {year})";
                revenueWorksheet.Cells[1, 1].Style.Font.Size = 16;
                revenueWorksheet.Cells[1, 1].Style.Font.Bold = true;
                revenueWorksheet.Cells[1, 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
                revenueWorksheet.Cells[1, 1, 1, 4].Merge = true;
                revenueWorksheet.Cells[1, 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                revenueWorksheet.Cells[1, 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.DarkBlue);
                revenueWorksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                revenueWorksheet.Cells[3, 1].Value = "Thời gian";
                revenueWorksheet.Cells[3, 2].Value = "Doanh thu (Đã thanh toán)";
                revenueWorksheet.Cells[3, 3].Value = "Doanh thu (Chưa thanh toán)";
                revenueWorksheet.Cells[3, 4].Value = "Tổng doanh thu";
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Font.Bold = true;
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                revenueWorksheet.Cells[3, 1, 3, 4].Style.Border.Right.Style = ExcelBorderStyle.Thin;

                row = 4;
                foreach (var stat in revenueStatsQuery)
                {
                    revenueWorksheet.Cells[row, 1].Value = stat.MonthYear;
                    revenueWorksheet.Cells[row, 2].Value = stat.PaidRevenue;
                    revenueWorksheet.Cells[row, 2].Style.Numberformat.Format = "#,##0 VNĐ";
                    revenueWorksheet.Cells[row, 3].Value = stat.UnpaidRevenue;
                    revenueWorksheet.Cells[row, 3].Style.Numberformat.Format = "#,##0 VNĐ";
                    revenueWorksheet.Cells[row, 4].Value = stat.TotalRevenue;
                    revenueWorksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0 VNĐ";
                    revenueWorksheet.Cells[row, 1, row, 4].Style.Border.Top.Style = ExcelBorderStyle.Thin;
                    revenueWorksheet.Cells[row, 1, row, 4].Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                    revenueWorksheet.Cells[row, 1, row, 4].Style.Border.Left.Style = ExcelBorderStyle.Thin;
                    revenueWorksheet.Cells[row, 1, row, 4].Style.Border.Right.Style = ExcelBorderStyle.Thin;
                    revenueWorksheet.Cells[row, 1, row, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    row++;
                }
                revenueWorksheet.Cells[3, 1, row - 1, 4].AutoFitColumns();

                // Xuất file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                string fileName = $"ThongKe_Nam_{year}.xlsx";
                stream.Position = 0;

                // Đảm bảo trình duyệt hiển thị hộp thoại lưu file
                Response.Clear();
                Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                Response.AddHeader("Content-Disposition", "attachment; filename=" + fileName);
                Response.BinaryWrite(stream.ToArray());
                stream.Close();
                Response.End();

                return new EmptyResult(); // Để tránh xung đột với Response.End()
            }
        }
    }
}