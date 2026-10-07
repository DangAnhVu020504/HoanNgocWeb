using System.Drawing;
using System.Drawing.Imaging;
using ZXing;
using ZXing.Common;
using ZXing.Windows.Compatibility;

namespace TechPos.Reporting;

/// <summary>
/// Sinh mã vạch (ZXing.Net + System.Drawing renderer) — Phase 6
/// (KE-HOACH §6.3: in tem mã vạch mặt hàng; chỉ Windows — SupportedOSPlatform trong csproj).
/// Mặc định CODE_128 (chuẩn tem kho). Trả PNG bytes.
/// </summary>
public static partial class BarcodeGenerator
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static byte[] CreatePng(string content, int width = 300, int height = 80)
    {
        var writer = new BarcodeWriter
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Width = width,
                Height = height,
                Margin = 4,
                PureBarcode = false
            }
        };
        using var bitmap = writer.Write(content ?? "");
        using var ms = new MemoryStream();
        bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }
}
