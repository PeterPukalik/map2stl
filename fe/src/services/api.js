const BASE_URL = "https://localhost:7188"; 

// Generic helper function for API requests
const apiRequest = async (endpoint, method = "GET", body = null, token = null) => {
  const headers = { "Content-Type": "application/json" };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const response = await fetch(`${BASE_URL}${endpoint}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : null,
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message || "Something went wrong");
  }

  return response.json();
};

// Auth APIs
export const registerUser = (userData) => apiRequest("/Auth/register", "POST", userData);
export const loginUser = (credentials) => apiRequest("/Auth/login", "POST", credentials);

// Generate Terrain Model API
export const generateTerrainGif = (boundingBox) =>
  apiRequest("/Model/generate", "POST", boundingBox);

// Example of other APIs
export const fetchProtectedResource = (token) =>
  apiRequest("/some-protected-endpoint", "GET", null, token);

// Fetch all users
// export const fetchAllUsers = (token) =>
// apiRequest("/Admin/users", "GET", null, token);
export const fetchAllUsers = async () => {
  const token = localStorage.getItem("token");
  return apiRequest("/Admin/users", "GET", null, token);
};
// Reset a user's password
export const resetUserPassword = (userId) => {
  const token = localStorage.getItem("token"); 
  return apiRequest(`/Admin/resetPassword/${userId}`, "POST", null, token); 
};
// Fetch the logged-in user's profile information
export const fetchUserProfile = () => {
  const token = localStorage.getItem("token");
  return apiRequest("/profile", "GET", null, token);
};

// Reset the logged-in user's password
export const resetOwnPassword = (newPassword) => {
  const token = localStorage.getItem("token");
  return apiRequest("/profile/resetPassword", "POST", { newPassword }, token);
};

// Fetch all models linked to the logged-in user
export const fetchUserModels = () => {
  const token = localStorage.getItem("token");
  return apiRequest("/Model/userModels", "GET", null, token);
};



