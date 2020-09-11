FROM mcr.microsoft.com/dotnet/core/sdk:3.1 as nakama-dotnet
ARG username=nakama-test-runner

RUN useradd -ms /bin/bash $username

USER $username
COPY --chown=$username . /home/$username/dotnet-sdk
WORKDIR /home/$username/dotnet-sdk
ENTRYPOINT dotnet build && dotnet test
