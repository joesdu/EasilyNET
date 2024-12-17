##### WebApi.Test.Unit

WebApi 相关库的测试单元

-   本地使用 docker 启动 MongoDB 服务单节点

```bash
docker run --name mongo1 -p 27017:27017 -d --rm -it -e MONGO_INITDB_ROOT_USERNAME=guest -e MONGO_INITDB_ROOT_PASSWORD="guest" mongo:latest
```

-   本地使用 docker 启动 MSSQL 服务

```bash
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=guest" -p 1433:1433 --name sql1 --hostname sql1 --hostname sql1 -d mcr.microsoft.com/mssql/server:2022-latest
```

-   本地使用 docker 启动 RabbitMQ 服务并添加延时队列
-   启动 RabbitMQ 服务,该镜像包含 delayed_message_exchange 插件

```bash
docker run --name rabbitmq -p 5672:5672 -p 15672:15672 -d --rm -it -e RABBITMQ_DEFAULT_USER=admin -e RABBITMQ_DEFAULT_PASS=a123456 ghcr.io/joesdu/rabbitmq-dlx:latest
```

-   本地使用 docker 启动 Minio 服务

```bash
docker run -d --restart always  -p 9000:9000 -p 9090:9090 --name minio -v F:\data:/data -e "MINIO_ROOT_USER=admin" -e "MINIO_ROOT_PASSWORD=admin123456" minio/minio server /data --console-address ":9090"
```

#### OpenSSL 证书申请

-   [下载 openssl 安装包](http://slproweb.com/products/Win32OpenSSL.html)并安装,推荐下载最新 64 位版本.
-   打开命令行,输入 openssl,如果提示 Openssl 不是内部或外部命令,需要设置一下环境变量,把 Openssl 的安装目录加入到 path
    环境变量.
-   另外新建一个环境变量,如以下所示,名称为：OPENSSL_CONF,指向你安装目录的 openssl.cfg 文件,现在输入 openssl 应该没有问题了.

-   新建一个文件夹用于放置密钥,在该目录打开命令行.

-   1.申请一个私钥,在命令行中输入:

```shell
openssl genrsa -out private_ids.key 2048
```

-   申请一个 2048 位的 RSA 加密私钥.目录下将多了一个名为 private_ids.key 的文件.

-   2.申请一个公钥

```shell
openssl req -new -x509 -key private_ids.key -days 3650 -out public_ids.crt
```

-   其中 -key private_ids.key 是指定这个公钥的配对私钥,就是第一步申请的私钥.x509 是 X.509 公钥格式标准.

-   接下来会提示你输入一些信息,用于颁发机构的信息展示,如公司,所在国家,城市等

```conf
You are about to be asked to enter information that will be incorporated
into your certificate request.
What you are about to enter is what is called a Distinguished Name or a DN.
There are quite a few fields but you can leave some blank
For some fields there will be a default value,
If you enter '.', the field will be left blank.
-----
Country Name (2 letter code) [AU]:CN
State or Province Name (full name) [Some-State]:ShangHai
Locality Name (eg, city) []:ShangHai
Organization Name (eg, company) [Internet Widgits Pty Ltd]:XXXXX
Organizational Unit Name (eg, section) []:XXXXX-XXXXX
Common Name (e.g. server FQDN or YOUR name) []:XXXXX
Email Address []:XXXXX@outlook.COM
```

-   如果不想每次都输入这些信息,可以使用“-config 配置文件目录”的方式指定配置文件,安装后 Openssl 后,有一个名为 openssl.cnf
    的默认的配置文件在安装目录 bin/cnf 目录中.编辑该文件,找到 req_distinguished_name

```conf
[ req_distinguished_name ]
countryName         = Country Name (2 letter code)
countryName_default     = AU
countryName_min         = 2
countryName_max         = 2

stateOrProvinceName     = State or Province Name (full name)
stateOrProvinceName_default = Some-State

localityName            = Locality Name (eg, city)

0.organizationName      = Organization Name (eg, company)
0.organizationName_default  = Internet Widgits Pty Ltd

# we can do this but it is not needed normally :-)
#1.organizationName     = Second Organization Name (eg, company)
#1.organizationName_default = World Wide Web Pty Ltd

organizationalUnitName      = Organizational Unit Name (eg, section)
#organizationalUnitName_default =

commonName          = Common Name (e.g. server FQDN or YOUR name)
commonName_max          = 64

emailAddress            = Email Address
emailAddress_max        = 64
```

-   这里可以指定这些参数的默认值,如指定国家默认值为 CN.把 countryName_default 改成 CN 就行了.申请完公钥后,目录下将多了一个
    public_ids.crt 的文件.

-   3.公钥及私钥的提取加密.由于传播安全方面的考虑,需要将公钥及私钥加密,微软支持 PCK12(公钥加密技术 12 号标准：Public Key
    Cryptography Standards #12),PCK12 将公钥和私钥合在一个 PFX 后缀文件并用密码保护,如要提取公钥和私钥需要密码确认.另一种觉的公钥私钥提取加密方式是
    JKS(JAVA Key Store)用于 JAVA 环境的公钥和私钥提取.这两种格式可以相互转换.

-   在命令行中输入

```shell
openssl pkcs12 -export -in public_ids.crt -inkey private_ids.key -out ids.pfx
```

-   输入密码和确认密码后,当前目录会多出一个文件:ids.pfx.这就是我们要用的密钥证书了.

##### 使用密钥

-   将第二步生成 ids.pfx 文件复制服务器发布目录,如果只是为了测试,可以复制到本地项目目录.将目录信息及证书提取密码存入配置文件

```csharp
if (Environment.IsDevelopment())
{
    builder.AddDeveloperSigningCredential();
}
else
{
    builder.AddSigningCredential(new X509Certificate2(Path.Combine(Environment.CurrentDirectory, "ids.pfx"), "your_password"));
}
```

如果想调试环境也统一证书,可以把环境判断去掉,只用 AddSigningCredential 方式加载密钥证书.
