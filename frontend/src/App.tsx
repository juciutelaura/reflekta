import { useState } from "react";
import { SignedIn, SignedOut, SignInButton, SignOutButton, useAuth } from "@clerk/clerk-react";
import { createApiClient } from "./lib/apiClient";
import { IntentionPage } from "./pages/IntentionPage";
import { JourneyPage } from "./pages/JourneyPage";
import "./App.css";

export default function App() {
  const { getToken } = useAuth();
  const [journeyId, setJourneyId] = useState<string | null>(null);
  const apiClient = createApiClient(getToken);

  return (
    <div className="shell">
      <header className="topbar">
        <span className="wordmark">Reflekta</span>
        <SignedIn>
          <SignOutButton>
            <button className="btn btn-ghost">Sign out</button>
          </SignOutButton>
        </SignedIn>
      </header>

      <main className="main">
        <SignedOut>
          <div className="stack">
            <div className="intro">
              <h1>Welcome to Reflekta</h1>
              <p>A clearer way to see your own thinking.</p>
            </div>
            <SignInButton mode="modal">
              <button className="btn">Sign in</button>
            </SignInButton>
          </div>
        </SignedOut>

        <SignedIn>
          {journeyId === null ? (
            <IntentionPage apiClient={apiClient} onJourneyStarted={setJourneyId} />
          ) : (
            <JourneyPage apiClient={apiClient} journeyId={journeyId} />
          )}
        </SignedIn>
      </main>
    </div>
  );
}
