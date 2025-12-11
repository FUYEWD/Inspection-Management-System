-- ============================================
-- 公共設施巡檢管理系統
-- MS SQL Server 資料庫 Schema
-- ============================================

-- 建立資料庫
CREATE DATABASE InspectionDB;
GO

USE InspectionDB;
GO

-- ============================================
-- 1. 使用者資料表 (Users)
-- ============================================
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY(1,1),
    Username NVARCHAR(50) NOT NULL UNIQUE,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FullName NVARCHAR(100) NOT NULL,
    [Role] NVARCHAR(20) NOT NULL CHECK ([Role] IN ('Admin', 'Inspector', 'Supervisor')),
    Department NVARCHAR(100),
    Phone NVARCHAR(20),
    [Status] NVARCHAR(20) NOT NULL CHECK ([Status] IN ('Active', 'Inactive', 'Suspended')),
    LastLogin DATETIME,
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CreatedBy INT,
    CONSTRAINT FK_Users_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- 索引
CREATE INDEX IX_Users_Email ON Users(Email);
CREATE INDEX IX_Users_Role ON Users([Role]);
CREATE INDEX IX_Users_Status ON Users([Status]);

-- ============================================
-- 2. 巡檢任務表 (Inspections)
-- ============================================
CREATE TABLE Inspections (
    InspectionId INT PRIMARY KEY IDENTITY(1,1),
    TaskCode NVARCHAR(50) NOT NULL UNIQUE,
    [Location] NVARCHAR(200) NOT NULL,
    LocationDescription NVARCHAR(MAX),
    AssignedTo INT NOT NULL,
    CreatedBy INT NOT NULL,
    [Status] NVARCHAR(20) NOT NULL CHECK ([Status] IN ('NotStarted', 'InProgress', 'Completed', 'Overdue', 'Cancelled')),
    DueDate DATETIME NOT NULL,
    CompletedDate DATETIME,
    Priority NVARCHAR(20) CHECK (Priority IN ('Low', 'Medium', 'High', 'Critical')),
    Description NVARCHAR(MAX),
    [Frequency] NVARCHAR(50),  -- 日、週、月、季、年
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Inspections_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES Users(UserId),
    CONSTRAINT FK_Inspections_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES Users(UserId)
);

-- 索引
CREATE INDEX IX_Inspections_Status ON Inspections([Status]);
CREATE INDEX IX_Inspections_AssignedTo ON Inspections(AssignedTo);
CREATE INDEX IX_Inspections_DueDate ON Inspections(DueDate);
CREATE INDEX IX_Inspections_TaskCode ON Inspections(TaskCode);

-- ============================================
-- 3. 異常回報表 (Reports)
-- ============================================
CREATE TABLE Reports (
    ReportId INT PRIMARY KEY IDENTITY(1,1),
    InspectionId INT NOT NULL,
    ReportedBy INT NOT NULL,
    IssueType NVARCHAR(100) NOT NULL,  -- 故障類型
    Severity NVARCHAR(20) NOT NULL CHECK (Severity IN ('Low', 'Medium', 'High', 'Critical')),
    [Status] NVARCHAR(20) NOT NULL CHECK ([Status] IN ('Pending', 'Assigned', 'InProgress', 'Resolved', 'Closed')),
    [Description] NVARCHAR(MAX) NOT NULL,
    GPSLatitude DECIMAL(10, 8),
    GPSLongitude DECIMAL(11, 8),
    AssignedTo INT,
    ResolvedBy INT,
    ResolvedDate DATETIME,
    ResolutionNotes NVARCHAR(MAX),
    WorkOrderNumber NVARCHAR(50),  -- 關聯工單編號
    ReportedAt DATETIME DEFAULT GETDATE(),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_Reports_Inspection FOREIGN KEY (InspectionId) REFERENCES Inspections(InspectionId),
    CONSTRAINT FK_Reports_ReportedBy FOREIGN KEY (ReportedBy) REFERENCES Users(UserId),
    CONSTRAINT FK_Reports_AssignedTo FOREIGN KEY (AssignedTo) REFERENCES Users(UserId),
    CONSTRAINT FK_Reports_ResolvedBy FOREIGN KEY (ResolvedBy) REFERENCES Users(UserId)
);

-- 索引
CREATE INDEX IX_Reports_InspectionId ON Reports(InspectionId);
CREATE INDEX IX_Reports_Status ON Reports([Status]);
CREATE INDEX IX_Reports_Severity ON Reports(Severity);
CREATE INDEX IX_Reports_ReportedBy ON Reports(ReportedBy);
CREATE INDEX IX_Reports_ReportedAt ON Reports(ReportedAt);

