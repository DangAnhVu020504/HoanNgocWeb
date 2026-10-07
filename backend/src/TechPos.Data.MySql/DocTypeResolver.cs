namespace TechPos.Data.MySql;

/// <summary>
/// Phân loại chứng từ TDONHANG đa năng khi ĐỌC (in phiếu kho — Phase 6.3).
/// Nguyên tắc (quy tắc vàng 5): không đoán LOAI/MODE. Phân loại theo cấu trúc dữ liệu:
///   kho xuất + kho nhập → CHUYEN_KHO · chỉ kho nhập → NHAP_KHO · chỉ kho xuất → XUAT_KHO.
/// Ngoài ra tra ngược từ số phiếu qua tiền tố trong W_DOC_TYPE_MAP (PN/KTL/BL/BS/TL/XK/CK/XT).
/// </summary>
public static class DocTypeResolver
{
    /// <summary>Phân loại theo cấu trúc kho (độ tin cậy cao nhất, không phụ thuộc LOAI).</summary>
    public static string ResolveByWarehouses(int? loai, int? mode, bool hasKhoNhap, bool hasKhoXuat) => (hasKhoNhap, hasKhoXuat) switch
    {
        (true, true) => "CHUYEN_KHO",
        (true, false) => "NHAP_KHO",
        (false, true) => "XUAT_KHO",
        _ => "KIEM_KE"    // không có kho = kiểm kê (KTL — đã xác minh LOAI=5 trên DEMO)
    };
}
