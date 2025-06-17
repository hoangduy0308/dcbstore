using System.Text.Json;
using Microsoft.AspNetCore.Http; // Đảm bảo đã có using này
using System.Text.Json.Serialization; // BẮT ĐẦU PHẦN THÊM MỚI

namespace DCBStore.Helpers
{
    public static class SessionHelper
    {
        public static void SetObjectAsJson(this ISession session, string key, object value)
        {
            // BẮT ĐẦU PHẦN SỬA ĐỔI
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve, // Xử lý tham chiếu vòng tròn
                MaxDepth = 256 // Tăng giới hạn độ sâu nếu cần (mặc định là 64)
            };
            session.SetString(key, JsonSerializer.Serialize(value, options));
            // KẾT THÚC PHẦN SỬA ĐỔI
        }

        public static T? GetObjectFromJson<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            // BẮT ĐẦU PHẦN SỬA ĐỔI
            var options = new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.Preserve,
                MaxDepth = 256
            };
            return value == null ? default : JsonSerializer.Deserialize<T>(value, options);
            // KẾT THÚC PHẦN SỬA ĐỔI
        }
    }
}