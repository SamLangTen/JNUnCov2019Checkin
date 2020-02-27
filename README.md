# JNU nCov2019 Check-in

This program is used for JNU Student's Daily Health Check-in for nCov2019.

## Warning

This program isn't user-friendly. Please don't use it if you don't know the meaning of ```mainTable``` or ```encrypted password```.

> **Notice: use it on your own disk, this program will not encrypt your key or password.**

## Check-in Mode

This program supports Automatic Check-in Mode and Manual Check-in Mode.

* Username, plain password and encryption key are needed in Automatic Mode. Please use Manual Mode if you have no idea how to find encryption key.

    > We strongly advice using one-time-Automatic Mode, which username, plain password and key should be provided and plain password will be encrypted and saved locally. After that, program runs in Manual Mode.

* Username, encrypted password instead of plain password are need in Manual Mode.

## Usage

1. Install .NET Core 3.1 SDK and build.

2. Program runs under Interactive Mode without arguments, which can add bot.

    * To add a new bot, input ```add```. Notice that ```mainTable content``` should be saved in text file and its path should be provided.

3. Program runs under Check-in Mode with argument ```-a```. We strongly advice using ```crontab``` or ```Task Scheduler``` for automatic check-in.
