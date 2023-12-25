#### EasilyNET.Images

包含 QRCode 工具,由于绘图等一些操作需要平台依赖包支持,所以会较大,因此单独打包.
简化二维码生成,一般仅需使用 Encode 就够了.

#### 使用 QRCode 功能

- 使用 Nuget GUI 工具添加至项目
- Install-Package EasilyNET.Images
- 若包含中文推荐安装 System.Text.Encoding.CodePages
- 并在程序入口处添加注册代码. Programe.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
```

#### QRCode 生成以及解析

- 1.生成二维码.

```csharp
/// <summary>
/// 生成二维码(默认大小:320*320)
/// </summary>
/// <param name="text">文本内容</param>
/// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
/// <param name="width">二维码边长</param>
/// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <returns>Base64字符串</returns>
QrCode.Encode(string text, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")

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
QrCode.Encode(string text, string logoImage, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")

/// <summary>
/// 生成二维码(默认大小:320*320)
/// </summary>
/// <param name="obj">编码对象</param>
/// <param name="keepWhiteBorderPixelVal">白边处理(负值表示不做处理，最大值不超过真实二维码区域的1/10)</param>
/// <param name="width">二维码边长</param>
/// <param name="background">背景色,默认:FFFFFF,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <param name="foreground">前景色,默认:000000,16进制色值编码,不带'#'编码格式: AARRGGB, RRGGBB, ARGB, RGB.</param>
/// <returns>Base64字符串</returns>
QrCode.Encode<T>(T obj, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")

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
QrCode.Encode<T>(T obj, string logoImage, int keepWhiteBorderPixelVal = -1, int width = 320, string background = "FFF", string foreground = "000")
```

- 2.解析二维码.

```csharp
/// <summary>
/// 从流中解析二维码数据
/// </summary>
/// <param name="stream"></param>
/// <returns></returns>
QrCode.Decode(Stream stream)

/// <summary>
/// 从流中解析二维码数据并转换成对象
/// </summary>
/// <param name="stream"></param>
/// <exception cref="ArgumentNullException"></exception>
/// <exception cref="JsonException"></exception>
/// <exception cref="NotSupportedException"></exception>
/// <returns></returns>
QrCode.Decode<T>(Stream stream)

/// <summary>
/// 从byte数组中解析二维码
/// </summary>
/// <param name="data"></param>
/// <returns>编码到二维码中的信息</returns>
QrCode.Decode(byte[] data)

/// <summary>
/// 从byte数组中解析二维码数据并转换成对象
/// </summary>
/// <param name="data"></param>
/// <exception cref="ArgumentNullException"></exception>
/// <exception cref="JsonException"></exception>
/// <exception cref="NotSupportedException"></exception>
/// <returns>编码到二维码中的信息</returns>
QrCode.Decode<T>(byte[] data)

/// <summary>
/// 从Base64解析二维码
/// </summary>
/// <param name="base64"></param>
/// <returns>编码到二维码中的信息</returns>
QrCode.Decode(string base64)

/// <summary>
/// 从二维码解析对象数据
/// </summary>
/// <typeparam name="T">对象实体</typeparam>
/// <param name="base64">Base64字符串</param>
/// <exception cref="ArgumentNullException"></exception>
/// <exception cref="JsonException"></exception>
/// <exception cref="NotSupportedException"></exception>
/// <returns></returns>
QrCode.Decode<T>(string base64)
```
