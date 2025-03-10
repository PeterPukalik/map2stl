import React, { useState } from "react";
import { forgotPassword } from "../../services/api.js";

function ForgotPassword() {
  const [email, setEmail] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    try {
      await forgotPassword({ email }); // Calls POST /auth/forgotPassword
      alert("Password reset email sent!");
    } catch (error) {
      alert(`Reset failed: ${error.message}`);
    }
  };

  return (
    <form onSubmit={handleSubmit}>
      <input
        type="email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        placeholder="Enter your email"
      />
      <button type="submit">Reset Password</button>
    </form>
  );
}

export default ForgotPassword;
