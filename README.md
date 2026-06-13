# 🌐 LuminaLink - Auto IP & DNS Manager

[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-blue.svg)](#)
[![Framework](https://img.shields.io/badge/framework-.NET%209.0%20%7C%20Avalonia%20UI-violet.svg)](#)
[![Python](https://img.shields.io/badge/python-3.10%2B-green.svg)](#)
[![License](https://img.shields.io/badge/license-MIT-orange.svg)](#)

*Choose your language / Chọn ngôn ngữ hiển thị:*
- [English Description](#english-version)
- [Mô tả Tiếng Việt](#tiếng-việt)

---

## English Version

### 📝 Project Overview
**LuminaLink** is a modern, cross-platform network management suite designed for power users, network administrators, and developers. It provides a visual UI and CLI tool to easily change IP configurations (Static & DHCP), benchmark DNS performance using raw UDP queries, run real-time internet speed tests, manage profiles, schedule automatic IP changes, and locate geographical IP details.

The project features two versions:
1. **Desktop GUI App (C# / .NET 9 + Avalonia UI)**: A highly-optimized, premium, cross-platform MVVM desktop client with custom-rendered controls and localization support.
2. **CLI & Dear PyGui App (Python)**: A lightweight, command-line and simple GUI alternative.

---

### ✨ Key Features
- 🔄 **Quick IP Changing & DHCP Reset**: Configure Static IP (IP, Subnet Mask, Gateway) or instantly restore DHCP settings across Windows, Linux, and macOS.
- 📁 **Profile Management**: Save, delete, import, and export network profiles (JSON format) to switch between home, office, or lab environments in just two clicks.
- ⚡ **Real-time Speedtest**: Powered by Ookla Speedtest CLI. Automatically fetches the nearest test servers via HTTP API cache and displays real-time download/upload speed, latency (ping), jitter, packet loss, and results URL.
- 🎯 **Advanced DNS Benchmark**: Perform multi-round DNS speed tests (via raw UDP sockets) on 10+ major public DNS servers (Google, Cloudflare, Quad9, AdGuard, NextDNS, etc.) against customized website categories:
  - *Standard Web*: Google, Youtube, Facebook, Wikipedia, Netflix...
  - *AI Clouds*: ChatGPT, Gemini, Claude, Hugging Face, Perplexity...
  - *Cloud Services*: AWS, Azure, Google Cloud, GitHub, Vercel...
  - Ranks and rates servers (*Excellent, Good, Average, Poor*) dynamically based on response speed.
- ⏱️ **Auto IP Scheduler**: Automate IP rotation or configuration switching at specific intervals.
- 📍 **Real-time GeoIP Location**: Inspect public IP details (Country, City, Region, ISP, GPS coordinates, and Timezone) with an automated HTTP API fallback mechanism (`ipinfo.io` & `ip-api.com`).
- 📊 **Connectivity Tests & History Logging**: Quick pings to Gateway, Internet (8.8.8.8), and DNS Resolution with comprehensive action logging.
- 🎨 **Modern UI & Visual Design**:
  - **SpeedGaugeControl**: A custom-drawn circular speedometer (270° arc) with logarithmic scaling, smooth rendering (60 FPS pre-computed brushes), and glowing gradient arcs.
  - Multi-language support (English & Vietnamese).
  - Adaptive Light, Dark, and System Default themes.

---

### 🛠️ Technical Stack
* **Desktop Application (.NET Core)**:
  - Framework: **Avalonia UI** (Cross-platform UI framework)
  - Pattern: **MVVM**
  - Custom Rendering: Draw-on-demand 270° logarithmic gauge control
  - Native Integration: Process-level OS commands wrapper (`netsh`, `ip addr/route`, `networksetup`)
  - Icons: **FluentIcons** & **SimpleIcons**
* **Scripting / CLI (Python)**:
  - GUI Library: **Dear PyGui**
  - CLI Library: **Rich** & **Colorama** for styling and menus

---

### 🚀 Getting Started

#### Prerequisites
- **For C# Desktop App**: .NET 9 SDK installed.
- **For Python App**: Python 3.10+ installed.
- **System Permissions**: Running the app requires **Administrator** (Windows) or **Root** (Linux/macOS) permissions since changing network configurations is a protected OS action.

---

#### 1. Running the Avalonia C# App (.NET 9)
```bash
# Navigate to the Avalonia project directory
cd ui_avalonia

# Restore dependencies
dotnet restore

# Build & Run the project
dotnet run
```
*Note: Run the terminal or IDE as Administrator/sudo to execute IP and DNS changes successfully.*

---

#### 2. Running the Python CLI / GUI App
```bash
# Install required libraries
pip install -r requirements.txt

# Run GUI version (Dear PyGui)
python gui.py   # Windows (Run as Admin)
sudo python gui.py # Linux/macOS

# Run CLI version (Rich Terminal)
python main.py  # Windows (Run as Admin)
sudo python main.py # Linux/macOS
```

---

### 📂 Directory Structure
```
├── ui_avalonia/               # C# Avalonia UI Project
│   ├── App.axaml              # App Entry point & theme styles
│   ├── Controls/              # Custom SpeedGaugeControl UI code
│   ├── Models/                # NetworkCard, DnsRecord, IPProfile
│   ├── Services/              # Speedtest, DnsBenchmark, GeoIp, Network Services
│   ├── ViewModels/            # MVVM ViewModels logic
│   ├── Views/                 # AXAML Views (Dashboard, Profiles, DNS...)
│   └── ui_avalonia.csproj     # Project config
├── core/                      # Python Core Logic (IP Changing, Validator, DNS)
├── ui/                        # Python GUI & CLI interface layouts
├── scheduler/                 # Scheduler engine for Python version
├── utils/                     # Helper modules (GeoIP, speedtest, loggers)
├── main.py                    # Entry point CLI
├── gui.py                     # Entry point GUI (PyGui)
└── DNS.txt                    # Config file containing DNS server lists
```

---

## Tiếng Việt

### 📝 Tổng Quan Dự Án
**LuminaLink** là một bộ công cụ quản lý cấu hình mạng đa nền tảng hiện đại, được thiết kế cho người dùng nâng cao, quản trị viên mạng và lập trình viên. Ứng dụng hỗ trợ thay đổi địa chỉ IP (Static & DHCP), kiểm thử và so sánh tốc độ DNS (Benchmark) qua UDP Socket trực tiếp, đo tốc độ internet thời gian thực (Speedtest), lên lịch đổi IP tự động và tra cứu vị trí địa lý của IP Public.

Dự án bao gồm 2 phiên bản song song:
1. **Phiên bản Desktop GUI (C# / .NET 9 + Avalonia UI)**: Giao diện trực quan cao cấp, hoạt động mượt mà trên nhiều hệ điều hành, viết theo mô hình MVVM với các thành phần giao diện vẽ tay tùy biến.
2. **Phiên bản CLI & Dear PyGui (Python)**: Giao diện dòng lệnh (CLI) đẹp mắt kết hợp giao diện đồ họa siêu nhẹ.

---

### ✨ Các Tính Năng Nổi Bật
- 🔄 **Thay đổi IP & Khôi phục DHCP nhanh chóng**: Cấu hình IP Tĩnh (IP, Subnet Mask, Gateway) hoặc khôi phục nhận IP Tự động (DHCP) trên Windows, Linux và macOS.
- 📁 **Quản lý Profile Cấu Hình**: Lưu các thông số mạng hiện tại thành Profile dưới dạng tệp tin JSON, xuất/nhập tệp dễ dàng để chuyển đổi cấu hình mạng (nhà riêng, công ty, phòng lab) chỉ với 2 click chuột.
- ⚡ **Đo tốc độ mạng (Speedtest) thời gian thực**: Tích hợp Ookla Speedtest CLI. Tự động tìm và lưu bộ nhớ đệm (cache) máy chủ gần nhất thông qua HTTP API, hiển thị biểu đồ kim chỉ tốc độ cùng kết quả Download, Upload, Ping, Jitter, Packet Loss và liên kết chia sẻ kết quả trực tuyến.
- 🎯 **Đo tốc độ DNS (DNS Benchmark)**: Sử dụng phương thức gửi UDP DNS Query trực tiếp từ ứng dụng để kiểm tra độ trễ (latency) của 10+ máy chủ DNS phổ biến (Google, Cloudflare, Quad9, AdGuard...) dựa trên từng nhóm nhu cầu truy cập cụ thể:
  - *Mạng Xã Hội / Web*: Google, Youtube, Facebook, Wikipedia, Netflix...
  - *Dịch vụ Trí tuệ nhân tạo (AI)*: ChatGPT, Gemini, Claude, Hugging Face, Perplexity...
  - *Dịch vụ đám mây (Cloud)*: AWS, Azure, Google Cloud, GitHub, Vercel...
  - Tự động xếp hạng và chấm điểm hiệu năng mạng của từng DNS (*Xuất sắc, Tốt, Trung bình, Kém*).
- ⏱️ **Tự động đổi IP theo lịch**: Lên lịch xoay vòng cấu hình hoặc tự động chuyển IP định kỳ.
- 📍 **Tra cứu vị trí địa lý IP Public (GeoIP)**: Hiển thị thông tin chi tiết về IP đang sử dụng gồm Quốc gia, Thành phố, Vùng, Nhà cung cấp dịch vụ Internet (ISP), Tọa độ GPS và Múi giờ (tự động chuyển đổi giữa `ipinfo.io` và `ip-api.com` nếu một trong hai dịch vụ gặp lỗi).
- 📊 **Kiểm tra kết nối nhanh & Ghi lịch sử**: Ping nhanh Gateway, Internet (8.8.8.8) và phân giải thử domain kèm nhật ký log chi tiết.
- 🎨 **Giao diện hiện đại & Cao cấp**:
  - **SpeedGaugeControl**: Vòng tốc kế tròn 270 độ được dựng tùy chỉnh thông qua ma trận Logarithmic, mang lại chuyển động mượt mà (60 FPS qua pre-computed brushes) và hiệu ứng glow neon gradient đẹp mắt.
  - Hỗ trợ đa ngôn ngữ (Tiếng Anh & Tiếng Việt).
  - Tự động thay đổi giao diện Sáng / Tối (Dark & Light Mode) theo hệ điều hành.

---

### 🛠️ Công Nghệ Sử Dụng
* **Ứng dụng Desktop (.NET Core)**:
  - Framework: **Avalonia UI** (Framework Desktop đa nền tảng)
  - Kiến trúc: **MVVM (Model-View-ViewModel)**
  - Tùy biến UI: Tự phát triển Custom Control kim tốc kế logarithmic 270° 
  - Thao tác hệ thống: Wrapper gọi các lệnh hệ thống để thay đổi IP (`netsh`, `ip addr/route`, `networksetup`)
  - Icons: **FluentIcons** & **SimpleIcons**
* **Kịch bản / CLI (Python)**:
  - Thư viện GUI: **Dear PyGui**
  - Thư viện CLI: **Rich** & **Colorama** tạo hiệu ứng bảng màu và menu lựa chọn

---

### 🚀 Hướng Dẫn Cài Đặt & Chạy

#### Yêu Cầu Hệ Thống
- **Với bản C# Desktop**: Cần cài đặt .NET 9 SDK.
- **Với bản Python**: Cần cài đặt Python 3.10 trở lên.
- **Quyền hệ thống**: Bắt buộc khởi chạy công cụ bằng quyền **Administrator** (Windows) hoặc **Root / sudo** (Linux/macOS) để cấp phép thực thi thay đổi card mạng của hệ điều hành.

---

#### 1. Khởi Chạy Ứng Dụng C# Avalonia (.NET 9)
```bash
# Di chuyển tới thư mục dự án Avalonia
cd ui_avalonia

# Khôi phục các gói thư viện
dotnet restore

# Biên dịch & Chạy dự án
dotnet run
```
*Lưu ý: Mở CMD/PowerShell bằng quyền Administrator hoặc dùng lệnh `sudo` khi chạy để ứng dụng có quyền sửa cấu hình card mạng.*

---

#### 2. Khởi Chạy Phiên Bản Python (CLI & GUI)
```bash
# Cài đặt các thư viện cần thiết
pip install -r requirements.txt

# Chạy bản giao diện đồ họa nhẹ (Dear PyGui)
python gui.py   # Windows (Chạy Admin)
sudo python gui.py # Linux/macOS

# Chạy bản dòng lệnh (CLI Rich Terminal)
python main.py  # Windows (Chạy Admin)
sudo python main.py # Linux/macOS
```

---

### 📁 Cấu Trúc Thư Mục
```
├── ui_avalonia/               # Dự án C# Avalonia UI chính
│   ├── App.axaml              # Điểm đầu vào ứng dụng & định nghĩa Style Theme
│   ├── Controls/              # Chứa logic Custom Control SpeedGaugeControl
│   ├── Models/                # Định nghĩa các lớp dữ liệu NetworkCard, DnsRecord, IPProfile
│   ├── Services/              # Các dịch vụ xử lý Speedtest, DnsBenchmark, GeoIp, Network...
│   ├── ViewModels/            # Chứa logic điều khiển dữ liệu (MVVM ViewModels)
│   ├── Views/                 # Giao diện hiển thị (Dashboard, Profiles, DNS...)
│   └── ui_avalonia.csproj     # File cấu hình project .NET
├── core/                      # Module xử lý IP Changer, Validator, DNS (Python)
├── ui/                        # Layout giao diện GUI & CLI của bản Python
├── scheduler/                 # Trình lên lịch chạy tự động cho Python
├── utils/                     # Các tiện ích bổ trợ (GeoIP, Speedtest runner, loggers)
├── main.py                    # File chạy CLI Python chính
├── gui.py                     # File chạy GUI PyGui Python chính
└── DNS.txt                    # File cấu hình chứa danh sách máy chủ DNS
```

---

### 📄 License
MIT License — Tự do sử dụng, chỉnh sửa và phân phối cho mục đích cá nhân và thương mại.