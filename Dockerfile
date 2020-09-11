FROM mcr.microsoft.com/dotnet/core/sdk:3.1
ARG username=nakama-dotnet-user

RUN useradd -ms /bin/bash $username

USER $username
COPY --chown=$username . /home/$username/dotnet-sdk
WORKDIR /home/$username/dotnet-sdk
RUN dotnet build
ENTRYPOINT dotnet test
