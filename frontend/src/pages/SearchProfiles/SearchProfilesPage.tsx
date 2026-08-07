import { useState } from 'react'
import { Link } from 'react-router-dom'
import {
  useCreateSearchProfile,
  useDeleteSearchProfile,
  useSearchProfiles,
  useUpdateSearchProfile,
} from '../../hooks/useSearchProfiles'
import { SearchProfileForm } from './SearchProfileForm'
import { formatPercent, formatPrice } from '../../utils/format'
import { searchProfileToListingFilters } from '../../utils/searchProfileFilters'
import type { SearchProfile, SearchProfileRequest } from '../../types'
import './SearchProfilesPage.css'

export function SearchProfilesPage() {
  const { data: profiles, isLoading } = useSearchProfiles()
  const createMutation = useCreateSearchProfile()
  const updateMutation = useUpdateSearchProfile()
  const deleteMutation = useDeleteSearchProfile()

  const [isCreating, setIsCreating] = useState(false)
  const [editingId, setEditingId] = useState<string | null>(null)

  function handleCreate(request: SearchProfileRequest) {
    createMutation.mutate(request, { onSuccess: () => setIsCreating(false) })
  }

  function handleUpdate(id: string, request: SearchProfileRequest) {
    updateMutation.mutate({ id, request }, { onSuccess: () => setEditingId(null) })
  }

  return (
    <section className="search-profiles-page">
      <div className="search-profiles-header">
        <h1>Profils de recherche</h1>
        {!isCreating && (
          <button type="button" className="search-profiles-add-button" onClick={() => setIsCreating(true)}>
            + Nouveau profil
          </button>
        )}
      </div>

      {isCreating && (
        <SearchProfileForm
          onSubmit={handleCreate}
          onCancel={() => setIsCreating(false)}
          isSubmitting={createMutation.isPending}
        />
      )}

      {isLoading && <p className="search-profiles-status">Chargement...</p>}
      {profiles && profiles.length === 0 && !isCreating && (
        <p className="search-profiles-status">Aucun profil de recherche pour le moment.</p>
      )}

      <div className="search-profiles-list">
        {profiles?.map((profile) =>
          editingId === profile.id ? (
            <SearchProfileForm
              key={profile.id}
              initial={profile}
              onSubmit={(request) => handleUpdate(profile.id, request)}
              onCancel={() => setEditingId(null)}
              isSubmitting={updateMutation.isPending}
            />
          ) : (
            <ProfileCard
              key={profile.id}
              profile={profile}
              onEdit={() => setEditingId(profile.id)}
              onDelete={() => deleteMutation.mutate(profile.id)}
            />
          ),
        )}
      </div>
    </section>
  )
}

function ProfileCard({
  profile,
  onEdit,
  onDelete,
}: {
  profile: SearchProfile
  onEdit: () => void
  onDelete: () => void
}) {
  return (
    <div className={`search-profile-card ${profile.isEnabled ? '' : 'search-profile-card--disabled'}`}>
      <div className="search-profile-card-header">
        <h2>{profile.name}</h2>
        <span className="search-profile-card-status">{profile.isEnabled ? 'Actif' : 'Inactif'}</span>
      </div>

      <dl className="search-profile-card-facts">
        <div>
          <dt>Prix maximum</dt>
          <dd>{formatPrice(profile.maximumPrice)}</dd>
        </div>
        <div>
          <dt>Rendement minimum</dt>
          <dd>{formatPercent(profile.minimumGrossYield)}</dd>
        </div>
        <div>
          <dt>Logements minimum</dt>
          <dd>{profile.minimumUnitCount ?? '—'}</dd>
        </div>
        <div>
          <dt>Score minimum</dt>
          <dd>{profile.minimumOpportunityScore ?? '—'}</dd>
        </div>
      </dl>

      {profile.postalCodes.length > 0 && (
        <p className="search-profile-card-tags">Communes : {profile.postalCodes.join(', ')}</p>
      )}

      <Link
        className="search-profile-card-matches"
        to="/listings"
        state={{ filters: searchProfileToListingFilters(profile) }}
      >
        {profile.matchingListingsCount} annonce{profile.matchingListingsCount !== 1 ? 's' : ''} correspondante
        {profile.matchingListingsCount !== 1 ? 's' : ''} →
      </Link>

      <div className="search-profile-card-actions">
        <button type="button" onClick={onEdit}>
          Modifier
        </button>
        <button type="button" className="search-profile-card-delete" onClick={onDelete}>
          Supprimer
        </button>
      </div>
    </div>
  )
}
