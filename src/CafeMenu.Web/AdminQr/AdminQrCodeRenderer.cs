using System.Text;
using QRCoder;

namespace CafeMenu.Web.AdminQr;

public sealed class AdminQrCodeRenderer : IAdminQrCodeRenderer
{
    private const int PngPixelsPerModule = 12;
    private const int SvgPixelsPerModule = 10;

    public byte[] GeneratePng(string content)
    {
        using var qrCodeData = QRCodeGenerator.GenerateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);

        return qrCode.GetGraphic(PngPixelsPerModule, drawQuietZones: true);
    }

    public string GenerateSvg(string content)
    {
        using var qrCodeData = QRCodeGenerator.GenerateQrCode(content, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new SvgQRCode(qrCodeData);

        return qrCode.GetGraphic(
            SvgPixelsPerModule,
            darkColorHex: "#000000",
            lightColorHex: "#ffffff",
            drawQuietZones: true,
            sizingMode: SvgQRCode.SizingMode.ViewBoxAttribute);
    }

    public byte[] GenerateSvgBytes(string content)
    {
        return Encoding.UTF8.GetBytes(GenerateSvg(content));
    }
}
