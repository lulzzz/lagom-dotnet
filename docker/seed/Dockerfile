FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

COPY ./example/*.csproj ./example/
COPY ./src/wyvern.api/*.csproj ./src/wyvern.api/
COPY ./src/wyvern.entity/*.csproj ./src/wyvern.entity/
COPY ./src/wyvern.utils/*.csproj ./src/wyvern.utils/


RUN cd ./example/ && \
    dotnet restore

COPY ./example ./example/
COPY ./src ./src/

RUN cd ./example && \
    dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/example/out .
ENTRYPOINT ["dotnet", "wyvern.api.hello.dll"]
