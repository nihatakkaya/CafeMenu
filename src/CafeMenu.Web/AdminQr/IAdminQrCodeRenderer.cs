namespace CafeMenu.Web.AdminQr;

public interface IAdminQrCodeRenderer
{
    byte[] GeneratePng(string content);

    string GenerateSvg(string content);

    byte[] GenerateSvgBytes(string content);
}
