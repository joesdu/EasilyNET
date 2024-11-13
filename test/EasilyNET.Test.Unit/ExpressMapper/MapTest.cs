using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Configuration;
using EasilyNET.ExpressMapper.Mapper;
using EasilyNET.Test.Unit.ExpressMapper.Entities;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EasilyNET.Test.Unit.ExpressMapper;

file class ProductMapConfig : MapperConfig<Product, ProductDto>
{
    protected override void Configure(IMappingConfigure<Product, ProductDto> configurer)
    {
        configurer
            .Map(dto => dto.Id, _ => Guid.NewGuid())
            .Map(dto => dto.Name, product => product.ProductName);
    }
}

[TestClass]
public class MapTest
{
    [TestMethod]
    public void CorrectMappingValue()
    {
        var mapper = Mapper.Create<ProductMapConfig>();
        var product = new Product
        {
            ProductName = "name"
        };
        var dto = mapper.Map<Product, ProductDto>(product);
        dto.Id.Should().NotBe(Guid.Empty);
    }

    [TestMethod]
    public void CorrectMappingSourceMember()
    {
        var mapper = Mapper.Create<ProductMapConfig>();
        var product = new Product
        {
            ProductName = "name"
        };
        var dto = mapper.Map<Product, ProductDto>(product);
        dto.Name.Should().Be(product.ProductName);
    }
}