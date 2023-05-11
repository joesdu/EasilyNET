using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;
using ZXing.QrCode.Internal;

namespace EasilyNET.Images.QrCode;

/// <summary>
/// 二维码帮助类
/// </summary>
internal static class QrCodeHelper
{
    /// <summary>
    /// 生成二维码(320*320)
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="logoImage">Logo图片(缩放到真实二维码区域尺寸的1/6)</param>
    /// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
    /// <param name="width">边长,默认:320px</param>
    /// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <returns></returns>
    internal static byte[] Encode(string text, byte[]? logoImage = null, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")
    {
        // ReSharper disable once InlineTemporaryVariable
        var height = width;
        var qRCodeWriter = new QRCodeWriter();
        var hints = new Dictionary<EncodeHintType, object>
        {
            { EncodeHintType.CHARACTER_SET, "utf-8" },
            { EncodeHintType.QR_VERSION, 9 },
            { EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.L }
        };
        var bitMatrix = qRCodeWriter.encode(text, BarcodeFormat.QR_CODE, width, height, hints);
        var w = bitMatrix.Width;
        var h = bitMatrix.Height;
        var sKBitmap = new SKBitmap(w, h);
        var blackStartPointX = 0;
        var blackStartPointY = 0;
        var blackEndPointX = w;
        var blackEndPointY = h;

        #region --绘制二维码(同时获取真实的二维码区域起绘点和结束点的坐标)--

        using var sKCanvas = new SKCanvas(sKBitmap);
        var sKColorForeground = SKColor.Parse(foreground);
        var sKColorBackground = SKColor.Parse(background);
        sKCanvas.Clear(sKColorBackground);
        var blackStartPointIsNotWriteDown = true;
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var flag = bitMatrix[x, y];
                if (!flag) continue;
                if (blackStartPointIsNotWriteDown)
                {
                    blackStartPointX = x;
                    blackStartPointY = y;
                    blackStartPointIsNotWriteDown = false;
                }
                blackEndPointX = x;
                blackEndPointY = y;
                sKCanvas.DrawPoint(x, y, sKColorForeground);
            }
        }

        #endregion

        var qrCodeRealWidth = blackEndPointX - blackStartPointX;
        var qrCodeRealHeight = blackEndPointY - blackStartPointY;

        #region -- 处理白边 --

        if (keepWhiteBorderPixelVal > -1) //指定了边框宽度
        {
            var borderMaxWidth = (int)Math.Floor((double)qrCodeRealWidth / 10);
            if (keepWhiteBorderPixelVal > borderMaxWidth)
            {
                keepWhiteBorderPixelVal = borderMaxWidth;
            }
            var nQrCodeRealWidth = width - keepWhiteBorderPixelVal - keepWhiteBorderPixelVal;
            var nQrCodeRealHeight = height - keepWhiteBorderPixelVal - keepWhiteBorderPixelVal;
            using var sKBitmap2 = new SKBitmap(width, height);
            using var sKCanvas2 = new SKCanvas(sKBitmap2);
            sKCanvas2.Clear(sKColorBackground);
            //二维码绘制到临时画布上时无需抗锯齿等处理(避免文件增大)
            sKCanvas2.DrawBitmap(sKBitmap, new()
                {
                    Location = new() { X = blackStartPointX, Y = blackStartPointY },
                    Size = new() { Height = qrCodeRealHeight, Width = qrCodeRealWidth }
                },
                new SKRect
                {
                    Location = new() { X = keepWhiteBorderPixelVal, Y = keepWhiteBorderPixelVal },
                    Size = new() { Width = nQrCodeRealWidth, Height = nQrCodeRealHeight }
                });
            blackStartPointX = keepWhiteBorderPixelVal;
            blackStartPointY = keepWhiteBorderPixelVal;
            qrCodeRealWidth = nQrCodeRealWidth;
            qrCodeRealHeight = nQrCodeRealHeight;
            sKBitmap.Dispose();
            sKBitmap = sKBitmap2;
        }

