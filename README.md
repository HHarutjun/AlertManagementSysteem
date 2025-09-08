# VanDijk Alert Management

## Introductie
Dit project automatiseert het monitoren van logs en het versturen van alerts naar verantwoordelijken. Dit gebeurd aan de hand van strategieën die gedifineerd zijn. Alerts worden verstuurd via e-mail (FlowMailer) en er kunnen automatisch Azure DevOps work items worden aangemaakt.

## Getting Started

### Installatie & Vereisten
- .NET 8.0 SDK
- Visual Studio 2022 of nieuwer
- Toegang tot Azure/AWS logs (credentials in `appsettings.json` of UserSecrets)
- FlowMailer API credentials
- Azure DevOps Personal Access Token

### Configuratie
1. Zet gevoelige gegevens zoals tokens in UserSecrets of als environment variables op de volgende manier:
dotnet user-secrets set "Azure:ClientId" "jouw-client-id"
dotnet user-secrets set "Azure:ClientSecret" "jouw-client-secret"
dotnet user-secrets set "AlertSettings:FlowMailer:ClientId" "jouw-flowmailer-client-id"
dotnet user-secrets set "AlertSettings:FlowMailer:ClientSecret" "jouw-flowmailer-client-secret"
dotnet user-secrets set "AlertSettings:Teams:ClientId" "jouw-teams-client-id"
dotnet user-secrets set "AlertSettings:Teams:ClientSecret" "jouw-teams-client-secret"
dotnet user-secrets set "AlertSettings:Smtp:Pass" "jouw-echte-wachtwoord"

### Builden & Runnen
```bash
cd Presentation
dotnet build
dotnet run
```
Om de webapp te runnen:
```bash
cd Presentation
dotnet build
dotnet run --web
```


## Testen
Ga eerst naar de `Tests` map:
```bash
cd Tests
dotnet test --collect:"XPlat Code Coverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByAttribute="ExcludeFromCodeCoverage" -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Exclude="[Infrastructure*]*,[*]TaskCreator*,[*]SprintService*"
```

Voor een uitgebreide motivatie waarom bepaalde klassen uitgesloten zijn van code coverage, zie hoofdstuk 1.1. van het [testplan](https://luminiseu.atlassian.net/wiki/spaces/AF/pages/593854546/Testplan+Alertmanagamentsysteem).

## Bijdragen
- Fork de repository en maak een feature branch.
- Maak een pull request met duidelijke omschrijving.
- Voeg waar mogelijk tests toe.

## Meer informatie
Zie [Software Architecture Document](https://luminiseu.atlassian.net/wiki/spaces/AF/pages/499810427/Software+Architecture+Document+Harutjun), [Software Design Description](https://luminiseu.atlassian.net/wiki/spaces/AF/pages/499646599/Software+Design+Description+Harutjun) en [Software Requirement Specification](https://luminiseu.atlassian.net/wiki/spaces/AF/pages/499613838/Software+Requirement+*Specification*+Harutjun) voor meer details over de architectuur, en het [Plan van Aanpak](https://luminiseu.atlassian.net/wiki/spaces/AF/pages/494764076/Plan+van+Aanpak+Alermanagement) op meer informatie op het project.
