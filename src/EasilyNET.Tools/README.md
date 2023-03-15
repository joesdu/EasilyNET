#### EasilyNET.Tools

- 新增 Ini 文件帮助类.
- 一些基础库,如数据类型,一些公共静态方法,工具函数.包含数组,日期,字符串,中国农历,拼音,身份证验证等功能

##### 加入身份证校验以及通过身份证号码获取生日,年龄,以及性别

```csharp
var check = "52305199405088125".CheckIDCard(); // true or false
var birthday = "52305199405088125".CalculateBirthday(); // DateOnly
var gender = "52305199405088125".CalculateGender(); // EGender
var age = "52305199405088125".CalculateAge(); // Int32
```