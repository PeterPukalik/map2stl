import React, { useContext } from "react";
import { AuthContext } from "../../contexts/AuthContext";
import { Link } from "react-router-dom";

const SidebarMenu = () => {
  const { user, isAdmin, logout } = useContext(AuthContext);

  return (
    <div>
      <h3>Menu</h3>
      <ul>
        {!user ? (
          <>
            <li><Link to="/login">Login</Link></li>
            <li><Link to="/register">Register</Link></li>
          </>
        ) : (
          <>
             <li>Logged in as: <button onClick={() => window.location.href = "/profile"}>{user.username}</button></li>
            <li><button onClick={logout}>Logout</button></li>
          </>
        )}
        <li><Link to="/">Home</Link></li>
        <li><Link to="/about">About</Link></li>
        {isAdmin && (
          <li><Link to="/admin">Admin Panel</Link></li>
        )}
      </ul>
    </div>
  );
};

export default SidebarMenu;
