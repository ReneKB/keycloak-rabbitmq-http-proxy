FROM mcr.microsoft.com/dotnet/sdk:3.1-bullseye AS build-env

WORKDIR /app
COPY . ./
RUN ["dotnet", "restore"]
RUN ["dotnet", "build"]
EXPOSE 5000/tcp
EXPOSE 5001/tcp

ENTRYPOINT ["dotnet", "run", "--launch-profile", "keycloak_rabbitmq_http_proxy"]