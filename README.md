# 🎯 FaceIT Scoreboard Plugin

[![Source 2](https://img.shields.io/badge/Source%202-orange?style=for-the-badge&logo=valve&logoColor=white)](https://developer.valvesoftware.com/wiki/Source_2)
[![CounterStrikeSharp](https://img.shields.io/badge/CounterStrikeSharp-blue?style=for-the-badge&logo=counter-strike&logoColor=white)](https://github.com/roflmuffin/CounterStrikeSharp)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-11.0-green?style=for-the-badge&logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)

[![Stars](https://img.shields.io/github/stars/zhw1nq/FaceIT_Scoreboard?style=social)](https://github.com/zhw1nq/FaceIT_Scoreboard/stargazers)
[![Forks](https://img.shields.io/github/forks/zhw1nq/FaceIT_Scoreboard?style=social)](https://github.com/zhw1nq/FaceIT_Scoreboard/network/members)
[![Watchers](https://img.shields.io/github/watchers/zhw1nq/FaceIT_Scoreboard?style=social)](https://github.com/zhw1nq/FaceIT_Scoreboard/watchers)

📖 **[README.md](README.md)** | **[README-ENGLISH.md](README-ENGLISH.md)**

> **Hiển thị cấp độ FaceIT trên bảng điểm – biết ai đang "carry" chỉ trong một cái nhìn.**

Plugin CounterStrikeSharp này nâng cao máy chủ CS2 của bạn bằng cách hiển thị cấp độ kỹ năng FaceIT trực tiếp trên bảng điểm sử dụng các đồng xu/huy chương tùy chỉnh. Người chơi có thể bật/tắt hiển thị cấp độ FaceIT và plugin lưu trữ dữ liệu hiệu quả để giảm thiểu các lệnh gọi API.

## ✨ Tính năng

- 🏆 **Hiển thị Cấp độ FaceIT**: Hiển thị cấp độ kỹ năng FaceIT (1-10) dưới dạng đồng xu tùy chỉnh trên bảng điểm
- ⚡ **Cập nhật Thời gian thực**: Tự động lấy và cập nhật cấp độ FaceIT của người chơi
- 🔄 **Điều khiển Người chơi**: Bật/tắt hiển thị cấp độ FaceIT bằng các lệnh đơn giản
- 💾 **Bộ nhớ đệm Thông minh**: Hệ thống bộ nhớ đệm hiệu quả để giảm các lệnh gọi API và cải thiện hiệu suất
- 🎮 **Hỗ trợ Đa trò chơi**: Hỗ trợ cả dữ liệu FaceIT CS2 và CSGO
- ⚙️ **Có thể Cấu hình**: Nhiều tùy chọn cấu hình để tùy chỉnh
- 💿 **Dữ liệu Bền vững**: Tùy chọn người chơi được lưu qua các lần khởi động lại máy chủ

## 🎨 Đồng xu Cấp độ FaceIT

Plugin sử dụng ID đồng xu tùy chỉnh để đại diện cho các cấp độ kỹ năng FaceIT khác nhau:

| Cấp độ | ID Đồng xu | Mô tả |
|--------|------------|-------|
| 1      | 1088       | Cấp độ 1  |
| 2      | 1087       | Cấp độ 2  |
| 3      | 1032       | Cấp độ 3  |
| 4      | 1055       | Cấp độ 4  |
| 5      | 1041       | Cấp độ 5  |
| 6      | 1074       | Cấp độ 6  |
| 7      | 1039       | Cấp độ 7  |
| 8      | 1067       | Cấp độ 8  |
| 9      | 1061       | Cấp độ 9  |
| 10     | 1017       | Cấp độ 10 |

## 📋 Yêu cầu

- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) (Phiên bản API tối thiểu: 147)
- Máy chủ CS2 Dedicated
- FaceIT API Key

## 🔧 Cài đặt

1. **Tải xuống** và cài đặt bản phát hành mới nhất
2. **Tùy chỉnh** tệp cấu hình (`addons/counterstrikesharp/configs/plugins/FaceIT_Scoreboard/FaceIT_Scoreboard.json`)
3. **Tải xuống** kho workshop nằm trong thư mục gốc của releases hiện tại và cài đặt KHÔNG CẦN BIÊN DỊCH
   
   **Lưu ý**: Đặt các tệp không phải trong `content/csgo_addons/****`, mà trong đường dẫn `game/csgo_addons/****`

4. **Khởi động lại** máy chủ hoặc sử dụng `css_plugins reload`

## ⚙️ Cấu hình

Plugin tạo tệp cấu hình tại:
```
/game/csgo/addons/counterstrikesharp/configs/plugins/FaceIT_Scoreboard/FaceIT_Scoreboard.json
```

### Cấu hình Mặc định

```json
{
  "FaceitApiKey": "",
  "UseCSGO": false,
  "DefaultStatus": true,
  "Commands": ["!faceit", "!fl"],
  "CacheExpiryHours": 24,
  "MaxConcurrentRequests": 10,
  "RequestTimeoutSeconds": 10,
  "ConfigVersion": 2
}
```

### Các Tùy chọn Cấu hình

| Tùy chọn | Loại | Mô tả | Mặc định |
|----------|------|-------|----------|
| `FaceitApiKey` | string | FaceIT API key của bạn (**Bắt buộc**) | `""` |
| `UseCSGO` | boolean | Dự phòng dữ liệu CSGO nếu không tìm thấy CS2 | `false` |
| `DefaultStatus` | boolean | Hiển thị cấp độ FaceIT mặc định cho người chơi mới | `true` |
| `Commands` | array | Lệnh để bật/tắt hiển thị FaceIT | `["!faceit", "!fl"]` |
| `CacheExpiryHours` | integer | Số giờ trước khi tải lại dữ liệu người chơi | `24` |
| `MaxConcurrentRequests` | integer | Số lượng yêu cầu API đồng thời tối đa | `10` |
| `RequestTimeoutSeconds` | integer | Thời gian chờ yêu cầu API | `10` |

### 🔑 Lấy FaceIT API Key

1. Truy cập [FaceIT Developer Portal](https://developers.faceit.com/)
2. Đăng nhập bằng tài khoản FaceIT của bạn
3. Tạo ứng dụng mới
4. Sao chép API key vào tệp cấu hình

## 🎮 Lệnh

### Lệnh Người chơi

| Lệnh | Bí danh | Mô tả |
|------|---------|-------|
| `!faceit` | `!fl` | Bật/tắt hiển thị cấp độ FaceIT |

### Lệnh Console

| Lệnh | Mô tả |
|------|-------|
| `css_faceit` | Bật/tắt hiển thị cấp độ FaceIT (console) |
| `css_fl` | Bật/tắt hiển thị cấp độ FaceIT (console) |

## 📁 Cấu trúc Tệp

```
addons/counterstrikesharp/plugins/FaceIT_Scoreboard/
├── FaceIT_Scoreboard.dll          # Tệp plugin chính
├── FaceIT_Scoreboard.pdb          # Ký hiệu debug
└── data/
    └── faceit_data.json           # Bộ nhớ đệm dữ liệu người chơi
```

## 🐛 Khắc phục Sự cố

### Các Vấn đề Thường gặp

1. **Không hiển thị cấp độ FaceIT**
   - Kiểm tra xem FaceIT API key đã được cấu hình đúng chưa
   - Xác minh người chơi có tài khoản FaceIT được liên kết với Steam ID
   - Kiểm tra console máy chủ để xem lỗi API

2. **Plugin không tải**
   - Đảm bảo phiên bản CounterStrikeSharp tối thiểu (147) đã được đáp ứng
   - Xác minh quyền truy cập tệp
   - Kiểm tra các plugin xung đột

3. **Vấn đề hiệu suất**
   - Giảm giá trị `MaxConcurrentRequests`
   - Tăng `CacheExpiryHours` để giảm các lệnh gọi API
   - Theo dõi tài nguyên máy chủ

## 🙏 Tín dụng

- **Ý tưởng Gốc**: Dựa trên ý tưởng từ [Pisex's cs2-faceit-level](https://github.com/Pisex/cs2-faceit-level)
- **Framework**: [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) bởi roflmuffin
- **API**: [FaceIT Data API](https://developers.faceit.com/)

## 💬 Hỗ trợ & Cộng đồng

- **Hỗ trợ Discord**: [@vhming_](https://discord.com/users/vhming_)
- **Cộng đồng CounterStrikeSharp**: [Tham gia Discord](https://discord.gg/eA9QTuNYkp)

---

<div align="center">
<i>Được tạo với ❤️ cho cộng đồng CS2</i>
</div>
