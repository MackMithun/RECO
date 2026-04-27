#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

ENV ASPNETCORE_URLS=http://*:8080

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app
COPY . .
RUN dotnet restore "./RECO.Reaccommodation_MS.csproj"
COPY . .

RUN dotnet build "./RECO.Reaccommodation_MS.csproj" -c Release -o /app/build
RUN chmod -R 777 ./Configs/PNRDetails.xlsx
RUN chmod -R 777 ./Configs/NonActionPNR.xlsx       
FROM build AS publish
RUN dotnet publish "./RECO.Reaccommodation_MS.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN ls
RUN chmod -R 777 ./Configs
RUN chmod -R 777 ./Configs/PNRDetails.xlsx
RUN chmod -R 777 ./Configs/NonActionPNR.xlsx  
ENTRYPOINT ["dotnet", "./RECO.Reaccommodation_MS.dll"]
#RECO_Bussiness_MS/RECO_Reaccommodation_MS/RECO.Reaccommodation_MS.csproj

