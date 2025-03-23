import React, { useState } from "react";
import { forgotPassword } from "../../services/api.js";
import "./AuthForms.css"; // <-- Import the shared CSS

function ForgotPassword() {
  const [email, setEmail] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      await forgotPassword({ email });
      alert("Password reset email sent!");
    } catch (error) {
      alert(`Reset failed: ${error.message}`);
    }
  };

  return (
    <div className="auth-form-container">
      <h2>Forgot Password</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Enter your email"
        />
        <button type="submit">Reset Password</button>
      </form>
    </div>
  );
}

export default ForgotPassword;
