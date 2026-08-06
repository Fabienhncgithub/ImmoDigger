import './Pagination.css'

interface PaginationProps {
  page: number
  totalPages: number
  totalCount: number
  onPageChange: (page: number) => void
}

export function Pagination({ page, totalPages, totalCount, onPageChange }: PaginationProps) {
  if (totalPages <= 1) {
    return (
      <div className="pagination">
        <span className="pagination-summary">{totalCount} annonce(s)</span>
      </div>
    )
  }

  return (
    <div className="pagination">
      <span className="pagination-summary">
        Page {page} / {totalPages} - {totalCount} annonce(s)
      </span>
      <div className="pagination-controls">
        <button type="button" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          Precedent
        </button>
        <button type="button" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
          Suivant
        </button>
      </div>
    </div>
  )
}
