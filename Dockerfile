FROM microsoft/dotnet:2.0-sdk
WORKDIR /app

# copy csproj and restore as distinct layers
COPY . .
RUN dotnet restore
RUN dotnet publish src/App.WSr/App.WSr.csproj -c Release -o out

EXPOSE 80
ENTRYPOINT ["dotnet", "src/App.WSr/out/App.WSr.dll"]