-- ============================================
-- 4. 附件表 (Attachments) - 照片與檔案
-- ============================================
CREATE TABLE Attachments (
    AttachmentId INT PRIMARY KEY IDENTITY(1,1),
    ReportId INT NOT NULL,
    FileName NVARCHAR(255) NOT NULL,
    FilePath NVARCHAR(MAX) NOT NULL,
    FileType NVARCHAR(50),  -- jpg, png, pdf, doc
    FileSize BIGINT,  -- 位元組
    UploadedBy INT NOT NULL,
    UploadedAt DATETIME DEFAULT GETDATE(),
    Description NVARCHAR(500),
    IsDeleted BIT DEFAULT 0,
    CONSTRAINT FK_Attachments_Report FOREIGN KEY (ReportId) REFERENCES Reports(ReportId) ON DELETE CASCADE,
    CONSTRAINT FK_Attachments_UploadedBy FOREIGN KEY (UploadedBy) REFERENCES Users(UserId)
);

-- 索引
CREATE INDEX IX_Attachments_ReportId ON Attachments(ReportId);
CREATE INDEX IX_Attachments_UploadedAt ON Attachments(UploadedAt);

-- ============================================
-- 5. 稽核日誌表 (AuditLogs)
-- ============================================
CREATE TABLE AuditLogs (
    AuditId INT PRIMARY KEY IDENTITY(1,1),
    UserId INT,
    [Action] NVARCHAR(100) NOT NULL,  -- Create, Update, Delete, Login, etc.
    TableName NVARCHAR(100),
    RecordId INT,
    OldValue NVARCHAR(MAX),
    NewValue NVARCHAR(MAX),
    [Timestamp] DATETIME DEFAULT GETDATE(),
    IPAddress NVARCHAR(50),
    CONSTRAINT FK_AuditLogs_User FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- 索引
CREATE INDEX IX_AuditLogs_UserId ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_Timestamp ON AuditLogs([Timestamp]);
CREATE INDEX IX_AuditLogs_Action ON AuditLogs([Action]);

-- ============================================
-- 6. 系統設定表 (SystemSettings)
-- ============================================
CREATE TABLE SystemSettings (
    SettingId INT PRIMARY KEY IDENTITY(1,1),
    SettingKey NVARCHAR(100) NOT NULL UNIQUE,
    SettingValue NVARCHAR(MAX),
    [Description] NVARCHAR(500),
    UpdatedAt DATETIME DEFAULT GETDATE(),
    UpdatedBy INT
);

-- ============================================
-- 7. 通知記錄表 (Notifications)
-- ============================================
CREATE TABLE Notifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    RecipientId INT NOT NULL,
    [Type] NVARCHAR(50),  -- TaskAssigned, TaskOverdue, ReportSubmitted
    Title NVARCHAR(200) NOT NULL,
    [Message] NVARCHAR(MAX),
    RelatedInspectionId INT,
    RelatedReportId INT,
    IsRead BIT DEFAULT 0,
    CreatedAt DATETIME DEFAULT GETDATE(),
    ReadAt DATETIME,
    CONSTRAINT FK_Notifications_Recipient FOREIGN KEY (RecipientId) REFERENCES Users(UserId),
    CONSTRAINT FK_Notifications_Inspection FOREIGN KEY (RelatedInspectionId) REFERENCES Inspections(InspectionId),
    CONSTRAINT FK_Notifications_Report FOREIGN KEY (RelatedReportId) REFERENCES Reports(ReportId)
);

-- 索引
CREATE INDEX IX_Notifications_RecipientId ON Notifications(RecipientId);
CREATE INDEX IX_Notifications_IsRead ON Notifications(IsRead);

-- ============================================
-- 初始化資料
-- ============================================

-- 系統設定
INSERT INTO SystemSettings (SettingKey, SettingValue, [Description])
VALUES 
    ('AppName', '公共設施巡檢管理系統', '應用程式名稱'),
    ('Version', '1.0.0', '系統版本'),
    ('MaxUploadSize', '10485760', '最大上傳檔案大小（位元組）'),
    ('OverdueWarningDays', '2', '超期警告提前天數'),
    ('SMTPHost', 'smtp.gmail.com', 'SMTP 伺服器'),
    ('SMTPPort', '587', 'SMTP 埠號');

-- 測試帳號（開發用）
INSERT INTO Users (Username, Email, PasswordHash, FullName, [Role], Department, Phone, [Status])
VALUES 
    ('admin', 'admin@test.com', '$2b$10$...', '系統管理員', 'Admin', '資訊部', '02-1234-5678', 'Active'),
    ('inspector1', 'inspector@test.com', '$2b$10$...', '巡檢員甲', 'Inspector', '北區巡檢隊', '0912-345-678', 'Active'),
    ('supervisor', 'supervisor@test.com', '$2b$10$...', '部門主管', 'Supervisor', '北區', '02-2345-6789', 'Active');

