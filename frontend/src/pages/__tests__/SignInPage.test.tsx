import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { describe, it, expect, vi, beforeEach } from "vitest";
import { useClerk, useSignIn } from "@clerk/clerk-react";
import { SignInPage } from "../SignInPage";

vi.mock("@clerk/clerk-react", () => ({
  useSignIn: vi.fn(),
  useClerk: vi.fn(),
}));

describe("SignInPage", () => {
  const create = vi.fn();
  const authenticateWithRedirect = vi.fn();
  const setActive = vi.fn();
  const openSignIn = vi.fn();
  const openSignUp = vi.fn();

  beforeEach(() => {
    create.mockReset();
    authenticateWithRedirect.mockReset();
    setActive.mockReset();
    openSignIn.mockReset();
    openSignUp.mockReset();

    vi.mocked(useSignIn).mockReturnValue({
      isLoaded: true,
      signIn: { create, authenticateWithRedirect } as never,
      setActive,
    } as never);
    vi.mocked(useClerk).mockReturnValue({ openSignIn, openSignUp } as never);
  });

  it("signs in with email and password and activates the session on success", async () => {
    create.mockResolvedValue({ status: "complete", createdSessionId: "sess_1" });

    render(<SignInPage />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "user@example.com" } });
    fireEvent.change(screen.getByLabelText(/^password$/i), { target: { value: "correct-password" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() =>
      expect(create).toHaveBeenCalledWith({
        strategy: "password",
        identifier: "user@example.com",
        password: "correct-password",
      }),
    );
    await waitFor(() => expect(setActive).toHaveBeenCalledWith({ session: "sess_1" }));
  });

  it("shows the error Clerk returns when sign-in fails", async () => {
    create.mockRejectedValue({ errors: [{ longMessage: "Email address is not enabled for this application." }] });

    render(<SignInPage />);

    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "user@example.com" } });
    fireEvent.change(screen.getByLabelText(/^password$/i), { target: { value: "correct-password" } });
    fireEvent.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() =>
      expect(screen.getByRole("alert")).toHaveTextContent("Email address is not enabled for this application."),
    );
    expect(setActive).not.toHaveBeenCalled();
  });

  it("toggles password visibility", () => {
    render(<SignInPage />);

    const passwordInput = screen.getByLabelText(/^password$/i);
    expect(passwordInput).toHaveAttribute("type", "password");

    fireEvent.click(screen.getByRole("button", { name: /show password/i }));
    expect(passwordInput).toHaveAttribute("type", "text");
  });

  it("starts the Google OAuth redirect flow", () => {
    render(<SignInPage />);

    fireEvent.click(screen.getByRole("button", { name: /continue with google/i }));

    expect(authenticateWithRedirect).toHaveBeenCalledWith({
      strategy: "oauth_google",
      redirectUrl: "/sso-callback",
      redirectUrlComplete: "/",
    });
  });

  it("opens Clerk's sign-up modal from the create-account link", () => {
    render(<SignInPage />);

    fireEvent.click(screen.getByRole("button", { name: /create an account/i }));

    expect(openSignUp).toHaveBeenCalled();
  });
});
