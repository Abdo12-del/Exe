using ZXing;
using ZXing.Common;

namespace SalesManagement.Reporting.Services;

public interface IBarcodeGeneratorService
{
    byte[] GenerateBarcodePng(string content, int width = 300, int height = 90);
    byte[] GenerateQrCodePng(string content, int size = 200);
}

public class BarcodeGeneratorService : IBarcodeGeneratorService
{
    public byte[] GenerateBarcodePng(string content, int width = 300, int height = 90)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.CODE_128,
            Options = new EncodingOptions
            {
                Height = height,
                Width = width,
                Margin = 2,
                PureBarcode = false
            }
        };

        var pixelData = writer.Write(content);
        return EncodeBmp(pixelData.Pixels, pixelData.Width, pixelData.Height);
    }

    public byte[] GenerateQrCodePng(string content, int size = 200)
    {
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new EncodingOptions
            {
                Height = size,
                Width = size,
                Margin = 2
            }
        };

        var pixelData = writer.Write(content);
        return EncodeBmp(pixelData.Pixels, pixelData.Width, pixelData.Height);
    }

    // Pure self-contained BMP encoder for raw RGBA bytes without System.Drawing dependencies
    private static byte[] EncodeBmp(byte[] pixels, int width, int height)
    {
        int rowSize = (width * 3 + 3) & ~3;
        int imageSize = rowSize * height;
        int fileSize = 54 + imageSize;

        using var ms = new MemoryStream(fileSize);
        using var bw = new BinaryWriter(ms);

        // Header
        bw.Write((byte)'B');
        bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write((short)0);
        bw.Write((short)0);
        bw.Write(54); // Offset

        // DIB Header
        bw.Write(40); // Size
        bw.Write(width);
        bw.Write(height);
        bw.Write((short)1); // Planes
        bw.Write((short)24); // 24 bpp
        bw.Write(0); // Compression
        bw.Write(imageSize);
        bw.Write(2835); // 72 DPI
        bw.Write(2835);
        bw.Write(0);
        bw.Write(0);

        // Pixels (bottom-up, BGR)
        byte[] row = new byte[rowSize];
        for (int y = height - 1; y >= 0; y--)
        {
            int rowOffset = y * width * 4;
            int destIdx = 0;
            for (int x = 0; x < width; x++)
            {
                int srcIdx = rowOffset + (x * 4);
                byte b = pixels[srcIdx + 0];
                byte g = pixels[srcIdx + 1];
                byte r = pixels[srcIdx + 2];

                row[destIdx++] = b;
                row[destIdx++] = g;
                row[destIdx++] = r;
            }
            bw.Write(row);
        }

        return ms.ToArray();
    }
}
