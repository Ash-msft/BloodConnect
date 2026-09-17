import { Navigate, Route, Routes } from 'react-router-dom'
import { Layout } from './components/Layout'
import { useTeamsContext } from './context/useTeamsContext'
import { DashboardPage } from './pages/DashboardPage'
import { ProfilePage } from './pages/ProfilePage'
import { CreateRequestPage } from './pages/CreateRequestPage'
import { RequestsPage } from './pages/RequestsPage'
import { RequestDetailPage } from './pages/RequestDetailPage'
import { MatchesPage } from './pages/MatchesPage'
import { NotificationsPage } from './pages/NotificationsPage'

export function App() {
  const teamsHostInfo = useTeamsContext()

  return (
    <Routes>
      <Route path="/" element={<Layout teamsHostInfo={teamsHostInfo} />}>
        <Route index element={<DashboardPage />} />
        <Route path="profile" element={<ProfilePage />} />
        <Route path="requests" element={<RequestsPage />} />
        <Route path="requests/new" element={<CreateRequestPage />} />
        <Route path="requests/:id" element={<RequestDetailPage />} />
        <Route path="matches" element={<MatchesPage />} />
        <Route path="notifications" element={<NotificationsPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  )
}
