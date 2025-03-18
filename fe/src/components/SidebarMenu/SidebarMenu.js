import React, { useContext, useState } from "react";
import { AuthContext } from "../../contexts/AuthContext";
import { Link } from "react-router-dom";
import "./SidebarMenu.css";

const SidebarMenu = () => {
  const { user, isAdmin, logout } = useContext(AuthContext);

  // Track whether the menu is open (visible) or closed (hidden)
  const [menuOpen, setMenuOpen] = useState(true);

  const handleBurgerClick = () => {
    setMenuOpen((prev) => !prev);
  };

  return (
    <>
      {/* The "hamburger" button */}
      <button className="hamburger-btn" onClick={handleBurgerClick}>
        {/* You can replace this ☰ with an icon (e.g., from Font Awesome) */}
        ☰
      </button>

      {/* Sidebar container with conditional classes */}
      <div className={`sidebar-menu ${menuOpen ? "open" : "closed"}`}>
        <div className="sidebar-header">
        </div>
        
        <ul>
        {!user ? (
        <>
          <li><Link to="/login">Login</Link></li>
          <li><Link to="/register">Register</Link></li>
          <li><Link to="/forgotpassword">Forgotten Password</Link></li>
        </>
      ) : (
        <>

        <li> 
          <button onClick={() => (window.location.href = "/profile")} >       {user.username}</button>

        </li>

          <li>
            <button onClick={logout}>Logout</button>
          </li>
        </>
      )}
          <li><Link to="/">Home</Link></li>
          <li><Link to="/about">About</Link></li>
          {isAdmin && <li><Link to="/admin">Admin Panel</Link></li>}
        </ul>
      </div>
    </>
  );
};

export default SidebarMenu;
