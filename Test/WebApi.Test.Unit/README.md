##### WebApi.Test.Unit

WebApi 相关库的测试单元

- 本地使用docker启动MongoDB服务
```bash
docker run --name mongo -p 27018:27017 -d --rm -it -e MONGO_INITDB_ROOT_USERNAME=oneblogs -e MONGO_INITDB_ROOT_PASSWORD="test789" mongo:latest
```

- 本地使用docker启动RabbitMQ服务并添加延时队列
```bash
# 启动RabbitMQ服务
docker run --name rabbitmq -p 5672:5672 -p 15672:15672 -d --rm -it -e RABBITMQ_DEFAULT_USER=guest -e RABBITMQ_DEFAULT_PASS=guest rabbitmq:management
# 下载延时插件,下载rabbitmq_delayed_message_exchange-*.ez插件并上传到指定文件夹中,[下载地址](https://www.rabbitmq.com/community-plugins.html)
# 将延时插件拷贝到容器中,其中插件路径根据实际情况替换
docker cp "C:\Users\Niu\Downloads\rabbitmq_delayed_message_exchange-3.11.1.ez" rabbitmq:/plugins
# 进入容器并启动插件
docker exec -it rabbitmq /bin/bash
rabbitmq-plugins enable rabbitmq_delayed_message_exchange
```