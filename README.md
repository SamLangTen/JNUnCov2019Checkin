# JNU nCov2019 Check-in

This program is used for JNU Student's Daily Health Check-in for COVID19.

## Warning

This program isn't user-friendly. Please don't use it if you don't know how to build the project.

> **Notice: use it on your own disk, this program will not encrypt your password.**

## Usage

1. Install .NET Core 3.1 SDK and build.

2. The program will load ```./config.json``` as configurations. If another config file is needed, specified by argument ```-c config_file_path```.

3. The program runs under Interactive Mode without arguments, which can add bot.

    * To add a new bot, input ```add```.
    * To do check-in, input ```checkin #username``` or ```checkin-all```.
    * To exit, input ```exit```.

4. The program runs under Check-in Mode with argument ```-a```. We strongly advice using ```crontab``` or ```Task Scheduler``` for automatic check-in.

### Docker

Docker image can only run under Check-in Mode, a configuration file should be prepared first:

```
"CheckinInterval": 0,
"GlobalUserAgent": "python-requests",
"RetryTimes": 0,
"ValidationHelper": "SMSValidationHelper"
"Configs": [
    {
        "Username":"your username",
        "Password":"your password",
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
docker run \
-e JNUCHECKIN_INTERVAL=0 \
-e JNUCHECKIN_RETRY=0 \
-e JNUCHECKIN_USERAGENT=python-requests \
-e JNUCHECKIN_VALIDATION_HELPER=SMSValidationHelper \
-e JNUCHECKIN1_USERNAME=your_username \
-e JNUCHECKIN1_PASSWORD=your_password \
-e JNUCHECKIN2_USERNAME=your_username \
-e JNUCHECKIN2_ENCRYPTED=your_encrypted_username \
--rm samlangten/jnu-ncov2019-checkin
```

## Validation Helper

JNU Student's Daily Health Check-in for COVID19 is protected by Netease Yidun Captcha System. Therefore, a validation helper that assists this program to fetch correct validation code is needed.
This program provides AkarinValidationHelper, which can fetch validation code from a special API implemented [Akarin's solution](https://github.com/SO-JNU/stuhealth-validator).
