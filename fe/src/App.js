// src/App.js
import React from 'react';
import './App.css';
import SidebarMenu from './components/SidebarMenu/SidebarMenu';
import MapWithDraw from './components/MapComponent/MapComponent';


function App() {
  return (
    <div className="app-container">
      <div className="sidebar">
        <SidebarMenu />
      </div>
      <div className="main-content">
        <h1>My First React App</h1>
        <MapWithDraw />
      </div>
    </div>
  );
}

export default App;
