import React, { useState } from "react";
// For React Router v6+:
import { useSearchParams } from "react-router-dom";
import { resetPasswordWithToken } from "../../services/api.js";

function ResetPasswordForm() {
  const [searchParams] = useSearchParams();
  const token = searchParams.get("token");

  const [newPassword, setNewPassword] = useState("");
  const [message, setMessage] = useState("");

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!token) {
      setMessage("No token found in the URL.");
      return;
    }

    try {
      const result = await resetPasswordWithToken(token, newPassword);
      setMessage(result.message || "Password updated successfully!");
    } catch (error) {
      setMessage(`Reset failed: ${error.message}`);
    }
  };

  return (
    <div>
      <h2>Reset Your Password</h2>
      {token ? (
        <form onSubmit={handleSubmit}>
          <label>New Password:</label>
          <input
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            placeholder="Enter new password"
          />
          <button type="submit">Set New Password</button>
        </form>
      ) : (
        <p>No token provided in the URL.</p>
      )}

      {message && <p>{message}</p>}
    </div>
  );
}

export default ResetPasswordForm;
