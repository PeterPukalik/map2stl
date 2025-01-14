// // src/components/SidebarMenu/SidebarMenu.js
// import React from 'react';
// import './SidebarMenu.css';

// const SidebarMenu = () => {
//   return (
//     <nav className="sidebar-menu">
//       <h3>Menu</h3>
//       <ul>
//         <li>Home</li>
//         <li>Register</li>
//         <li>Sign in</li>
//         <li>About</li>
//       </ul>
//     </nav>
//   );
// };

// export default SidebarMenu;
import { Link } from "react-router-dom";
import React, { useContext } from "react";
import { AuthContext } from "../../contexts/AuthContext";
import './SidebarMenu.css';

const SidebarMenu = () => {
  const { user, logout } = useContext(AuthContext);

  return (
    <div className="sidebar-menu">
      <h1>Menu</h1>
      {user ? (
        <>
          <p>Logged in as: {user.username}</p>
          <button onClick={logout}>Sign Out</button>
        </>
      ) : (
        <>
          <p>You are not logged in.</p>
          <Link to="/login">Login</Link>
          <br></br>
          <Link to="/register">Register</Link>
        </>
      )}
        <ul>
        <li>Home</li>
        <li>Register</li>
        <li>Sign in</li>
        <li>About</li>
      </ul>
    </div>
  );
};

export default SidebarMenu;
