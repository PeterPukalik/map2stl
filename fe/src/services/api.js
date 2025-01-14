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

// Example of other APIs
export const fetchProtectedResource = (token) =>
  apiRequest("/some-protected-endpoint", "GET", null, token);
