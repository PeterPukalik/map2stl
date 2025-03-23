import React, { useState, useEffect, useContext } from "react";
import { fetchUserProfile, resetOwnPassword, fetchUserModels } from "../../services/api.js";
import { AuthContext } from "../../contexts/AuthContext";
import UserModelList from "./UserModelList";
import "./Profile.css";  // Import the CSS file

const Profile = () => {
  const { user, logout, setUser } = useContext(AuthContext);
  const [newPassword, setNewPassword] = useState("");
  const [models, setModels] = useState([]);
  const [message, setMessage] = useState("");

  useEffect(() => {
    const fetchProfile = async () => {
      try {
        const response = await fetchUserProfile();
        setUser((prevUser) => ({
          ...prevUser,
          email: response.email,
        }));
      } catch (error) {
        console.error("Failed to fetch profile:", error);
        setMessage("Failed to load profile information.");
      }
    };

    const fetchModels = async () => {
      try {
        const modelsResponse = await fetchUserModels();
        setModels(modelsResponse || []);
        console.log(modelsResponse);
      } catch (error) {
        console.error("Failed to fetch models:", error);
        setMessage("Failed to load models.");
      }
    };

    fetchProfile();
    fetchModels();
  }, [setUser]);

  const handlePasswordReset = async (e) => {
    e.preventDefault();
    try {
      console.log(newPassword);
      await resetOwnPassword(newPassword);
      setMessage("Password updated successfully!");
    } catch (error) {
      console.error("Failed to reset password:", error);
      setMessage("Failed to reset password.");
    }
  };

  if (!user) {
    return <p>Please log in to view your profile.</p>;
  }

  return (
    <div className="profile-container">
      <div className="profile-header">
        <h2>Profile</h2>
      </div>
      <div className="profile-details">
        <p>Username: {user.username}</p>
        <p>Email: {user.email}</p>
      </div>

      <div className="reset-form">
        <h3>Reset Password</h3>
        <form onSubmit={handlePasswordReset}>
          <label>New Password:</label>
          <input
            type="password"
            placeholder="New Password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
          />
          <button type="submit">Reset Password</button>
        </form>
      </div>

      {message && <p>{message}</p>}

      <div className="profile-models">
        <h3>Your Models</h3>
        {models.length > 0 ? (
          <ul>
            <UserModelList />
          </ul>
        ) : (
          <p>No models found.</p>
        )}
      </div>

      <button className="logout-button" onClick={logout}>
        Logout
      </button>
    </div>
  );
};

export default Profile;
