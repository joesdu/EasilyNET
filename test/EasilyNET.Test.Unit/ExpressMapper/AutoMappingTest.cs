using EasilyNET.ExpressMapper.Mapper;
using EasilyNET.Test.Unit.ExpressMapper.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.ExpressMapper;

[TestClass]
public class AutoMappingTest
{
    [TestMethod]
    public void AutoMappingByName()
    {
        var mapper = Mapper.Create();
        var user = new User
        {
            Description = "desc",
            Age = 45
        };
        var dto = mapper.Map<User, UserDto>(user);
        dto.Description.Should().Be(user.Description);
        dto.Age.Should().Be(user.Age);
    }

    [TestMethod]
    public void CannotMapDestPropertyWithoutSetter()
    {
        var mapper = Mapper.Create();
        var user = new User
        {
            Name = "name"
        };
        var dto = mapper.Map<User, UserDto>(user);
        dto.Name.Should().BeNull();
    }

    [TestMethod]
    public void CannotMapDestReadonlyField()
    {
        var mapper = Mapper.Create();
        var user = new User
        {
            AnotherDescription = "another"
        };
        var dto = mapper.Map<User, UserDto>(user);
        dto.AnotherDescription.Should().BeNull();
    }
}