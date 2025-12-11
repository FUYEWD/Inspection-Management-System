# Inspection-Management-System
Inspection Management System
公共設施巡檢管理系統
Inspection Management System
一個為政府機構設計的完整巡檢、異常回報與資料管理平台
Show Image
Show Image
Show Image

📋 專案概述
本系統針對政府公用事業單位（台電、自來水公司、工務局等）設計，提供：

巡檢排程管理 - 任務派工、進度追蹤、自動通知
現場異常回報 - 包含照片上傳、GPS 定位、故障分類
權限管理 - 管理員、巡檢人員、主管三層級權限
資料分析儀表板 - 完成率、異常統計、月度報表
Excel/PDF 報表匯出 - 符合政府公文格式


🛠️ 技術棧
層級技術版本後端ASP.NET Core6.0+資料庫MS SQL Server2019+前端Bootstrap 5 + jQuery3.6.0APIRESTful + SwaggerOpenAPI 3.0認證JWT TokenRS256版本控管GitGitHubORMEntity Framework Core6.0+

📁 專案結構
inspection-management-system/
├── src/
│   ├── InspectionAPI/
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs
│   │   │   ├── InspectionsController.cs
│   │   │   ├── ReportsController.cs
│   │   │   └── DashboardController.cs
│   │   ├── Models/
│   │   │   ├── User.cs
│   │   │   ├── Inspection.cs
│   │   │   ├── Report.cs
│   │   │   └── Attachment.cs
│   │   ├── Services/
│   │   │   ├── InspectionService.cs
│   │   │   ├── ReportService.cs
│   │   │   └── EmailService.cs
│   │   ├── Data/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   └── Migrations/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   ├── InspectionWeb/
│   │   ├── wwwroot/
│   │   │   ├── css/
│   │   │   ├── js/
│   │   │   ├── images/
│   │   │   └── uploads/
│   │   ├── views/
│   │   │   ├── login.html
│   │   │   ├── dashboard.html
│   │   │   ├── inspections.html
│   │   │   ├── reports.html
│   │   │   └── settings.html
│   │   └── index.html
├── database/
│   ├── schema.sql
│   ├── seed-data.sql
│   └── migrations/
├── docs/
│   ├── API-Documentation.md
│   ├── Database-ERD.md
│   ├── System-Architecture.md
│   └── Deployment-Guide.md
├── .gitignore
├── README.md
└── docker-compose.yml

🚀 快速開始
前置需求

.NET 6.0 SDK 或更新版本
MS SQL Server 2019 或更新版本
Visual Studio 2022 或 VS Code
Git

安裝步驟
1. 複製專案
bashgit clone https://github.com/yourusername/inspection-management-system.git
cd inspection-management-system
2. 設定資料庫連線
編輯 appsettings.json：
json{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InspectionDB;User Id=sa;Password=YourPassword;"
  }
}
3. 建立資料庫
bashcd src/InspectionAPI
dotnet ef database update
4. 啟動後端 API
bashdotnet run
API 將在 https://localhost:5001 運行
5. 啟動前端
在瀏覽器開啟 src/InspectionWeb/index.html 或使用簡單 HTTP 伺服器：
bashcd src/InspectionWeb
python -m http.server 8000
訪問 http://localhost:8000

📊 核心功能
1️⃣ 使用者管理

管理員 - 建立任務、派工、權限管理、報表匯出
巡檢人員 - 接收任務、回報異常、上傳照片
部門主管 - 查看下屬進度、生成月度報告

2️⃣ 巡檢排程
新增任務 → 派工通知 → 人員接收 → 進行巡檢 → 結果回報 → 管理確認 → 歸檔

自動計算巡檢路線（可選）
截止日期提醒
超期警告

3️⃣ 異常回報

現場拍照上傳（支援多張）
GPS 座標記錄
故障類型分類
緊急程度標記
關聯的工單編號

4️⃣ 資料分析儀表板

當日完成率
異常分布圖表
人員績效排名
超期任務提醒
月度統計報表

5️⃣ 報表匯出

