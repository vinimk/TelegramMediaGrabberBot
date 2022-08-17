#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base

RUN set -x && \
    apt update && \
    apt install -y \
        wget \
        python3 \
        python3-pip

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

RUN set -ex && \
    ARCH=`uname -m` && \
    if [ "$ARCH" = "x86_64" ]; then \
        s6_package="s6-overlay-amd64.tar.gz" ; \
    elif [ "$ARCH" = "aarch64" ]; then \
        s6_package="s6-overlay-aarch64.tar.gz" ; \
    else \
        echo "unknown arch: ${ARCH}" && \
        exit 1 ; \
    fi && \
    wget -P /tmp/ https://github.com/just-containers/s6-overlay/releases/download/v2.2.0.3/${s6_package} && \
    tar -xzf /tmp/${s6_package} -C / && \
    rm -rf /tmp/*

RUN set -x && \
    python3 -m pip --no-cache-dir install yt-dlp

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TelegramMediaGrabberBot.csproj", "."]
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