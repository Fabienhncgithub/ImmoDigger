import { QueryClientProvider } from '@tanstack/react-query'
import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { queryClient } from './api/queryClient'
import { Layout } from './components/Layout/Layout'
import { DashboardPage } from './pages/Dashboard/DashboardPage'

/**
 * Root component: wires up global providers (React Query, Router) and
 * the top-level route table. Additional routes (Listings, Listing detail,
 * Search profiles, Sources) are added in a later commit alongside their
 * pages.
 */
function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <Layout>
          <Routes>
            <Route path="/" element={<DashboardPage />} />
          </Routes>
        </Layout>
      </BrowserRouter>
    </QueryClientProvider>
  )
}

export default App
