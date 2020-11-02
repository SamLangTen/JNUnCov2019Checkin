
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS builder
LABEL maintainer="samlangten@outlook.com"
COPY . /src/Obsidian
WORKDIR /src/Obsidian
RUN dotnet restore && \ 
    dotnet publish --output /app/ --configuration Release

FROM mcr.microsoft.com/dotnet/core/runtime:3.1
LABEL maintainer="samlangten@outlook.com"
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "JNUnCov2019Checkin.dll", "-a"]