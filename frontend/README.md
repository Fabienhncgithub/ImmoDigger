# ImmoDigger — Frontend

Interface React (Vite + TypeScript) d'ImmoDigger. Voir le
[README principal](../README.md) pour le contexte du projet, l'architecture
et la configuration.

## Commandes

```bash
npm install     # installer les dépendances
npm run dev     # lancer le serveur de développement
npm run build   # build de production (tsc -b && vite build)
npm run lint    # lint (oxlint)
npm run preview # prévisualiser le build de production
```

## Conventions

- Un composant = un fichier `.tsx` + un fichier `.css` du même nom
  (ex: `PropertyCard.tsx` / `PropertyCard.css`).
- Pas de Tailwind, CSS classique.
- Données serveur via TanStack Query, routage via React Router.
