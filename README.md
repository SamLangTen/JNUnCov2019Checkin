# JNU nCov2019 Check-in

This program is used for JNU Student's Daily Health Check-in for nCov2019.

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

### Get Encrypted Username

If an encrypted username is provided, the program can do check-in without original username and password, which is more secure.

You can use the following bookmarklet to get encrypted username:

1. Visit Daily Health Check-in System of JNU and add it as bookmarklet.

2. Edit the bookmarklet and replace address with the following codes:

```
javascript:(function(){alert('Your encrypted username:\n'+sessionStorage.getItem('jnuid'));})();
```

3. Login Daily Health Check-in System of JNU.

4. Click on bookmarklet and you can get your encrypted username.

#### Why is encrypted username more secure?
JNU Daily Health Check-in System (hereinafter called Stuhealth) do check-in with encrypted username (hereinafter called jnuid) calculated by original username, which means jnuid will not change with password. If jnuid or the original username and password leaks, others can permanently login Stuhealth even if user change their password. However, the leaked jnuid can only be used for Stuhealth. The leaked original username and password can not only be used for Stuhealth, but also be accessed for other online service of JNU.


### Docker

Docker image can only run under Check-in Mode, a configuration file should be prepared first:

```
"CheckinInterval": 0,
"GlobalUserAgent": "python-requests",
"RetryTimes": 0,
"Configs": [
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
docker run \
-e JNUCHECKIN_INTERVAL=0 \
-e JNUCHECKIN_RETRY=0 \
-e JNUCHECKIN_USERAGENT=python-requests \
-e JNUCHECKIN1_USERNAME=your_username \
-e JNUCHECKIN1_PASSWORD=your_password \
-e JNUCHECKIN2_USERNAME=your_username \
-e JNUCHECKIN2_ENCRYPTED=your_encrypted_username \
--rm samlangten/jnu-ncov2019-checkin
```