Excel - 完整巡檢記錄、異常統計
PDF - 月度報告、正式公文格式


🔐 API 端點示例
認證
POST   /api/auth/login              - 登入
POST   /api/auth/logout             - 登出
POST   /api/auth/refresh            - 刷新 Token
巡檢管理
GET    /api/inspections             - 取得所有巡檢任務
GET    /api/inspections/{id}        - 取得單筆任務詳情
POST   /api/inspections             - 新增巡檢任務
PUT    /api/inspections/{id}        - 更新巡檢任務
DELETE /api/inspections/{id}        - 刪除巡檢任務
異常回報
GET    /api/reports                 - 取得所有異常回報
POST   /api/reports                 - 新增異常回報
POST   /api/reports/{id}/attach     - 上傳附件（照片）
PUT    /api/reports/{id}            - 更新回報內容
儀表板
GET    /api/dashboard/summary       - 取得首頁統計數據
GET    /api/dashboard/chart-data    - 取得圖表資料
GET    /api/dashboard/export-excel  - 匯出 Excel
GET    /api/dashboard/export-pdf    - 匯出 PDF
完整 Swagger API 文檔：https://localhost:5001/swagger

💾 資料庫設計
核心資料表
Users - 使用者帳號
sqlUserId (PK) | Username | Email | PasswordHash | Role | Status | CreatedAt
Inspections - 巡檢任務
sqlInspectionId (PK) | TaskCode | Location | AssignedTo | Status | DueDate | CreatedAt
Reports - 異常回報
sqlReportId (PK) | InspectionId (FK) | ReportedBy | IssueType | Severity | Description | GPSLat | GPSLng | ReportedAt
Attachments - 照片/檔案
sqlAttachmentId (PK) | ReportId (FK) | FilePath | FileType | UploadedAt

🔧 配置說明
環境變數 .env
DB_SERVER=localhost
DB_NAME=InspectionDB
DB_USER=sa
DB_PASSWORD=YourPassword
JWT_SECRET=your-secret-key-here
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=your-email@gmail.com
SMTP_PASS=your-app-password
郵件通知設定
系統會在以下情況自動寄送通知：

任務派工時 → 寄通知給巡檢人員
任務逾期時 → 寄提醒給管理員
異常上報時 → 寄緊急通知給主管


📈 效能指標

API 回應時間 < 200ms（99% 案例）
資料庫連線池 最大 100 連線
同時支援使用者 500+
照片上傳限制 單張 10MB，單次最多 5 張
月度報表生成 < 30 秒（10,000 筆資料）


🧪 測試
單元測試
bashdotnet test
測試帳號（開發用）
角色帳號密碼管理員admin@test.comAdmin@123巡檢員inspector@test.comInspector@123主管supervisor@test.comSupervisor@123

📚 文檔
詳細文檔請見 docs/ 資料夾：

API-Documentation.md - 完整 API 參考
Database-ERD.md - 資料庫 ER 圖
System-Architecture.md - 系統架構與設計決策
Deployment-Guide.md - 部署到生產環境步驟


🐳 Docker 部署
bashdocker-compose up -d
將啟動：

API 服務 - http://localhost:5001
Web 前端 - http://localhost:3000
MS SQL Server - localhost:1433


🤝 貢獻指南

Fork 此專案
建立功能分支 (git checkout -b feature/amazing-feature)
Commit 更改 (git commit -m 'Add some amazing feature')
Push 到分支 (git push origin feature/amazing-feature)
開啟 Pull Request


📄 授權
MIT License - 見 LICENSE 檔案

👨‍💼 專案負責人
Your Name

GitHub: @yourusername
Email: your.email@example.com
LinkedIn: Your Profile


⭐ 致謝
感謝以下技術社群與開源專案的支持：

ASP.NET Core 團隊
Entity Framework Core
Bootstrap 框架
台灣開發者社群


📞 聯繫與支援
有任何問題或建議？

📧 Email: your.email@example.com
💬 GitHub Issues: 提交 Issue
📖 Wiki: 專案 Wiki
