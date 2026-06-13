# Kế hoạch Kết hợp Hai Nguồn Logo: FontAwesome & SimpleIcons

Kế hoạch này đề xuất phương án kết hợp tối ưu cả hai thư viện **SimpleIcons** (thông qua gói `IconPacks.Avalonia.SimpleIcons`) và **FontAwesome** để hiển thị logo thương hiệu chính xác và đầy đủ nhất cho cả 30 website đo tốc độ DNS. Các thương hiệu có sẵn trên SimpleIcons sẽ được hiển thị bằng hình ảnh SimpleIcons cực kỳ sắc nét, còn các thương hiệu thiếu sẽ tự động hiển thị bằng FontAwesome.

## Proposed Changes

---

### [ui_avalonia]

#### [MODIFY] [Program.cs](file:///f:/Project%20Code/Mang/ui_avalonia/Program.cs)
* Khôi phục hàm `Main` về cấu hình chạy classic desktop bình thường:
  ```csharp
  [STAThread]
  public static void Main(string[] args) => BuildAvaloniaApp()
      .StartWithClassicDesktopLifetime(args);
  ```

#### [MODIFY] [App.axaml](file:///f:/Project%20Code/Mang/ui_avalonia/App.axaml)
* Bổ sung tài nguyên giao diện của SimpleIcons vào phần Styles của ứng dụng:
  ```xml
  <StyleInclude Source="avares://IconPacks.Avalonia.SimpleIcons/SimpleIcons.axaml" />
  ```

#### [MODIFY] [ViewModels/DnsBenchmarkViewModel.cs](file:///f:/Project%20Code/Mang/ui_avalonia/ViewModels/DnsBenchmarkViewModel.cs)
* Mở rộng `DomainItem` để hỗ trợ định danh nguồn của Icon và kiểu dữ liệu Enum an toàn cho SimpleIcons:
  ```csharp
  public class DomainItem : ObservableObject
  {
      public string Label { get; set; } = string.Empty;
      public string Domain { get; set; } = string.Empty;
      public Symbol Icon { get; set; } = Symbol.Globe;
      public string LogoIconKey { get; set; } = string.Empty;
      public string LogoColor { get; set; } = "#FFFFFF";
      public string IconSource { get; set; } = "SimpleIcons"; // "SimpleIcons" hoặc "FontAwesome"
      
      public bool IsSimpleIcon => IconSource == "SimpleIcons";
      public bool IsFontAwesome => IconSource == "FontAwesome";
      
      public IconPacks.Avalonia.SimpleIcons.PackIconSimpleIconsKind SimpleIconKind
      {
          get
          {
              if (IsSimpleIcon && Enum.TryParse<IconPacks.Avalonia.SimpleIcons.PackIconSimpleIconsKind>(LogoIconKey, out var result))
              {
                  return result;
              }
              return IconPacks.Avalonia.SimpleIcons.PackIconSimpleIconsKind.None;
          }
      }
      
      public string Display => $"{Label}";

      private bool _isSelected;
      public bool IsSelected
      {
          get => _isSelected;
          set
          {
              if (SetProperty(ref _isSelected, value))
              {
                  OnPropertyChanged(nameof(LogoColorToShow));
              }
          }
      }

      public string LogoColorToShow => IsSelected ? "#FFFFFF" : LogoColor;
  }
  ```
* Cập nhật hàm `ChangeGroup` trong `DnsBenchmarkViewModel.cs` để nhận và gán đúng thuộc tính `IconSource` từ Service.

#### [MODIFY] [Services/DnsBenchmarkService.cs](file:///f:/Project%20Code/Mang/ui_avalonia/Services/DnsBenchmarkService.cs)
* Cập nhật kiểu dữ liệu của 3 danh sách website tĩnh để bao gồm trường nguồn của logo (`IconSource`):
  * Cú pháp tuple: `(string Label, string Domain, Symbol Icon, string LogoIconKey, string LogoColor, string IconSource)`
  * Cập nhật danh sách chi tiết:
    * **Google, Facebook, YouTube, TikTok, Wikipedia, Netflix, Steam, X, Instagram, Cloudflare**: `SimpleIcons`
    * **AWS, Azure, Cursor**: `FontAwesome`
    * **OpenAI, GoogleGemini, Claude, HuggingFace, Perplexity, Poe, GitHub Copilot, ElevenLabs, Suno, GoogleCloud, DigitalOcean, GitHub, GitLab, Vercel, Netlify, Firebase**: `SimpleIcons`

#### [MODIFY] [Views/DnsBenchmarkView.axaml](file:///f:/Project%20Code/Mang/ui_avalonia/Views/DnsBenchmarkView.axaml)
* Thêm namespace XAML cho SimpleIcons:
  `xmlns:iconPacks="clr-namespace:IconPacks.Avalonia.SimpleIcons;assembly=IconPacks.Avalonia.SimpleIcons"`
* Chỉnh sửa cấu trúc nút của `UniformGrid` để hiển thị có điều kiện một trong hai loại logo tùy thuộc vào nguồn `IconSource`:
  * Đối với các card website:
    ```xml
    <!-- SimpleIcons -->
    <iconPacks:PackIconSimpleIcons Grid.RowSpan="2" Grid.Column="0"
                                   Kind="{Binding SimpleIconKind}" 
                                   Width="26" Height="26" 
                                   Foreground="{Binding LogoColorToShow}"
                                   Margin="0,0,12,0"
                                   VerticalAlignment="Center"
                                   HorizontalAlignment="Center"
                                   IsVisible="{Binding IsSimpleIcon}"/>
    <!-- FontAwesome -->
    <i:Icon Grid.RowSpan="2" Grid.Column="0"
            Value="{Binding LogoIconKey}" 
            Width="26" Height="26" 
            Foreground="{Binding LogoColorToShow}"
            Margin="0,0,12,0"
            VerticalAlignment="Center"
            IsVisible="{Binding IsFontAwesome}"/>
    ```
  * Đối với thanh tiến trình đo (🎯 Test với:):
    ```xml
    <!-- SimpleIcons -->
    <iconPacks:PackIconSimpleIcons Kind="{Binding SelectedDomain.SimpleIconKind}" 
                                   Width="18" Height="18"
                                   VerticalAlignment="Center" 
                                   Foreground="{Binding SelectedDomain.LogoColor}"
                                   IsVisible="{Binding SelectedDomain.IsSimpleIcon}"/>
    <!-- FontAwesome -->
    <i:Icon Value="{Binding SelectedDomain.LogoIconKey}" 
            Width="18" Height="18"
            VerticalAlignment="Center" 
            Foreground="{Binding SelectedDomain.LogoColor}"
            IsVisible="{Binding SelectedDomain.IsFontAwesome}"/>
    ```

---

## Verification Plan

### Automated Tests
* Chạy lệnh build:
  ```powershell
  dotnet build ui_avalonia/ui_avalonia.csproj
  ```

### Manual Verification
* Chạy ứng dụng:
  ```powershell
  dotnet run --project ui_avalonia/ui_avalonia.csproj
  ```
* Kiểm tra tab "Tối ưu hóa DNS", xem danh sách website có hiển thị đầy đủ 30 website (trong đó có Netflix, Steam, AWS, Azure, Cursor...) với logo tương ứng từ cả FontAwesome và SimpleIcons hay không.
