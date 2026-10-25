FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Worker.Api/RayTracer/RayTracer.csproj Worker.Api/RayTracer/
COPY Worker.Api/Worker.Api/Worker.Api.csproj Worker.Api/Worker.Api/
RUN dotnet restore Worker.Api/Worker.Api/Worker.Api.csproj

COPY Worker.Api/ Worker.Api/
RUN dotnet publish Worker.Api/Worker.Api/Worker.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Worker.Api.dll"]
