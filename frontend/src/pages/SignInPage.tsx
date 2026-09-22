import { useState } from "react";
import { useClerk, useSignIn } from "@clerk/clerk-react";
import { AppleIcon, GoogleIcon } from "./AuthIcons";
import authIllustration from "../assets/auth-illustration.png";
import "./SignInPage.css";

function clerkErrorMessage(err: unknown): string {
  if (err && typeof err === "object" && "errors" in err) {
    const errors = (err as { errors?: Array<{ longMessage?: string; message?: string }> }).errors;
    const first = errors?.[0];
    if (first?.longMessage) return first.longMessage;
    if (first?.message) return first.message;
  }
  return "Something went wrong. Please try again.";
}

export function SignInPage() {
  const { isLoaded, signIn, setActive } = useSignIn();
  const clerk = useClerk();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!isLoaded) return;
    setError(null);
    setIsSubmitting(true);
    try {
      const result = await signIn.create({ strategy: "password", identifier: email, password });
      if (result.status === "complete") {
        await setActive({ session: result.createdSessionId });
      } else {
        setError("Additional verification is required to finish signing in.");
      }
    } catch (err) {
      setError(clerkErrorMessage(err));
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleOAuth(strategy: "oauth_google" | "oauth_apple") {
    if (!isLoaded) return;
    setError(null);
    try {
      await signIn.authenticateWithRedirect({
        strategy,
        redirectUrl: "/sso-callback",
        redirectUrlComplete: "/",
      });
    } catch (err) {
      setError(clerkErrorMessage(err));
    }
  }

  return (
    <div className="auth">
      <div className="auth-left">
        <h1 className="auth-headline">Take a flip</h1>
        <hr className="auth-rule" />
        <p className="auth-subhead">
          A clearer way
          <br />
          to see your own
          <br />
          thinking.
        </p>

        <img
          className="auth-illustration"
          src={authIllustration}
          alt="A looping ribbon, twisted like a Möbius strip"
        />

        <p className="auth-footer">
          Same thoughts.
          <br />
          A different
          <br />
          perspective.
        </p>
      </div>

      <div className="auth-divider-line" aria-hidden="true" />

      <div className="auth-right">
        <form className="auth-form" onSubmit={handleSubmit}>
          <div className="auth-field">
            <label className="auth-field-label" htmlFor="auth-email">
              Email
            </label>
            <div className="auth-input-row">
              <input
                id="auth-email"
                type="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </div>
          </div>

          <div className="auth-field">
            <label className="auth-field-label" htmlFor="auth-password">
              Password
            </label>
            <div className="auth-input-row">
              <input
                id="auth-password"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
              <button
                type="button"
                className="auth-toggle"
                onClick={() => setShowPassword((value) => !value)}
              >
                {showPassword ? "Hide password" : "Show password"}
              </button>
            </div>
          </div>

          {error && (
            <p className="auth-error" role="alert">
              {error}
            </p>
          )}

          <button type="submit" className="auth-submit" disabled={isSubmitting}>
            Sign in
          </button>
        </form>

        <div className="auth-links">
          <button type="button" className="auth-link" onClick={() => clerk.openSignIn()}>
            Forgot password?
          </button>
          <button type="button" className="auth-link" onClick={() => clerk.openSignUp()}>
            Create an account
          </button>
        </div>

        <div className="auth-oauth-divider">
          <span>or continue with</span>
        </div>

        <div className="auth-oauth-stack">
          <button type="button" className="auth-oauth" onClick={() => handleOAuth("oauth_google")}>
            <GoogleIcon />
            <span>Continue with Google</span>
          </button>
          <button type="button" className="auth-oauth" onClick={() => handleOAuth("oauth_apple")}>
            <AppleIcon />
            <span>Continue with Apple</span>
          </button>
        </div>
      </div>
    </div>
  );
}
