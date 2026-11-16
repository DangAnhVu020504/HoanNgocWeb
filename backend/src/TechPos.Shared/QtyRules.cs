namespace TechPos.Shared;

/// <summary>Quy tắc chung cho số lượng hàng hoá: phải là số nguyên (không bán lẻ 0.5 chiếc).</summary>
public static class QtyRules
{
    /// <summary>True nếu là số nguyên (2.0 → true; 2.5 → false).</summary>
    public static bool IsWhole(decimal v) => v == decimal.Truncate(v);
}
