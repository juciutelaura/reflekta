import { render, screen } from "@testing-library/react";
import { describe, it, expect, vi } from "vitest";
import App from "../App";

vi.mock("@clerk/clerk-react", () => ({
  SignedIn: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignedOut: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignInButton: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  SignOutButton: ({ children }: { children: React.ReactNode }) => <>{children}</>,
  useAuth: () => ({ getToken: vi.fn() }),
}));

describe("App", () => {
  it("renders the wordmark and both the sign-in and sign-out controls", () => {
    render(<App />);

    expect(screen.getByText("Reflekta")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /sign out/i })).toBeInTheDocument();
  });
});
