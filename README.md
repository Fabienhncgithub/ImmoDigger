# ImmoDigger

ImmoDigger est une application personnelle qui surveille les nouvelles
annonces d'immeubles de rapport et d'immeubles à appartements mis en vente à
Bruxelles et dans les communes proches. Elle collecte régulièrement des
annonces depuis plusieurs sources, détecte les biens jamais vus, écarte les
doublons, calcule des indicateurs d'investissement (rendement brut estimé,
score d'opportunité, niveau de risque) et notifie l'utilisateur lorsqu'un
bien intéressant apparaît.

Le projet est conçu pour un usage personnel, avec une architecture assez
propre pour pouvoir évoluer vers un SaaS plus tard.

> **Statut** : projet en construction, développé commit par commit. Ce
> README sera complété au fur et à mesure (voir la feuille de route
> ci-dessous). La documentation complète (Docker, Telegram, mode démo,
> ajout d'un collecteur, limites légales...) sera ajoutée avec la mise en
> place du déploiement Docker.

## Architecture

Monorepo composé d'un backend .NET (Clean Architecture simplifiée) et d'un
frontend React.

```text
ImmoDigger/
  backend/
    ImmoDigger.Domain/          entités et règles métier
    ImmoDigger.Application/     cas d'usage, DTO, interfaces, services
    ImmoDigger.Infrastructure/  base de données, scrapers, notifications
    ImmoDigger.Api/             contrôleurs, configuration
    ImmoDigger.Tests/           tests xUnit (backend)
  frontend/                     interface React (Vite + TypeScript)
  docker-compose.yml            (ajouté ultérieurement)
  .env.example
```

Chaque couche backend ne dépend que des couches "en dessous" d'elle :
`Api` → `Infrastructure` → `Application` → `Domain`. Le `Domain` ne dépend
de rien d'autre.

## Stack technique

**Backend** : ASP.NET Core Web API (.NET 10), Entity Framework Core,
PostgreSQL, `BackgroundService` pour les collectes planifiées,
`HttpClientFactory`, Playwright (uniquement si nécessaire), Serilog,
FluentValidation, Swagger/OpenAPI, xUnit, Docker.

**Frontend** : React, Vite, TypeScript, React Router, TanStack Query, CSS
classique (un fichier `.css` par composant, portant le même nom). Pas de
Tailwind.

## Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) ≥ 20 et npm
- PostgreSQL (local ou via Docker, ajouté dans une étape ultérieure)
- Docker (optionnel pour l'instant, requis à partir du déploiement conteneurisé)

## Lancement local

### Backend

```bash
cd backend
dotnet build
dotnet test
dotnet run --project ImmoDigger.Api
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Configuration

Les secrets ne sont jamais commités. En développement, utiliser les
[user-secrets .NET](https://learn.microsoft.com/aspnet/core/security/app-secrets)
ou des variables d'environnement ; voir [.env.example](.env.example) pour
la liste des variables attendues (connexion PostgreSQL, jeton Telegram,
identifiants SMTP, URL de l'API pour le frontend).

## Feuille de route (commits)

1. ✅ Initialisation du monorepo (solution .NET, projets, frontend, structure)
2. ✅ Modèle de données `PropertyListing` et persistance PostgreSQL
3. ✅ Framework de collecte (`IListingCollector`, `BackgroundService`)
4. ✅ Détection de doublons et historique des prix
5. ✅ Analyse d'investissement et score de risque
6. ✅ API REST (annonces, profils de recherche, sources)
7. ⏸️ Notifications Telegram (reporté à la demande de l'utilisateur)
8. ✅ Tableau de bord React
9. Couverture de tests complète
10. Déploiement Docker et documentation complète

Réalisé hors feuille de route initiale : refonte de l'ingestion en
pipeline multi-source conforme (voir section suivante), collecteurs réels
Biddit et Régie des Bâtiments/Défense.

## Pipeline de collecte conforme

ImmoDigger ne scrape jamais un site qui l'interdit explicitement ou le
bloque techniquement. Plutôt que de traiter "scraper ou renoncer" comme un
choix binaire, la collecte est organisée en plusieurs sources, chacune
n'utilisant que des moyens autorisés :

```text
Alertes email (Immoweb, Immovlan, Zimmo, agences)
        ↓
Sites/API publics vétés (Biddit, Régie des Bâtiments/Défense)
        ↓
Import manuel par URL (POST /api/import/url)
        ↓
Déduplication → Analyse d'investissement → Score
```

- **Alertes email** (`IEmailListingImporter`, `IEmailListingParser`) :
  Immoweb, Immovlan et Zimmo ne sont jamais scrapés directement (CGU
  explicites pour Immoweb, WAF/Cloudflare actifs pour les deux autres) ;
  ils sont classés `ExternalAlertSource` et leurs annonces n'arrivent que
  via les emails d'alerte que l'utilisateur reçoit déjà. Une agence
  immobilière s'ajoute en enregistrant une nouvelle instance
  `AgencyEmailParser` (domaine expéditeur + forme d'URL), pas un nouveau
  fichier. Le raccordement à une vraie boîte mail (IMAP ou webhook) reste
  à faire : `IEmailInbox` n'a qu'une implémentation `NullEmailInbox` (no-op)
  pour l'instant.
- **Sites/API publics vétés** (`IListingCollector`) : Biddit et la Régie
  des Bâtiments (qui gère aussi la vente des anciens sites Défense), après
  vérification de `robots.txt`, des CGU et de l'absence de protection
  anti-bot. Voir `ReferenceDataSeeder` pour la décision et sa justification
  par source (`CollectionMethod`, `Allowed`, `Notes`).
- **Import manuel par URL** (`POST /api/import/url`) : l'utilisateur colle
  l'URL d'une annonce qu'il regarde ; l'app lit uniquement les métadonnées
  Open Graph publiques de la page, ou accepte une saisie manuelle si la
  page ne peut pas être récupérée.
- **Extension navigateur** (TODO, non développée) : un bouton "Ajouter à
  ImmoDigger" sur les pages d'annonces consultées par l'utilisateur,
  transmettant les informations déjà visibles à l'écran vers son instance
  personnelle. Idée retenue pour une étape ultérieure.

## Limites légales et techniques de la collecte

ImmoDigger ne contourne jamais un CAPTCHA, une authentification, une
limitation technique, une protection anti-bot (WAF, Cloudflare) ou une
interdiction explicite des sites collectés. Chaque source déclare
explicitement sa méthode de collecte (`CollectionMethod`) et si elle est
`Allowed` ; ce dernier champ est un verrou de conformité central
(`ListingCollectionBackgroundService`), indépendant du simple
activé/désactivé (`IsEnabled`). Chaque source restant scrapable est
désactivable, interrogée à fréquence raisonnable, avec un délai entre les
requêtes et un User-Agent identifiant clairement l'application.
