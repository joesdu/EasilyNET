##### EntityFrameworkCore.Test.Unit

- 本地使用 docker 启动 MSSQL 服务

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=guest" -p 1433:1433 --name sql1 --hostname sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2022-latest
```
