# LabelFlow

Application professionnelle d'impression d'étiquettes.

**by AikFlow**

## Prérequis

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Accès SQL Server (base type Sage 100c)

## Lancement

```bash
git clone https://github.com/Fiainana/LabelFlow.git
cd LabelFlow
dotnet restore
dotnet run
```

## Architecture

```
Models/     Article, ConnectionConfig, PrintItem
Services/   SQL, config, barcodes, rendu & impression
Views/      Splash, Main, Settings, Print prep & preview
```

## Fonctionnalités

- Connexion SQL (Windows / SQL Auth) chiffrée DPAPI
- Liste articles + scroll 50 + recherche multi-critères
- Sélection persistante, quantités, aperçu / impression
- 4 designs d'étiquettes, Code 128 / 39, devise Ar
