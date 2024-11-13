using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Expressions;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.ExpressMapper;

[TestClass]
public class MemberSearchTest
{
    private readonly ParameterExpression _param = Expression.Parameter(typeof(User));

    [TestMethod]
    public void CorrectFormProp()
    {
        var expression = MemberSearchHelper.FormMemberAccess(_param, nameof(User.Name), typeof(string));
        expression.Should().NotBeNull();
        expression.Member.Should().Be(typeof(User).GetProperty(nameof(User.Name)));
    }

    [TestMethod]
    public void CorrectFormField()
    {
        var expression = MemberSearchHelper.FormMemberAccess(_param, nameof(User.Description), typeof(string));
        expression.Should().NotBeNull();
        expression.Member.Should().Be(typeof(User).GetField(nameof(User.Description)));
    }

    [TestMethod]
    public void CorrectFindWithIncorrectType()
    {
        var expression = MemberSearchHelper.FormMemberAccess(_param, nameof(User.Description), typeof(int));
        expression.Should().BeNull();
    }

    [TestMethod]
    public void CannotFindUnexistingMember()
    {
        var expression = MemberSearchHelper.FormMemberAccess(_param, "AbraKadabra", typeof(string));
        expression.Should().BeNull();
    }

    private class User
    {
        //field
#pragma warning disable CS0649 // 从未对字段赋值，字段将一直保持其默认值
        public required string Description;
#pragma warning restore CS0649 // 从未对字段赋值，字段将一直保持其默认值

        //prop
        public required string Name { get; set; }
    }
}