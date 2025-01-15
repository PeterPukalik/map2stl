import React, { useContext } from "react";
import { Navigate } from "react-router-dom";
import { AuthContext } from "../../contexts/AuthContext";

const AdminRoute = ({ children }) => {
  const { isAdmin } = useContext(AuthContext);

  return isAdmin ? children : <Navigate to="/login" />;
};

export default AdminRoute;
