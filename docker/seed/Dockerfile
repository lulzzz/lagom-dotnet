FROM mcr.microsoft.com/dotnet/core/sdk:2.1-alpine AS build-env
WORKDIR /app

COPY ./example/*.csproj ./example/
COPY ./src/wyvern.api/*.csproj ./src/wyvern.api/
COPY ./src/wyvern.entity/*.csproj ./src/wyvern.entity/
COPY ./src/wyvern.utils/*.csproj ./src/wyvern.utils/
COPY ./src/wyvern.visualize/*.csproj ./src/wyvern.visualize/

RUN cd ./example/ && \
    dotnet restore

COPY ./example ./example/
COPY ./src ./src/

RUN cd ./example && \
    dotnet publish -o out

COPY ./docker/seed/akka.overrides.conf ./example/out/

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/runtime:2.1-alpine
WORKDIR /app
COPY --from=build-env /app/example/out .
ENTRYPOINT ["dotnet", "wyvern.api.hello.dll"]
