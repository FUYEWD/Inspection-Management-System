// ============================================
// 公共設施巡檢管理系統 - API Controller
// ASP.NET Core 6.0
// ============================================

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InspectionAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InspectionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IInspectionService _inspectionService;
        private readonly ILogger<InspectionsController> _logger;

        public InspectionsController(
            ApplicationDbContext context,
            IInspectionService inspectionService,
            ILogger<InspectionsController> logger)
        {
            _context = context;
            _inspectionService = inspectionService;
            _logger = logger;
        }

        /// <summary>
        /// 取得所有巡檢任務
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<InspectionDto>>> GetInspections(
            [FromQuery] string? status = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Inspections.AsQueryable();

                // 篩選狀態
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(i => i.Status == status);
                }

                // 分頁
                var inspections = await query
                    .OrderByDescending(i => i.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(i => i.AssignedToUser)
                    .Select(i => new InspectionDto
                    {
                        InspectionId = i.InspectionId,
                        TaskCode = i.TaskCode,
                        Location = i.Location,
                        AssignedToName = i.AssignedToUser.FullName,
                        Status = i.Status,
                        DueDate = i.DueDate,
                        CreatedAt = i.CreatedAt,
                        DaysRemaining = EF.Functions.DateDiffDay(DateTime.Now, i.DueDate)
                    })
                    .ToListAsync();

                return Ok(inspections);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得巡檢任務時發生錯誤");
                return StatusCode(500, new { message = "取得資料時發生錯誤" });
            }
        }

        /// <summary>
        /// 取得單筆巡檢任務詳情
        /// </summary>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<InspectionDetailDto>> GetInspection(int id)
        {
            try
            {
                var inspection = await _context.Inspections
                    .Include(i => i.AssignedToUser)
                    .Include(i => i.CreatedByUser)
                    .Include(i => i.Reports)
                    .ThenInclude(r => r.Attachments)
                    .FirstOrDefaultAsync(i => i.InspectionId == id);

                if (inspection == null)
                {
                    return NotFound(new { message = "巡檢任務不存在" });
                }

                var dto = new InspectionDetailDto
                {
                    InspectionId = inspection.InspectionId,
                    TaskCode = inspection.TaskCode,
                    Location = inspection.Location,
                    LocationDescription = inspection.LocationDescription,
                    AssignedToName = inspection.AssignedToUser?.FullName,
                    CreatedByName = inspection.CreatedByUser?.FullName,
                    Status = inspection.Status,
                    DueDate = inspection.DueDate,
                    CompletedDate = inspection.CompletedDate,
                    Priority = inspection.Priority,
                    Description = inspection.Description,
                    ReportCount = inspection.Reports.Count,
                    CreatedAt = inspection.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得巡檢任務詳情時發生錯誤");
                return StatusCode(500, new { message = "取得資料時發生錯誤" });
            }
        }

        /// <summary>
        /// 新增巡檢任務
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<InspectionDto>> CreateInspection(CreateInspectionRequest request)
        {
            try
            {
                // 驗證輸入
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // 檢查負責人是否存在
                var assignedUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == request.AssignedToUserId);

                if (assignedUser == null)
                {
                    return BadRequest(new { message = "指定的負責人不存在" });
                }

                // 取得當前使用者ID
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // 建立新任務
                var inspection = new Inspection
                {
                    TaskCode = $"TASK-{DateTime.Now:yyyyMMddHHmmss}",
                    Location = request.Location,
                    LocationDescription = request.LocationDescription,
                    AssignedTo = request.AssignedToUserId,
                    CreatedBy = userId,
                    Status = "NotStarted",
                    DueDate = request.DueDate,
                    Priority = request.Priority ?? "Medium",
                    Description = request.Description,
                    Frequency = request.Frequency,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Inspections.Add(inspection);
                await _context.SaveChangesAsync();

                // 記錄稽核日誌
                await _inspectionService.LogAuditAsync(
                    userId,
                    "Create",
                    "Inspections",
                    inspection.InspectionId,
                    null,
                    inspection.TaskCode);

                // 發送通知
                await _inspectionService.SendTaskNotificationAsync(
                    inspection.AssignedTo,
                    $"新的巡檢任務派工：{inspection.TaskCode}",
                    inspection.InspectionId);

                var dto = new InspectionDto
                {
                    InspectionId = inspection.InspectionId,
                    TaskCode = inspection.TaskCode,
                    Location = inspection.Location,
                    Status = inspection.Status,
                    DueDate = inspection.DueDate,
                    CreatedAt = inspection.CreatedAt
                };

                return CreatedAtAction(nameof(GetInspection), new { id = inspection.InspectionId }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立巡檢任務時發生錯誤");
                return StatusCode(500, new { message = "建立任務失敗" });
            }
        }

        /// <summary>
        /// 更新巡檢任務
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Supervisor")]
        public async Task<IActionResult> UpdateInspection(int id, UpdateInspectionRequest request)
        {
            try
            {
                var inspection = await _context.Inspections.FindAsync(id);

                if (inspection == null)
                {
                    return NotFound(new { message = "巡檢任務不存在" });
                }

                // 更新欄位
                inspection.Location = request.Location ?? inspection.Location;
                inspection.Status = request.Status ?? inspection.Status;
                inspection.Priority = request.Priority ?? inspection.Priority;
                inspection.Description = request.Description ?? inspection.Description;
                inspection.DueDate = request.DueDate ?? inspection.DueDate;
                inspection.UpdatedAt = DateTime.Now;

                // 如果狀態更新為完成，記錄完成時間
                if (request.Status == "Completed" && inspection.Status != "Completed")
                {
                    inspection.CompletedDate = DateTime.Now;
                }

                _context.Inspections.Update(inspection);
                await _context.SaveChangesAsync();

                // 記錄稽核日誌
                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");
                await _inspectionService.LogAuditAsync(
                    userId,
                    "Update",
                    "Inspections",
                    inspection.InspectionId,
                    null,
                    inspection.TaskCode);

                return Ok(new { message = "巡檢任務已更新" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新巡檢任務時發生錯誤");
                return StatusCode(500, new { message = "更新任務失敗" });
            }
        }

        /// <summary>
        /// 刪除巡檢任務
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInspection(int id)
        {
            try
            {
                var inspection = await _context.Inspections
                    .Include(i => i.Reports)
                    .FirstOrDefaultAsync(i => i.InspectionId == id);

                if (inspection == null)
                {
                    return NotFound(new { message = "巡檢任務不存在" });
                }

                // 檢查是否有相關異常回報
                if (inspection.Reports.Any())
                {
                    return BadRequest(new { message = "無法刪除：任務存在相關異常回報" });
                }

                _context.Inspections.Remove(inspection);
                await _context.SaveChangesAsync();

                return Ok(new { message = "巡檢任務已刪除" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刪除巡檢任務時發生錯誤");
                return StatusCode(500, new { message = "刪除任務失敗" });
            }
        }
    }

    // ============================================
    // 異常回報 Controller
    // ============================================
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            IReportService reportService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _reportService = reportService;
            _logger = logger;
        }

        /// <summary>
        /// 取得所有異常回報
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ReportDto>>> GetReports(
            [FromQuery] string? status = null,
            [FromQuery] string? severity = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Reports.AsQueryable();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(r => r.Status == status);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(r => r.Severity == severity);

                var reports = await query
                    .OrderByDescending(r => r.ReportedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Include(r => r.ReportedByUser)
                    .Include(r => r.Attachments)
                    .Select(r => new ReportDto
                    {
                        ReportId = r.ReportId,
                        InspectionId = r.InspectionId,
                        ReportedByName = r.ReportedByUser.FullName,
                        IssueType = r.IssueType,
                        Severity = r.Severity,
                        Status = r.Status,
                        Description = r.Description,
                        AttachmentCount = r.Attachments.Count,
                        ReportedAt = r.ReportedAt
                    })
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得異常回報時發生錯誤");
                return StatusCode(500, new { message = "取得資料時發生錯誤" });
            }
        }

        /// <summary>
        /// 新增異常回報
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Inspector")]
        public async Task<ActionResult<ReportDto>> CreateReport(CreateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // 驗證巡檢任務是否存在
                var inspection = await _context.Inspections
                    .FindAsync(request.InspectionId);

                if (inspection == null)
                    return BadRequest(new { message = "巡檢任務不存在" });

                var report = new Report
                {
                    InspectionId = request.InspectionId,
                    ReportedBy = userId,
                    IssueType = request.IssueType,
                    Severity = request.Severity ?? "Medium",
                    Status = "Pending",
                    Description = request.Description,
                    GPSLatitude = request.GPSLatitude,
                    GPSLongitude = request.GPSLongitude,
                    WorkOrderNumber = request.WorkOrderNumber,
                    ReportedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // 通知主管
                await _reportService.NotifySupervisorAsync(
                    $"新的異常回報：{report.IssueType}（{report.Severity}）",
                    report.ReportId);

                var dto = new ReportDto
                {
                    ReportId = report.ReportId,
                    InspectionId = report.InspectionId,
                    IssueType = report.IssueType,
                    Severity = report.Severity,
                    Status = report.Status,
                    Description = report.Description,
                    ReportedAt = report.ReportedAt
                };

                return CreatedAtAction(nameof(GetReports), new { id = report.ReportId }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "建立異常回報時發生錯誤");
                return StatusCode(500, new { message = "建立回報失敗" });
            }
        }

        /// <summary>
        /// 上傳附件（照片）
        /// </summary>
        [HttpPost("{reportId}/attachments")]
        [Authorize]
        public async Task<IActionResult> UploadAttachment(int reportId, IFormFile file)
        {
            try
            {
                // 驗證檔案
                if (file == null || file.Length == 0)
                    return BadRequest(new { message = "檔案不能為空" });

                const long maxFileSize = 10 * 1024 * 1024; // 10 MB
                if (file.Length > maxFileSize)
                    return BadRequest(new { message = "檔案大小超過限制（最大 10MB）" });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf", ".doc", ".docx" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest(new { message = "不支援的檔案格式" });

                // 驗證異常回報是否存在
                var report = await _context.Reports.FindAsync(reportId);
                if (report == null)
                    return NotFound(new { message = "異常回報不存在" });

                var userId = int.Parse(User.FindFirst("UserId")?.Value ?? "0");

                // 儲存檔案
                var fileName = $"{reportId}_{DateTime.Now:yyyyMMddHHmmss}{fileExtension}";
                var filePath = Path.Combine("uploads", fileName);

                // 確保目錄存在
                Directory.CreateDirectory("uploads");

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // 記錄附件
                var attachment = new Attachment
                {
                    ReportId = reportId,
                    FileName = file.FileName,
                    FilePath = filePath,
                    FileType = fileExtension,
                    FileSize = file.Length,
                    UploadedBy = userId,
                    UploadedAt = DateTime.Now
                };

                _context.Attachments.Add(attachment);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "檔案已上傳",
                    attachmentId = attachment.AttachmentId,
                    fileName = attachment.FileName,
                    fileSize = attachment.FileSize
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "上傳附件時發生錯誤");
                return StatusCode(500, new { message = "上傳失敗" });
            }
        }
    }

    // ============================================
    // Dashboard Controller
    // ============================================
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 取得儀表板摘要資料
        /// </summary>
        [HttpGet("summary")]
        [Authorize]
        public async Task<ActionResult<DashboardSummaryDto>> GetSummary()
        {
            try
            {
                var today = DateTime.Today;
                var todayStart = today;
                var todayEnd = today.AddDays(1);

                // 今日統計
                var todayTasks = await _context.Inspections
                    .Where(i => i.CreatedAt >= todayStart && i.CreatedAt < todayEnd)
                    .ToListAsync();

                var completedToday = todayTasks.Count(i => i.Status == "Completed");
                var completionRate = todayTasks.Count > 0 
                    ? (completedToday * 100.0 / todayTasks.Count) 
                    : 0;

                // 異常統計
                var totalReports = await _context.Reports.CountAsync();
                var criticalReports = await _context.Reports
                    .Where(r => r.Severity == "Critical" && r.Status != "Closed")
                    .CountAsync();

                // 超期任務
                var overdueTasks = await _context.Inspections
                    .Where(i => i.Status != "Completed" && i.DueDate < DateTime.Now)
                    .CountAsync();

                var summary = new DashboardSummaryDto
                {
                    TodayTaskCount = todayTasks.Count,
                    CompletedTaskCount = completedToday,
                    CompletionRate = Math.Round(completionRate, 2),
                    TotalReports = totalReports,
                    CriticalReportsCount = criticalReports,
                    OverdueTaskCount = overdueTasks,
                    LastUpdated = DateTime.Now
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得儀表板資料時發生錯誤");
                return StatusCode(500, new { message = "取得資料失敗" });
            }
        }

        /// <summary>
        /// 取得圖表資料（異常類型分佈）
        /// </summary>
        [HttpGet("chart-issues")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<ChartDataDto>>> GetIssueChart(
            [FromQuery] int days = 30)
        {
            try
            {
                var startDate = DateTime.Now.AddDays(-days);

                var data = await _context.Reports
                    .Where(r => r.ReportedAt >= startDate)
                    .GroupBy(r => r.IssueType)
                    .Select(g => new ChartDataDto
                    {
                        Label = g.Key,
                        Value = g.Count()
                    })
                    .OrderByDescending(x => x.Value)
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取得圖表資料時發生錯誤");
                return StatusCode(500, new { message = "取得資料失敗" });
            }
        }
    }

    // ============================================
    // DTO 模型
    // ============================================

    public class InspectionDto
    {
        public int InspectionId { get; set; }
        public string TaskCode { get; set; }
        public string Location { get; set; }
        public string AssignedToName { get; set; }
        public string Status { get; set; }
        public DateTime DueDate { get; set; }
        public int? DaysRemaining { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class InspectionDetailDto
    {
        public int InspectionId { get; set; }
        public string TaskCode { get; set; }
        public string Location { get; set; }
        public string LocationDescription { get; set; }
        public string AssignedToName { get; set; }
        public string CreatedByName { get; set; }
        public string Status { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public int ReportCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateInspectionRequest
    {
        [Required(ErrorMessage = "地點必填")]
        public string Location { get; set; }
        public string LocationDescription { get; set; }
        [Required(ErrorMessage = "負責人必填")]
        public int AssignedToUserId { get; set; }
        [Required(ErrorMessage = "截止日期必填")]
        public DateTime DueDate { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public string Frequency { get; set; }
    }

    public class UpdateInspectionRequest
    {
        public string Location { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string Description { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class ReportDto
    {
        public int ReportId { get; set; }
        public int InspectionId { get; set; }
        public string ReportedByName { get; set; }
        public string IssueType { get; set; }
        public string Severity { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public int AttachmentCount { get; set; }
        public DateTime ReportedAt { get; set; }
    }

    public class CreateReportRequest
    {
        [Required(ErrorMessage = "巡檢任務必填")]
        public int InspectionId { get; set; }
        [Required(ErrorMessage = "故障類型必填")]
        public string IssueType { get; set; }
        public string Severity { get; set; }
        [Required(ErrorMessage = "異常說明必填")]
        public string Description { get; set; }
        public decimal? GPSLatitude { get; set; }
        public decimal? GPSLongitude { get; set; }
        public string WorkOrderNumber { get; set; }
    }

    public class DashboardSummaryDto
    {
        public int TodayTaskCount { get; set; }
        public int CompletedTaskCount { get; set; }
        public double CompletionRate { get; set; }
        public int TotalReports { get; set; }
        public int CriticalReportsCount { get; set; }
        public int OverdueTaskCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class ChartDataDto
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }
}