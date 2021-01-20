# JNU nCov2019 Check-in

[English](README.md)

本程序为JNU学生的每日健康打卡提供自动化服务。

## 警告

本程序不是用户友好的程序。如果您不知如何编译或部署本程序请勿使用。


> **注意：请用在安全的机器上，本程序不会加密您输入的密码。**

## 使用方法

1. 安装 .Net Core 3.1 SDK 并编译本程序。

2. 程序会默认使用```./config.json```作为配置文件。如果需要指定配置文件，请使用参数```-c 配置文件```。

3. 直接运行程序将进入交互模式，您可以在此模式下添加用户。

    * 若要添加打卡用户，请输入```add```。
    * 若要进行打卡，请输入```checkin 用户名```或```checkin-all```。
    * 若要退出，请输入```exit```。

4. 使用参数```-a```将进入打卡模式。强烈建议用```crontab```或```任务计划```实现自动打卡。

### Docker

Docker镜像里的程序只能运行在打卡模式，您需要实现准备好配置文件：

```
[
    {
        "Username":"用户名",
        "Password":"密码",
        "Enabled":true
    },
    {
        "Username":"打卡用户名称（自定义）",
        "EncryptedUsername":"加密后的用户名",
        "Enabled":true
    }
]
```

然后就可以挂载配置文件并运行Docker容器（```--rm```参数表示容器运行结束后即删除）：

```
docker run -v /path/to/your/config/file.json:/app/config.json --rm samlangten/jnu-ncov2019-checkin
```

或者直接使用环境变量：

```
docker run -e JNUCHECKIN1_USERNAME=your_username -e JNUCHECKIN1_PASSWORD=your_password -e JNUCHECKIN2_USERNAME=your_username -e JNUCHECKIN2_ENCRYPTED=your_encrypted_username --rm samlangten/jnu-ncov2019-checkin
```