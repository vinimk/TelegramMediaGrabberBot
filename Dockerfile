#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

RUN set -x && \
    apt update && \
    apt install -y \
        wget \
        python3 \
        python3-pip

RUN set -x && \
    wget 'https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp' -P /tmp/yt-dlp/ && \
    chmod -R a+x /tmp/yt-dlp/* && \
    mv /tmp/yt-dlp/yt-dlp /usr/local/bin/ && \
    rm -rf /tmp/*

RUN set -x && \
    ARCH=`uname -m` && \
    if [ "$ARCH" = "x86_64" ]; then \
        wget -q 'https://github.com/yt-dlp/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-linux64-gpl.tar.xz' -O - | tar -xJ -C /tmp/ --one-top-level=ffmpeg && \
        chmod -R a+x /tmp/ffmpeg/* && \
        mv $(find /tmp/ffmpeg/* -name ffmpeg) /usr/local/bin/ && \
        mv $(find /tmp/ffmpeg/* -name ffprobe) /usr/local/bin/ && \
        mv $(find /tmp/ffmpeg/* -name ffplay) /usr/local/bin/ && \
        rm -rf /tmp/* ; \
    else \
        if [ "$ARCH" = "aarch64" ]; then ARCH='arm64'; fi && \
        wget -q "https://johnvansickle.com/ffmpeg/builds/ffmpeg-git-${ARCH}-static.tar.xz" -O - | tar -xJ -C /tmp/ --one-top-level=ffmpeg && \
        chmod -R a+x /tmp/ffmpeg/* && \
        mv $(find /tmp/ffmpeg/* -name ffmpeg) /usr/local/bin/ && \
        mv $(find /tmp/ffmpeg/* -name ffprobe) /usr/local/bin/ && \
        rm -rf /tmp/* ; \
    fi

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TelegramMediaGrabberBot.csproj", "."]
COPY ["nuget.config", "."]
RUN dotnet restore "./TelegramMediaGrabberBot.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "TelegramMediaGrabberBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TelegramMediaGrabberBot.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TelegramMediaGrabberBot.dll"]