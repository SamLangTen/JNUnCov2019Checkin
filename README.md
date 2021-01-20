# JNU nCov2019 Check-in

[简体中文](README.zh-CN.md)

This program is used for JNU Student's Daily Health Check-in for nCov2019.

## Warning

This program isn't user-friendly. Please don't use it if you don't know how to build the project.

> **Notice: use it on your own disk, this program will not encrypt your password.**

## Usage

1. Install .NET Core 3.1 SDK and build.

2. Program will load ```./config.json``` as configurations. If another config file is needed, specified by argument ```-c config_file_path```.

3. Program runs under Interactive Mode without arguments, which can add bot.

    * To add a new bot, input ```add```.
    * To do check-in, input ```checkin #username``` or ```checkin-all```.
    * To exit, input ```exit```.

4. Program runs under Check-in Mode with argument ```-a```. We strongly advice using ```crontab``` or ```Task Scheduler``` for automatic check-in.

### Docker

Docker image can only run under Check-in Mode, a configuration file should be prepared first:

```
[
    {
        "Username":"your username",
        "Password":"your password",
        "Enabled":true
    },
    {
        "Username":"your bot name",
        "EncryptedUsername":"your encrypted username",
        "Enabled":true
    }
]
```

Create and run docker image with mounting configuration file ( ```--rm``` means that container will be removed after execution ):

```
docker run -v /path/to/your/config/file.json:/app/config.json --rm samlangten/jnu-ncov2019-checkin
```

or use environment variables without mounting:

```
docker run -e JNUCHECKIN1_USERNAME=your_username -e JNUCHECKIN1_PASSWORD=your_password -e JNUCHECKIN2_USERNAME=your_username -e JNUCHECKIN2_ENCRYPTED=your_encrypted_username --rm samlangten/jnu-ncov2019-checkin
```