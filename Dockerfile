FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY UserManagementApp.csproj ./
RUN dotnet restore UserManagementApp.csproj

COPY . ./
RUN dotnet publish UserManagementApp.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "UserManagementApp.dll"]
