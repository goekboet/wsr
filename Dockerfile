FROM microsoft/dotnet:sdk
WORKDIR /app

# copy csproj and restore as distinct layers
COPY ./src .
RUN dotnet restore App.WSr/App.WSr.csproj
RUN dotnet publish App.WSr/App.WSr.csproj -c Release -o out

EXPOSE 80
ENTRYPOINT ["dotnet", "App.WSr/out/App.WSr.dll"]