import React, { createContext, useState, useEffect } from "react";
import { jwtDecode } from "jwt-decode";


export const AuthContext = createContext();

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(null);
  const [isAdmin, setIsAdmin] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem("token");
    if (token) {
      try {
        const decodedToken = jwtDecode(token);
        const currentTime = Date.now() / 1000;
        if (decodedToken.exp && decodedToken.exp < currentTime) {
          console.warn("Token has expired");
          localStorage.removeItem("token");
          setUser(null);
          setIsAdmin(false);
        } else {
          setUser(decodedToken);
          setIsAdmin(decodedToken.role === "Admin");
        }
      } catch (err) {
        console.error("Failed to decode token:", err);
        setUser(null);
        setIsAdmin(false);
      }
    }
  }, []);

  const login = (token) => {
    localStorage.setItem("token", token);
    const decodedToken = jwtDecode(token);
    setUser(decodedToken);
    setIsAdmin(decodedToken.role === "Admin");
  };

  const logout = () => {
    localStorage.removeItem("token");
    setUser(null);
    setIsAdmin(false);
  };

  return (
    <AuthContext.Provider value={{ user, setUser, isAdmin, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
};
