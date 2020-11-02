# JNU nCov2019 Check-in

This program is used for JNU Student's Daily Health Check-in for nCov2019.

## Warning

This program isn't user-friendly. Please don't use it if you don't know how to build the project.

> **Notice: use it on your own disk, this program will not encrypt your password.**

## Usage

1. Install .NET Core 3.1 SDK and build.

2. Program runs under Interactive Mode without arguments, which can add bot.

    * To add a new bot, input ```add```.

3. Program runs under Check-in Mode with argument ```-a```. We strongly advice using ```crontab``` or ```Task Scheduler``` for automatic check-in.

### Docker

Docker image can only run under Check-in Mode, a configuration file should be prepared first:

```
[
    {
        "Username":"your username",
        "Password":"your password",
        "Enabled":true
    }
]
```

1. Build docker image:

    ```
    cd ./JNUnCov2019Checkin
    docker build . -t jnu-ncov2019-checkin
    ```

2. Create and run docker image with mounting configuration file ( ```--rm``` means that container will be removed after execution ):

    ```
    docker run -v /path/to/your/config/file.json:/app/config.json --rm jnu-ncov2019-checkin
    ```