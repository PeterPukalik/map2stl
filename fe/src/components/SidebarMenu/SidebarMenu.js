// src/components/SidebarMenu/SidebarMenu.js
import React from 'react';
import './SidebarMenu.css';

const SidebarMenu = () => {
  return (
    <nav className="sidebar-menu">
      <h3>Menu</h3>
      <ul>
        <li>Home</li>
        <li>Register</li>
        <li>Sign in</li>
        <li>About</li>
      </ul>
    </nav>
  );
};

export default SidebarMenu;
