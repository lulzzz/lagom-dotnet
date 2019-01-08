FROM microsoft/dotnet:sdk AS build-env
WORKDIR /app

COPY ./examples/api.tutorial/*.csproj ./examples/api.tutorial/
COPY ./src/wyvern.api/*.csproj ./src/wyvern.api/
COPY ./src/wyvern.entity/*.csproj ./src/wyvern.entity/
COPY ./src/wyvern.utils/*.csproj ./src/wyvern.utils/

RUN cd ./examples/api.tutorial/ && \
    dotnet restore

COPY ./examples ./examples/
COPY ./src ./src/

RUN cd ./examples/api.tutorial/ && \
    dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY --from=build-env /app/examples/api.tutorial/out .
ENTRYPOINT ["dotnet", "api.tutorial.dll"]
