using TechPos.Shared;

namespace TechPos.Application.Inventory;

/// <summary>
/// Tính giá vốn các dòng XUẤT từ chuỗi nhập/xuất của 1 mặt hàng (mục 4.8 — dialog
/// "Tính lại giá vốn"). Logic thuần, KHÔNG có SQL — unit test FIFO/bình quân.
/// GIA_VON lưu DECIMAL(15,2) ⇒ làm tròn 2 chữ số (quy tắc vàng 4: tiền = DECIMAL).
/// </summary>
public static class CostCalculator
{
    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Nhập trước xuất trước (FIFO): tiêu thụ từng lớp nhập theo thứ tự cũ nhất.
    /// Xuất vượt tồn ⇒ phần thiếu dùng đơn giá lớp gần nhất (hoặc fallback giá vốn mặt hàng).
    /// Trả về giá vốn đơn vị cho từng dòng xuất, đúng thứ tự movements.
    /// </summary>
    public static List<decimal> Fifo(
        decimal openingQty, decimal fallbackCost, IReadOnlyList<CostMovement> moves)
    {
        // Lớp tồn: (SL, giá đơn vị) — con trỏ layerIdx tiêu thụ lần lượt
        var layers = new List<(decimal Qty, decimal Cost)>();
        if (openingQty > 0) layers.Add((openingQty, fallbackCost));
        var lastCost = fallbackCost;
        var layerIdx = 0;

        var result = new List<decimal>();
        foreach (var m in moves)
        {
            if (m.SlNhap > 0)
            {
                layers.Add((m.SlNhap, m.GiaVonNhap));
                lastCost = m.GiaVonNhap;
            }
            else if (m.SlXuat > 0)
            {
                var remain = m.SlXuat;
                decimal value = 0;
                while (remain > 0 && layerIdx < layers.Count)
                {
                    var layer = layers[layerIdx];
                    var take = Math.Min(remain, layer.Qty);
                    value += take * layer.Cost;
                    remain -= take;
                    if (take == layer.Qty) layerIdx++;
                    else layers[layerIdx] = (layer.Qty - take, layer.Cost);
                }
                if (remain > 0) value += remain * lastCost; // xuất vượt tồn
                result.Add(Round(value / m.SlXuat));
            }
        }
        return result;
    }

    /// <summary>
    /// Bình quân gia quyền liên hoàn (moving average): sau mỗi lần nhập tính lại
    /// giá bình quân của toàn bộ tồn; xuất lấy theo bình quân hiện tại.
    /// </summary>
    public static List<decimal> MovingAverage(
        decimal openingQty, decimal fallbackCost, IReadOnlyList<CostMovement> moves)
    {
        var qty = Math.Max(0, openingQty);
        var value = qty * fallbackCost;
        var avg = fallbackCost;

        var result = new List<decimal>();
        foreach (var m in moves)
        {
            if (m.SlNhap > 0)
            {
                value += m.SlNhap * m.GiaVonNhap;
                qty += m.SlNhap;
                if (qty > 0) avg = value / qty;
            }
            else if (m.SlXuat > 0)
            {
                result.Add(Round(avg));
                value = Math.Max(0, value - m.SlXuat * avg);
                qty = Math.Max(0, qty - m.SlXuat);
                if (qty <= 0) avg = fallbackCost;
            }
        }
        return result;
    }

    /// <summary>
    /// Bình quân cuối kỳ (periodic average): 1 giá bình quân cho toàn kỳ =
    /// (giá trị tồn đầu + giá trị nhập trong kỳ) / (SL tồn đầu + SL nhập trong kỳ).
    /// </summary>
    public static List<decimal> PeriodicAverage(
        decimal openingQty, decimal fallbackCost, IReadOnlyList<CostMovement> moves)
    {
        var totalInQty = Math.Max(0, openingQty);
        var totalInValue = totalInQty * fallbackCost;
        var outCount = 0;
        foreach (var m in moves)
        {
            if (m.SlNhap > 0)
            {
                totalInQty += m.SlNhap;
                totalInValue += m.SlNhap * m.GiaVonNhap;
            }
            else if (m.SlXuat > 0) outCount++;
        }
        var avg = totalInQty > 0 ? totalInValue / totalInQty : fallbackCost;
        return Enumerable.Repeat(Round(avg), outCount).ToList();
    }
}