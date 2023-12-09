using EasilyNET.Core.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EasilyNET.Migrate.Console.Test.Model;

public class DbSetupUser : IDbSetup
{
    private readonly ILogger<DbSetupUser> _logger;

    private readonly IRepository<User, Guid> _repository;

    public DbSetupUser(ILogger<DbSetupUser> logger, IRepository<User, Guid> repository)
    {
        _logger = logger;
        _repository = repository;
    }

    public Task Init() => SaveAsync();

    private Expression<Func<User, bool>> GetExpression(User user)
    {
        return o => o.Name == user.Name;
    }

    private List<User> GetData()
    {
        var list = new List<User>();
        list.Add(new()
        {
            Name = "大黄瓜",
            Age = 10
        });
        list.Add(new()
        {
            Name = "大黄瓜01",
            Age = 10
        });
        list.Add(new()
        {
            Name = "大黄瓜02",
            Age = 10
        });
        list.Add(new()
        {
            Name = "大黄瓜03",
            Age = 10
        });
        list.Add(new()
        {
            Name = "大黄瓜04",
            Age = 10
        });
        return list;
    }

    private async Task SaveAsync()
    {
        await _repository.UnitOfWork.BeginTransactionAsync();
        foreach (var data in GetData())
        {
            var any = await _repository.FindEntity.AnyAsync(GetExpression(data));
            if (!any)
            {
                await _repository.AddAsync(data);
            }
        }
        var count = await _repository.UnitOfWork.SaveChangesAsync();
        await _repository.UnitOfWork.CommitTransactionAsync();
        _logger?.LogInformation($"迁移成功:{count}");
    }
}

public interface IDbSetup
{
    public Task Init();
}