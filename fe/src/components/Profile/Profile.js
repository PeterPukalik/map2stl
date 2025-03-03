import React, { useState, useEffect, useContext } from "react";
import { fetchUserProfile,resetOwnPassword,fetchUserModels } from "../../services/api.js";
import { AuthContext } from "../../contexts/AuthContext";

const Profile = () => {
  const { user, logout,setUser } = useContext(AuthContext); // Access the user and logout function from the context
  const [newPassword, setNewPassword] = useState("");
  const [models, setModels] = useState([]);
  const [message, setMessage] = useState("");
  

  useEffect(() => {
const fetchProfile = async () => {
      try {
        // const token = localStorage.getItem("token");
        // const response = await fetchUserProfile("/profile", "GET", null, token);
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
        // const token = localStorage.getItem("token");
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
      // const token = localStorage.getItem("token");
      console.log(newPassword)
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
    <div>
      <h2>Profile</h2>
      <p>Username: {user.username}</p>
      <p>Email: {user.email}</p>

      <h3>Reset Password</h3>
      <form onSubmit={handlePasswordReset}>
        <input
          type="password"
          placeholder="New Password"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
        />
        <button type="submit">Reset Password</button>
      </form>

      {message && <p>{message}</p>}

      <h3>Your Models</h3>
      {models.length > 0 ? (
        <ul>
          {models.map((model) => (
            <li key={model.id}>
              <strong>{model.name}</strong>: {model.description}
            </li>
          ))}
        </ul>
      ) : (
        <p>No models found.</p>
      )}

      <button onClick={logout}>Logout</button>
    </div>
  );
};

export default Profile;
