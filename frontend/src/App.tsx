import { useState } from "react";
import { Route, Routes } from "react-router-dom";
import {
  AuthenticateWithRedirectCallback,
  SignedIn,
  SignedOut,
  SignOutButton,
  useAuth,
} from "@clerk/clerk-react";
import { createApiClient } from "./lib/apiClient";
import { IntentionPage } from "./pages/IntentionPage";
import { JourneyPage } from "./pages/JourneyPage";
import { SignInPage } from "./pages/SignInPage";
import "./App.css";

function MainApp() {
  const { getToken } = useAuth();
  const [journeyId, setJourneyId] = useState<string | null>(null);
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
            <SignOutButton>
              <button className="btn btn-ghost">Sign out</button>
            </SignOutButton>
          </header>

          <main className="main">
            {journeyId === null ? (
              <IntentionPage apiClient={apiClient} onJourneyStarted={setJourneyId} />
            ) : (
              <JourneyPage apiClient={apiClient} journeyId={journeyId} />
            )}
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
