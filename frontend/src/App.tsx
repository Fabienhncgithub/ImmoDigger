import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { queryClient } from './api/queryClient'
import { Layout } from './components/Layout/Layout'
import { DashboardPage } from './pages/Dashboard/DashboardPage'
import { ListingsPage } from './pages/Listings/ListingsPage'
import { ListingDetailPage } from './pages/ListingDetail/ListingDetailPage'
import { SearchProfilesPage } from './pages/SearchProfiles/SearchProfilesPage'
import { SourcesPage } from './pages/Sources/SourcesPage'

/** Root component: wires up global providers (React Query, Router) and the top-level route table. */
function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<DashboardPage />} />
            <Route path="/listings" element={<ListingsPage />} />
            <Route path="/listings/:id" element={<ListingDetailPage />} />
            <Route path="/search-profiles" element={<SearchProfilesPage />} />
            <Route path="/sources" element={<SourcesPage />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
