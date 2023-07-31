##### EasilyNET.Security

一个.Net 中常用的加密算法的封装.降低加密解密的使用复杂度.

- 目前有的算法:AES,DES,SHA,RC4,TripleDES,RSA,SM2,SM3
- 支持 RSA XML 结构的 SecurityKey 和 Base64 格式的互转.

- 本库不是去实现加密算法,而是基于.Net 提供的接口封装,为了方便使用

- 未经测试的预测,若是遇到了解密乱码,可能是需要引入一个包.
- 在主项目中添加 System.Text.Encoding.CodePages 库,并在程序入口处添加注册代码. Programe.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
```
