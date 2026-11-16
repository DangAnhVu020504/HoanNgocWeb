using FluentAssertions;
using TechPos.Application.Payments;
using TechPos.Shared;
using Xunit;

namespace TechPos.UnitTests;

public class MoMoSignatureTests
{
    private static MoMoOptions Cfg() => new()
    {
        Enabled = true,
        PartnerCode = "MOMO",
        AccessKey = "F8BBA842ECF85",
        SecretKey = "K951B6PE1waDMi640xX08PD3vg6EkVlz",
        Endpoint = "https://test-payment.momo.vn/v2/gateway/api/create",
        RedirectUrl = "https://shop.local/momo/return",
        IpnUrl = "https://shop.local/api/payments/momo/ipn",
        RequestType = "captureWallet",
    };

    [Fact]
    public void HmacSha256_matches_known_vector()
    {
        MoMoService.HmacSha256("key", "The quick brown fox jumps over the lazy dog")
            .Should().Be("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8");
    }

    [Fact]
    public void Create_signature_uses_momo_canonical_order()
    {
        var c = Cfg();
        var raw = "accessKey=F8BBA842ECF85&amount=10000&extraData=&ipnUrl=https://shop.local/api/payments/momo/ipn"
                  + "&orderId=MM123&orderInfo=Thanh toan don HD001&partnerCode=MOMO"
                  + "&redirectUrl=https://shop.local/momo/return&requestId=req1&requestType=captureWallet";

        MoMoService.BuildCreateSignature(c, 10000, "MM123", "req1", "Thanh toan don HD001", "")
            .Should().Be(MoMoService.HmacSha256(c.SecretKey, raw));
    }

    [Fact]
    public void Ipn_signature_covers_all_momo_fields()
    {
        var c = Cfg();
        var ipn = new MoMoIpnRequest
        {
            PartnerCode = "MOMO",
            OrderId = "MM123",
            RequestId = "req1",
            Amount = 10000,
            OrderInfo = "Thanh toan don HD001",
            OrderType = "momo_wallet",
            TransId = 987654321,
            ResultCode = 0,
            Message = "Successful.",
            PayType = "qr",
            ResponseTime = 1700000000000,
            ExtraData = "",
        };
        var raw = "accessKey=F8BBA842ECF85&amount=10000&extraData=&message=Successful."
                  + "&orderId=MM123&orderInfo=Thanh toan don HD001&orderType=momo_wallet"
                  + "&partnerCode=MOMO&payType=qr&requestId=req1&responseTime=1700000000000"
                  + "&resultCode=0&transId=987654321";

        MoMoService.BuildIpnSignature(c, ipn)
            .Should().Be(MoMoService.HmacSha256(c.SecretKey, raw));
    }
}
