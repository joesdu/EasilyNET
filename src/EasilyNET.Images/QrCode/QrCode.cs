using System.Text.Json;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Images.QrCode;

/// <summary>
/// QRCode工具类.
/// </summary>
public static class QrCode
{
    /// <summary>
    /// 生成二维码(默认大小:320*320)
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
    /// <param name="width">二维码边长</param>
    /// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <returns>Base64字符串</returns>
    public static string Encode(string text, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")
    {
        var bytes = QrCodeHelper.Encode(text, null, keepWhiteBorderPixelVal, width, background, foreground);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    /// <summary>
    /// 生成二维码(320*320)
    /// </summary>
    /// <param name="text">文本内容</param>
    /// <param name="logoImage">Logo图片Base64(缩放到真实二维码区域尺寸的1/6)</param>
    /// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
    /// <param name="width">边长,默认:320px</param>
    /// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <returns></returns>
    public static string Encode(string text, string logoImage, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")
    {
        var logo = Convert.FromBase64String(logoImage);
        var bytes = QrCodeHelper.Encode(text, logo, keepWhiteBorderPixelVal, width, background, foreground);
        return $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
    }

    /// <summary>
    /// 生成二维码(默认大小:320*320)
    /// </summary>
    /// <param name="obj">编码对象</param>
    /// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
    /// <param name="width">二维码边长</param>
    /// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <returns>Base64字符串</returns>
    public static string Encode<T>(T obj, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")
    {
        var json = JsonSerializer.Serialize(obj);
        return Encode(json, keepWhiteBorderPixelVal, width, background, foreground);
    }

    /// <summary>
    /// 生成二维码(默认大小:320*320)
    /// </summary>
    /// <param name="obj">编码对象</param>
    /// <param name="logoImage">Logo图片Base64(缩放到真实二维码区域尺寸的1/6)</param>
    /// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
    /// <param name="width">二维码边长</param>
    /// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
    /// <returns>Base64字符串</returns>
    public static string Encode<T>(T obj, string logoImage, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")
    {
        var json = JsonSerializer.Serialize(obj);
        return Encode(json, logoImage, keepWhiteBorderPixelVal, width, background, foreground);
    }

    /// <summary>
    /// 从流中解析二维码数据
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    public static string Decode(Stream stream) => QrCodeHelper.Decode(stream);

    /// <summary>
    /// 从流中解析二维码数据并转换成对象
    /// </summary>
    /// <param name="stream"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="JsonException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <returns></returns>
    public static T? Decode<T>(Stream stream)
    {
        var json = Decode(stream);
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// 从byte数组中解析二维码
    /// </summary>
    /// <param name="data"></param>
    /// <returns>编码到二维码中的信息</returns>
    public static string Decode(byte[] data) => QrCodeHelper.Decode(new MemoryStream(data));

    /// <summary>
    /// 从byte数组中解析二维码数据并转换成对象
    /// </summary>
    /// <param name="data"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="JsonException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <returns>编码到二维码中的信息</returns>
    public static T? Decode<T>(byte[] data)
    {
        var json = Decode(data);
        return JsonSerializer.Deserialize<T>(json);
    }

    /// <summary>
    /// 从Base64解析二维码
    /// </summary>
    /// <param name="base64"></param>
    /// <returns>编码到二维码中的信息</returns>
    public static string Decode(string base64)
    {
        ArgumentException.ThrowIfNullOrEmpty(base64, nameof(base64));
        var data = base64[(base64.IndexOf(',', StringComparison.Ordinal) + 1)..];
        return Decode(Convert.FromBase64String(data));
    }

    /// <summary>
    /// 从二维码解析对象数据
    /// </summary>
    /// <typeparam name="T">对象实体</typeparam>
    /// <param name="base64">Base64字符串</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="JsonException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    /// <returns></returns>
    public static T? Decode<T>(string base64)
    {
        ArgumentException.ThrowIfNullOrEmpty(base64, nameof(base64));
        var data = base64[(base64.IndexOf(',', StringComparison.Ordinal) + 1)..];
        var json = Decode(Convert.FromBase64String(data));
        return JsonSerializer.Deserialize<T>(json);
    }
}