# Danh sách công việc (Task List) - Tích hợp hỗn hợp FontAwesome & SimpleIcons

- [x] Bước 1: Khôi phục [Program.cs](file:///f:/Project%20Code/Mang/ui_avalonia/Program.cs) để khởi chạy ClassicDesktopLifetime bình thường.
- [x] Bước 2: Bổ sung Style của SimpleIcons vào [App.axaml](file:///f:/Project%20Code/Mang/ui_avalonia/App.axaml).
- [x] Bước 3: Cập nhật cấu trúc DomainItem trong [DnsBenchmarkViewModel.cs](file:///f:/Project%20Code/Mang/ui_avalonia/ViewModels/DnsBenchmarkViewModel.cs) hỗ trợ `IconSource` và `SimpleIconKind`.
- [x] Bước 4: Cập nhật 30 domains với thông tin logo chuẩn của SimpleIcons & FontAwesome trong [DnsBenchmarkService.cs](file:///f:/Project%20Code/Mang/ui_avalonia/Services/DnsBenchmarkService.cs).
- [x] Bước 5: Cập nhật XAML trong [DnsBenchmarkView.axaml](file:///f:/Project%20Code/Mang/ui_avalonia/Views/DnsBenchmarkView.axaml) hiển thị có điều kiện logo theo nguồn.
- [x] Bước 6: Biên dịch (`dotnet build`) và chạy kiểm thử ứng dụng. (Biên dịch thành công với 0 lỗi)
