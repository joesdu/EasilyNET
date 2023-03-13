#### EasilyNET.Images

包含 QRCode 工具,由于绘图等一些操作需要平台依赖包支持,所以会较大,因此单独打包.
简化二维码生成,仅 5 个 API,一般仅需使用 GetBase64 就够了.

#### 使用 QRCode 功能

- 使用 Nuget GUI 工具添加至项目
- Install-Package EasilyNET.Images
- 若是遇到 System.ArgumentException: 'shift_jis' is not a supported encoding name. 的错误需要进行如下操作.
- 这是由于遇到了不支持的字符集的问题.
- 在主项目中添加 System.Text.Encoding.CodePages 库,并在程序入口处添加注册代码. Programe.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
```

#### QRCode 生成以及解析

- 1.GetBase64 方法.

```csharp
/// <summary>
/// 生成二维码(默认大小:320*320)
/// </summary>
/// <param name="text">文本内容</param>
/// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
/// <param name="width">二维码边长</param>
/// <param name="format">生成格式,默认:PNG</param>
/// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
public static string GetBase64(string text, int keepWhiteBorderPixelVal = -1, int width = 320, SKEncodedImageFormat format = SKEncodedImageFormat.Png, string background = "FFF", string foreground = "000")
```

- 2.获取图片的二进制数组.

```csharp
/// <summary>
/// 生成二维码(320*320)
/// </summary>
/// <param name="text">文本内容</param>
/// <param name="logoImage">Logo图片(缩放到真实二维码区域尺寸的1/6)</param>
/// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
/// <param name="width">边长,默认:320px</param>
/// <param name="format">生成格式,默认:PNG</param>
/// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <returns></returns>
public static byte[] QrCoder(string text, byte[]? logoImage = null, int keepWhiteBorderPixelVal = -1, int width = 320, SKEncodedImageFormat format = SKEncodedImageFormat.Png, string background = "FFF", string foreground = "000")
```

- 3.传入二维码的 base64 字符串,解析二维码.

```csharp
/// <summary>
/// 从Base64解析二维码
/// </summary>
/// <param name="base64"></param>
/// <returns></returns>
public static string QrDecoder(string base64)
```

- 4.通过 byte 数组解析二维码

```csharp
/// <summary>
/// 从byte数组中解析二维码
/// </summary>
/// <param name="data"></param>
/// <returns></returns>
public static string QrDecoder(string base64)
```

- 5.从流中解析二维码

```csharp
/// <summary>
/// 从流中解析二维码
/// </summary>
/// <param name="stream"></param>
/// <returns></returns>
public static string QrDecoder(Stream stream)
```
