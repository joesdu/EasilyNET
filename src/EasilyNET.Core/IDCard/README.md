[^ **\_\_**Back**\_\_** ^](..\README.md)

#### IDCardValidation

> 中国身份证验证,支持 15 位和 18 位身份证号码验证.可计算生日,性别,年龄等信息.同时验证身份证号码的合法性(
> 仅限于生成算法是否合法).

<details>
<summary style="font-size: 14px">English</summary>

> Chinese ID card verification, supports 15-digit and 18-digit ID card number verification. Can calculate birthday, sex,
> age and other information. At the same time, verify the legitimacy of the ID card number (only whether the generation
> algorithm is legal).

</details>

##### 使用方法(Usage)

```csharp
var check = "52305199405088125".CheckIDCard(); // true or false
var birthday = "52305199405088125".CalculateBirthday(); // DateOnly
var gender = "52305199405088125".CalculateGender(); // EGender
var age = "52305199405088125".CalculateAge(); // Int32
```
