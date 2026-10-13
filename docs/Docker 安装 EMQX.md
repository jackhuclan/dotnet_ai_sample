# Docker 安装 EMQX（开源社区版，推荐）

> 
> 开源版镜像：`emqx/emqx`；企业版：`emqx/emqx-enterprise`，日常测试 / 项目用社区版足够EMQX

## 1. 前提

已经装好 Docker，并且 Docker 服务已启动。

## 2. 快速体验（不持久化，临时测试）

```
# 直接拉取并启动
docker run -d --name emqx \
-p 1883:1883 \
-p 8083:8083 \
-p 8084:8084 \
-p 8883:8883 \
-p 18083:18083 \
emqx/emqx:latest
```

端口说明：

- `1883`：MQTT TCP 端口
- `8083`：MQTT over WebSocket
- `8883`：MQTT TLS
- `18083`：Dashboard 管理后台

访问后台：`http://服务器IP:18083`
默认账号：**admin**，密码：**public**，首次登录强制改密码EMQX

## 3. 生产推荐（挂载数据持久化，容器删除数据不丢）

```
# 创建宿主机存储目录
mkdir -p $PWD/emqx_data $PWD/emqx_log

# 启动带数据挂载
docker run -d --name emqx \
--hostname emqx-node1 \
-e EMQX_NODE_NAME=emqx@emqx-node1 \
-p 1883:1883 \
-p 8083:8083 \
-p 8084:8084 \
-p 8883:8883 \
-p 18083:18083 \
-v $PWD/emqx_data:/opt/emqx/data \
-v $PWD/emqx_log:/opt/emqx/log \
emqx/emqx:latest
```

> 
> `-v` 挂载 data 和 log，升级 / 重建容器配置、客户端数据不会丢失EMQX

## 4. 常用管理命令

```
# 查看容器状态
docker ps |grep emqx

# 查看日志
docker logs -f emqx

# 进入容器内部
docker exec -it emqx sh

# 停止
docker stop emqx

# 重启
docker restart emqx

# 删除容器（数据还在宿主机目录）
docker rm emqx
```

## 5. docker-compose.yml 方式（推荐长期使用）

新建 `docker-compose.yml`

```
version: '3'
services:
  emqx:
    image: emqx/emqx:latest
    container_name: emqx
    hostname: emqx-node1
    environment:
      - EMQX_NODE_NAME=emqx@emqx-node1
    ports:
      - "1883:1883"
      - "8083:8083"
      - "8084:8084"
      - "8883:8883"
      - "18083:18083"
    volumes:
      - ./emqx_data:/opt/emqx/data
      - ./emqx_log:/opt/emqx/log
    restart: always
```

启动：

```
docker-compose up -d
```

## 6. 常见坑

1. **云服务器**：安全组放行 `1883`、`18083` 端口，否则外网访问不了
2. **端口占用**：如果宿主机 1883 被占用，改成 `-p 1884:1883`
3. **密码**：默认密码 `public`，公网部署务必第一时间修改
4. **时间同步**：容器时间和宿主机不一致会导致证书 / 连接异常

## 7. 测试 MQTT 连通性

安装 mqttx 客户端，连接地址 `mqtt://ip:1883`，发布 / 订阅消息验证服务正常