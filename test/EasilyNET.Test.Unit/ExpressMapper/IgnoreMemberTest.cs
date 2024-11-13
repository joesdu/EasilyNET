using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Configuration;
using EasilyNET.ExpressMapper.Mapper;
using EasilyNET.Test.Unit.ExpressMapper.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.ExpressMapper;

file class IgnoreUserConfig : MapperConfig<User, UserDto>
{
    protected override void Configure(IMappingConfigure<User, UserDto> configurer)
    {
        configurer
            .IgnoreDest(dto => dto.Description)
            .IgnoreSource(user => user.Age);
    }
}

file class IgnoreToConstructor : MapperConfig<Address, AddressDto>
{
    protected override void Configure(IMappingConfigure<Address, AddressDto> configurer)
    {
        configurer
            .WithConstructor<string, string, string>()
            .IgnoreSource(address => address.City)
            .IgnoreDest(dto => dto.Country);
    }
}

[TestClass]
public class IgnoreMemberTest
{
    [TestMethod]
    public void DestMemberIgnored()
    {
        var mapper = Mapper.Create<IgnoreUserConfig>();
        var user = new User
        {
            Description = "desc"
        };
        var dto = mapper.Map<User, UserDto>(user);
        dto.Description.Should().BeNull();
    }

    [TestMethod]
    public void SourceMemberIgnored()
    {
        var mapper = Mapper.Create<IgnoreUserConfig>();
        var user = new User
        {
            Age = 45
        };
        var dto = mapper.Map<User, UserDto>(user);
        dto.Age.Should().Be(0);
    }
}