using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Configuration;
using EasilyNET.ExpressMapper.Exceptions;
using EasilyNET.ExpressMapper.Mapper;
using EasilyNET.Test.Unit.ExpressMapper.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.ExpressMapper;

file class MappingWithConstructor : MapperConfig<Address, AddressDto>
{
    protected override void Configure(IMappingConfigure<Address, AddressDto> configurer)
    {
        configurer
            .WithConstructor<string, string, string>();
    }
}

file class MappingWithIncorrectConstructorParams : MapperConfig<Address, AddressDto>
{
    protected override void Configure(IMappingConfigure<Address, AddressDto> configurer)
    {
        // non-existing constructor
        configurer.WithConstructor<string, int, DateTime, string>();
    }
}

[TestClass]
public class ConstructorMappingTest
{
    [TestMethod]
    public void MappingConstructorParamsToUpper()
    {
        var mapper = Mapper.Create<MappingWithConstructor>();
        var address = new Address
        {
            City = "city",
            Country = "country",
            Street = "street"
        };
        var addressDto = mapper.Map<Address, AddressDto>(address);
        addressDto.City.Should().Be(address.City);
        addressDto.Country.Should().Be(address.Country);
        addressDto.Street.Should().Be(address.Street);
    }

    [TestMethod]
    public void MappingNonExistingConstructor()
    {
        var mapper = Mapper.Create<MappingWithIncorrectConstructorParams>();
        var address = new Address
        {
            City = "city",
            Country = "country",
            Street = "street"
        };
        Action act = () => mapper.Map<Address, AddressDto>(address);
        act.Should().Throw<CannotFindConstructorWithoutParamsException>();
    }
}