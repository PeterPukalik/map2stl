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
export const forgotPassword = (email) => apiRequest("/Auth/forgotPassword", "POST", email);

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
  return apiRequest(`/Auth/resetPassword/${userId}`, "POST", null, token); 
};
// Fetch the logged-in user's profile information
export const fetchUserProfile = () => {
  const token = localStorage.getItem("token");
  return apiRequest("/profile/getProfile", "GET", null, token);
};

// Reset the logged-in user's password
export const resetOwnPassword = (newPassword) => {
  const token = localStorage.getItem("token");
  console.log("Sending new password:", newPassword); // Debug log
  return apiRequest("/profile/resetPassword", "POST", { newPassword }, token);
};

// Fetch all models linked to the logged-in user
export const fetchUserModels = () => {
  const token = localStorage.getItem("token");
  return apiRequest("/Model/userModels", "GET", null, token);
};
//reset password with token form
export async function resetPasswordWithToken(token, newPassword) {
  return await apiRequest("/auth/resetPasswordWithToken", "POST", { token, newPassword });
}

// load model
export const generateTerrainModel = async (bounds) => {
  const token = localStorage.getItem("token") || "";
  const response = await apiRequest("/Model/generateGltf", "POST", bounds, token);
  
  return `${BASE_URL}${response.fileUrl}`; 
};