        #endregion

        #region -- 绘制LOGO --

        if (logoImage is not null && logoImage.Length > 0)
        {
            using var sKBitmapLogo = SKBitmap.Decode(logoImage);
            if (!sKBitmapLogo.IsEmpty)
            {
                var logoTargetMaxWidth = (int)Math.Floor((double)qrCodeRealWidth / 6);
                var logoTargetMaxHeight = (int)Math.Floor((double)qrCodeRealHeight / 6);
                var qrCodeCenterX = (int)Math.Floor((double)qrCodeRealWidth / 2);
                var qrCodeCenterY = (int)Math.Floor((double)qrCodeRealHeight / 2);
                var logoResultWidth = sKBitmapLogo.Width;
                var logoResultHeight = sKBitmapLogo.Height;
                if (logoResultWidth > logoTargetMaxWidth)
                {
                    var r = (double)logoTargetMaxWidth / logoResultWidth;
                    logoResultWidth = logoTargetMaxWidth;
                    logoResultHeight = (int)Math.Floor(logoResultHeight * r);
                }
                if (logoResultHeight > logoTargetMaxHeight)
                {
                    var r = (double)logoTargetMaxHeight / logoResultHeight;
                    logoResultHeight = logoTargetMaxHeight;
                    logoResultWidth = (int)Math.Floor(logoResultWidth * r);
                }
                var pointX = qrCodeCenterX - (int)Math.Floor((double)logoResultWidth / 2) + blackStartPointX;
                var pointY = qrCodeCenterY - (int)Math.Floor((double)logoResultHeight / 2) + blackStartPointY;
                using var sKCanvas3 = new SKCanvas(sKBitmap);
                using var sKPaint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.Medium,
                    IsAntialias = true
                };
                sKCanvas3.DrawBitmap(sKBitmapLogo, new SKRect
                    {
                        Location = new() { X = 0, Y = 0 },
                        Size = new() { Height = sKBitmapLogo.Height, Width = sKBitmapLogo.Width }
                    },
                    new()
                    {
                        Location = new() { X = pointX, Y = pointY },
                        Size = new() { Height = logoResultHeight, Width = logoResultWidth }
                    }, sKPaint);
            }
        }

        #endregion

        using var sKImage = SKImage.FromBitmap(sKBitmap);
        sKBitmap.Dispose();
        using var data = sKImage.Encode(SKEncodedImageFormat.Png, 75);
        return data.ToArray();
    }

    /// <summary>
    /// 从流中解析二维码
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    internal static string Decode(Stream stream)
    {
        using var sKManagedStream = new SKManagedStream(stream, true);
        using var sKBitmap = SKBitmap.Decode(sKManagedStream) ?? throw new("未识别的图片文件");
        var w = sKBitmap.Width;
        var h = sKBitmap.Height;
        var ps = w * h;
        var bytes = new byte[ps * 3];
        var byteIndex = 0;
        for (var x = 0; x < w; x++)
        {
            for (var y = 0; y < h; y++)
            {
                var color = sKBitmap.GetPixel(x, y);
                bytes[byteIndex + 0] = color.Red;
                bytes[byteIndex + 1] = color.Green;
                bytes[byteIndex + 2] = color.Blue;
                byteIndex += 3;
            }
        }
        var rGbLuminanceSource = new RGBLuminanceSource(bytes, w, h);
        var hybrid = new HybridBinarizer(rGbLuminanceSource);
        var binaryBitmap = new BinaryBitmap(hybrid);
        var hints = new Dictionary<DecodeHintType, object>
        {
            { DecodeHintType.CHARACTER_SET, "utf-8" }
        };
        var qRCodeReader = new QRCodeReader();
        var result = qRCodeReader.decode(binaryBitmap, hints);
        return result is not null ? result.Text : "";
    }
}