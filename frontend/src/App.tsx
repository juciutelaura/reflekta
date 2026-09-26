import { Link, Navigate, Route, Routes, useNavigate, useParams } from "react-router-dom";
import {
  AuthenticateWithRedirectCallback,
  SignedIn,
  SignedOut,
  SignOutButton,
  useAuth,
} from "@clerk/clerk-react";
import { createApiClient, type ApiClient } from "./lib/apiClient";
import { IntentionPage } from "./pages/IntentionPage";
import { JourneyPage } from "./pages/JourneyPage";
import { SignInPage } from "./pages/SignInPage";
import { HistoryPage } from "./pages/HistoryPage";
import { JourneyDetailPage } from "./pages/JourneyDetailPage";
import "./App.css";

function NewIntentionRoute({ apiClient }: { apiClient: ApiClient }) {
  const navigate = useNavigate();
  return <IntentionPage apiClient={apiClient} onJourneyStarted={(id) => navigate(`/journey/${id}`)} />;
}

function ActiveJourneyRoute({ apiClient }: { apiClient: ApiClient }) {
  const { journeyId } = useParams<{ journeyId: string }>();
  const navigate = useNavigate();
  if (!journeyId) return <Navigate to="/" replace />;
  return (
    <JourneyPage
      apiClient={apiClient}
      journeyId={journeyId}
      onJourneyCompleted={() => navigate(`/history/${journeyId}`)}
    />
  );
}

function HistoryDetailRoute({ apiClient }: { apiClient: ApiClient }) {
  const { journeyId } = useParams<{ journeyId: string }>();
  if (!journeyId) return <Navigate to="/history" replace />;
  return <JourneyDetailPage apiClient={apiClient} journeyId={journeyId} />;
}

function MainApp() {
  const { getToken } = useAuth();
  const apiClient = createApiClient(getToken);

  return (
    <>
      <SignedOut>
        <SignInPage />
      </SignedOut>

      <SignedIn>
        <div className="shell">
          <header className="topbar">
            <span className="wordmark">Reflekta</span>
            <nav className="nav-links">
              <Link to="/history" className="link-btn">
                History
              </Link>
              <SignOutButton>
                <button className="btn btn-ghost btn-sm">Sign out</button>
              </SignOutButton>
            </nav>
          </header>

          <main className="main">
            <Routes>
              <Route path="/" element={<NewIntentionRoute apiClient={apiClient} />} />
              <Route path="/journey/:journeyId" element={<ActiveJourneyRoute apiClient={apiClient} />} />
              <Route path="/history" element={<HistoryPage apiClient={apiClient} />} />
              <Route path="/history/:journeyId" element={<HistoryDetailRoute apiClient={apiClient} />} />
            </Routes>
          </main>
        </div>
      </SignedIn>
    </>
  );
}

export default function App() {
  return (
    <Routes>
      <Route path="/sso-callback" element={<AuthenticateWithRedirectCallback />} />
      <Route path="*" element={<MainApp />} />
    </Routes>
  );
}
