import { useState } from "react";
import { SignedIn, SignedOut, SignInButton, useAuth } from "@clerk/clerk-react";
import { createApiClient } from "./lib/apiClient";
import { IntentionPage } from "./pages/IntentionPage";
import { JourneyPage } from "./pages/JourneyPage";

export default function App() {
  const { getToken } = useAuth();
  const [journeyId, setJourneyId] = useState<string | null>(null);
  const apiClient = createApiClient(getToken);

  return (
    <main>
      <SignedOut>
        <SignInButton />
      </SignedOut>
      <SignedIn>
        {journeyId === null ? (
          <IntentionPage apiClient={apiClient} onJourneyStarted={setJourneyId} />
        ) : (
          <JourneyPage apiClient={apiClient} journeyId={journeyId} />
        )}
      </SignedIn>
    </main>
  );
}
