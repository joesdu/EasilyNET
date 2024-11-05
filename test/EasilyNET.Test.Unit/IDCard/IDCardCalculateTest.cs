using EasilyNET.Core.Enums;
using EasilyNET.Core.IdCard;

namespace EasilyNET.Test.Unit.IDCard;

[TestClass]
public class IDCardCalculateTests
{
    //身份证号码:440232202108049420, 出生日期:2021-08-04, 性别:女,年龄:3,出生地:广东省 韶关市 乳源瑶族自治县
    //身份证号码:330781201209099413, 出生日期:2012-09-09, 性别:男,年龄:12,出生地:浙江省 金华市 兰溪市
    //身份证号码:540324199401114695, 出生日期:1994-01-11, 性别:男,年龄:30,出生地:西藏自治区 昌都市 丁青县
    //身份证号码:150124200608199819, 出生日期:2006-08-19, 性别:男,年龄:18,出生地:内蒙古自治区 呼和浩特市 清水河县
    //身份证号码:141128196708194762, 出生日期:1967-08-19, 性别:女,年龄:57,出生地:山西省 吕梁市 方山县
    //身份证号码:430203201608317139, 出生日期:2016-08-31, 性别:男,年龄:8,出生地:湖南省 株洲市 芦淞区
    //身份证号码:350426199909094501, 出生日期:1999-09-09, 性别:女,年龄:25,出生地:福建省 三明市 尤溪县
    //身份证号码:140725195303314437, 出生日期:1953-03-31, 性别:男,年龄:71,出生地:山西省 晋中市 寿阳县
    //身份证号码:220524199612060283, 出生日期:1996-12-06, 性别:女,年龄:27,出生地:吉林省 通化市 柳河县
    //身份证号码:230204197808110706, 出生日期:1978-08-11, 性别:女,年龄:46,出生地:黑龙江省 齐齐哈尔市 铁锋区
    private static readonly string[] ValidIDs =
    [
        "440232202108049420",
        "330781201209099413",
        "540324199401114695",
        "150124200608199819",
        "141128196708194762",
        "430203201608317139",
        "350426199909094501",
        "140725195303314437",
        "220524199612060283",
        "230204197808110706"
    ];

    [TestMethod]
    public void ValidateIDCard_ValidIDs_ShouldPass()
    {
        foreach (var id in ValidIDs)
        {
            Assert.IsTrue(id.CheckIDCard(), $"ID {id} should be valid.");
        }
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ValidateIDCard_InvalidID_ShouldThrowException()
    {
        const string invalidID = "123456789012345";
        invalidID.ValidateIDCard();
    }

    [TestMethod]
    public void CalculateBirthday_ValidIDs_ShouldReturnCorrectBirthday()
    {
        var expectedBirthdays = new[]
        {
            new DateTime(2021, 08, 04),
            new DateTime(2012, 09, 09),
            new DateTime(1994, 01, 11),
            new DateTime(2006, 08, 19),
            new DateTime(1967, 08, 19),
            new DateTime(2016, 08, 31),
            new DateTime(1999, 09, 09),
            new DateTime(1953, 03, 31),
            new DateTime(1996, 12, 06),
            new DateTime(1978, 08, 11)
        };
        for (var i = 0; i < ValidIDs.Length; i++)
        {
            var id = ValidIDs[i];
            id.CalculateBirthday(out DateTime birthday);
            Assert.AreEqual(expectedBirthdays[i], birthday, $"ID {id} should have birthday {expectedBirthdays[i]:yyyy-MM-dd}.");
        }
    }

    [TestMethod]
    public void CalculateBirthday_ValidIDs_ShouldReturnCorrectBirthday_DateOnly()
    {
        var expectedBirthdays = new[]
        {
            new DateOnly(2021, 08, 04),
            new DateOnly(2012, 09, 09),
            new DateOnly(1994, 01, 11),
            new DateOnly(2006, 08, 19),
            new DateOnly(1967, 08, 19),
            new DateOnly(2016, 08, 31),
            new DateOnly(1999, 09, 09),
            new DateOnly(1953, 03, 31),
            new DateOnly(1996, 12, 06),
            new DateOnly(1978, 08, 11)
        };
        for (var i = 0; i < ValidIDs.Length; i++)
        {
            var id = ValidIDs[i];
            id.CalculateBirthday(out DateOnly birthday);
            Assert.AreEqual(expectedBirthdays[i], birthday, $"ID {id} should have birthday {expectedBirthdays[i]:yyyy-MM-dd}.");
        }
    }

    [TestMethod]
    public void CalculateGender_ValidIDs_ShouldReturnCorrectGender()
    {
        var expectedGenders = new[]
        {
            EGender.女,
            EGender.男,
            EGender.男,
            EGender.男,
            EGender.女,
            EGender.男,
            EGender.女,
            EGender.男,
            EGender.女,
            EGender.女
        };
        for (var i = 0; i < ValidIDs.Length; i++)
        {
            var id = ValidIDs[i];
            var gender = id.CalculateGender();
            Assert.AreEqual(expectedGenders[i], gender, $"ID {id} should have gender {expectedGenders[i]}.");
        }
    }

    [TestMethod]
    public void CalculateAge_ValidBirthdays_ShouldReturnCorrectAge()
    {
        var birthdays = new[]
        {
            new DateOnly(2021, 08, 04),
            new DateOnly(2012, 09, 09),
            new DateOnly(1994, 01, 11),
            new DateOnly(2006, 08, 19),
            new DateOnly(1967, 08, 19),
            new DateOnly(2016, 08, 31),
            new DateOnly(1999, 09, 09),
            new DateOnly(1953, 03, 31),
            new DateOnly(1996, 12, 06),
            new DateOnly(1978, 08, 11)
        };
        var expectedAges = new[] { 3, 12, 30, 18, 57, 8, 25, 71, 27, 46 };
        for (var i = 0; i < birthdays.Length; i++)
        {
            var birthday = birthdays[i];
            var age = IDCardCalculate.CalculateAge(birthday);
            Assert.AreEqual(expectedAges[i], age, $"Birthday {birthday:yyyy-MM-dd} should have age {expectedAges[i]}.");
        }
    }

    [TestMethod]
    public void CalculateAge_ValidBirthdayDateTimes_ShouldReturnCorrectAge()
    {
        var birthdays = new[]
        {
            new DateTime(2021, 08, 04),
            new DateTime(2012, 09, 09),
            new DateTime(1994, 01, 11),
            new DateTime(2006, 08, 19),
            new DateTime(1967, 08, 19),
            new DateTime(2016, 08, 31),
            new DateTime(1999, 09, 09),
            new DateTime(1953, 03, 31),
            new DateTime(1996, 12, 06),
            new DateTime(1978, 08, 11)
        };
        var expectedAges = new[] { 3, 12, 30, 18, 57, 8, 25, 71, 27, 46 };
        for (var i = 0; i < birthdays.Length; i++)
        {
            var birthday = birthdays[i];
            var age = IDCardCalculate.CalculateAge(birthday);
            Assert.AreEqual(expectedAges[i], age, $"Birthday {birthday:yyyy-MM-dd} should have age {expectedAges[i]}.");
        }
    }
}