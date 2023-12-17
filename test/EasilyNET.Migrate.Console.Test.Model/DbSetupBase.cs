using EasilyNET.Core.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace EasilyNET.Migrate.Console.Test.Model;

public class DbSetupUser(ILogger<DbSetupUser> logger, IRepository<User, Guid> repository) : IDbSetup
{
    public Task Init() => SaveAsync();

    private static Expression<Func<User, bool>> GetExpression(User user) => o => o.Name == user.Name;

    private static List<User> GetData()
    {
        var list = new List<User>
        {
            new()
            {
                Name = "大黄瓜",
                Age = 10
            },
            new()
            {
                Name = "大黄瓜01",
                Age = 10
            },
            new()
            {
                Name = "大黄瓜02",
                Age = 10
            },
            new()
            {
                Name = "大黄瓜03",
                Age = 10
            },
            new()
            {
                Name = "大黄瓜04",
                Age = 10
            }
        };
        return list;
    }

    private async Task SaveAsync()
    {
        await repository.UnitOfWork.BeginTransactionAsync();
        foreach (var data in GetData())
        {
            var any = await repository.FindEntity.AnyAsync(GetExpression(data));
            if (!any)
            {
                await repository.AddAsync(data);
            }
        }
        var count = await repository.UnitOfWork.SaveChangesAsync();
        await repository.UnitOfWork.CommitTransactionAsync();
        logger?.LogInformation("迁移成功:{count}", count);
    }
}

public interface IDbSetup
{
    public Task Init();
}