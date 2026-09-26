import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, it, expect, vi } from "vitest";
import App from "../App";

vi.mock("@clerk/clerk-react", () => ({
  SignedIn: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignedOut: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignOutButton: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  AuthenticateWithRedirectCallback: () => null,
  useAuth: () => ({ getToken: vi.fn() }),
  useSignIn: () => ({
    isLoaded: true,
    signIn: { create: vi.fn(), authenticateWithRedirect: vi.fn() },
    setActive: vi.fn(),
  }),
  useClerk: () => ({ openSignIn: vi.fn(), openSignUp: vi.fn() }),
}));

describe("App", () => {
  it("renders the wordmark, a history link, and both auth controls on the root route", () => {
    render(
      <MemoryRouter initialEntries={["/"]}>
        <App />
      </MemoryRouter>,
    );

    expect(screen.getAllByText("Reflekta").length).toBeGreaterThan(0);
    expect(screen.getByRole("link", { name: /history/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign out/i })).toBeInTheDocument();
  });
});