-- ============================================
-- 預存程序：統計今日巡檢完成率
-- ============================================
CREATE PROCEDURE sp_GetTodayCompletionRate
AS
BEGIN
    DECLARE @TodayStart DATETIME = CAST(GETDATE() AS DATE);
    DECLARE @TodayEnd DATETIME = DATEADD(DAY, 1, @TodayStart);
    
    SELECT 
        COUNT(*) AS TotalInspections,
        SUM(CASE WHEN [Status] = 'Completed' THEN 1 ELSE 0 END) AS CompletedCount,
        CAST(SUM(CASE WHEN [Status] = 'Completed' THEN 1 ELSE 0 END) AS FLOAT) / 
        NULLIF(COUNT(*), 0) * 100 AS CompletionRate
    FROM Inspections
    WHERE CreatedAt >= @TodayStart AND CreatedAt < @TodayEnd;
END;
GO

-- ============================================
-- 預存程序：取得異常統計（按類型）
-- ============================================
CREATE PROCEDURE sp_GetIssueStatistics
    @StartDate DATETIME,
    @EndDate DATETIME
AS
BEGIN
    SELECT 
        IssueType,
        COUNT(*) AS Count,
        SUM(CASE WHEN Severity = 'Critical' THEN 1 ELSE 0 END) AS CriticalCount,
        SUM(CASE WHEN Severity = 'High' THEN 1 ELSE 0 END) AS HighCount
    FROM Reports
    WHERE ReportedAt >= @StartDate AND ReportedAt <= @EndDate
    GROUP BY IssueType
    ORDER BY Count DESC;
END;
GO

-- ============================================
-- 預存程序：取得超期任務
-- ============================================
CREATE PROCEDURE sp_GetOverdueTasks
AS
BEGIN
    SELECT 
        InspectionId,
        TaskCode,
        [Location],
        AssignedTo,
        DueDate,
        DATEDIFF(DAY, DueDate, GETDATE()) AS DaysOverdue
    FROM Inspections
    WHERE [Status] IN ('NotStarted', 'InProgress')
    AND DueDate < GETDATE()
    ORDER BY DaysOverdue DESC;
END;
GO

-- ============================================
-- 建立檢視 (Views)
-- ============================================

-- 檢視：巡檢任務詳細資訊
CREATE VIEW vw_InspectionDetails AS
SELECT 
    i.InspectionId,
    i.TaskCode,
    i.[Location],
    u.FullName AS AssignedToName,
    i.[Status],
    i.DueDate,
    DATEDIFF(DAY, GETDATE(), i.DueDate) AS DaysRemaining,
    COUNT(DISTINCT r.ReportId) AS ReportCount,
    i.CreatedAt
FROM Inspections i
LEFT JOIN Users u ON i.AssignedTo = u.UserId
LEFT JOIN Reports r ON i.InspectionId = r.InspectionId
GROUP BY 
    i.InspectionId, i.TaskCode, i.[Location], u.FullName, i.[Status], 
    i.DueDate, i.CreatedAt;
GO

-- 檢視：人員績效統計
CREATE VIEW vw_InspectorPerformance AS
SELECT 
    u.UserId,
    u.FullName,
    COUNT(i.InspectionId) AS TotalTasks,
    SUM(CASE WHEN i.[Status] = 'Completed' THEN 1 ELSE 0 END) AS CompletedTasks,
    SUM(CASE WHEN i.[Status] = 'Overdue' THEN 1 ELSE 0 END) AS OverdueTasks,
    CAST(SUM(CASE WHEN i.[Status] = 'Completed' THEN 1 ELSE 0 END) AS FLOAT) / 
    NULLIF(COUNT(i.InspectionId), 0) * 100 AS CompletionRate
FROM Users u
LEFT JOIN Inspections i ON u.UserId = i.AssignedTo
WHERE u.[Role] = 'Inspector'
GROUP BY u.UserId, u.FullName;
GO

-- ============================================
-- 建立觸發器：自動更新 UpdatedAt
-- ============================================
CREATE TRIGGER tr_UpdateTimestamp_Inspections
ON Inspections
AFTER UPDATE
AS
BEGIN
    UPDATE Inspections
    SET UpdatedAt = GETDATE()
    WHERE InspectionId IN (SELECT InspectionId FROM inserted);
END;
GO

CREATE TRIGGER tr_UpdateTimestamp_Reports
ON Reports
AFTER UPDATE
AS
BEGIN
    UPDATE Reports
    SET UpdatedAt = GETDATE()
    WHERE ReportId IN (SELECT ReportId FROM inserted);
END;
GO

-- ============================================
-- 完成！
-- ============================================
PRINT '資料庫初始化完成！';
PRINT '已建立資料表數量: 8';
PRINT '已建立預存程序數量: 3';
PRINT '已建立檢視數量: 2';