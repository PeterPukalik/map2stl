import React from "react";
import { BrowserRouter as Router, Route, Routes } from "react-router-dom";
import SidebarMenu from "./components/SidebarMenu/SidebarMenu";
import Login from "./components/Auth/Login";
import Register from "./components/Auth/Register";
import { AuthProvider } from "./contexts/AuthContext";
import MapWithDraw from './components/MapComponent/MapComponent';
import Admin from "./components/Admin/Admin";
import './App.css';
import Profile from "./components/Profile/Profile";

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app-container">
          <div className="sidebar">
            <SidebarMenu />
          </div>
          <div className="main-content">
            <h1>Map2stl</h1>
            <Routes>
              <Route path="/" element={<MapWithDraw />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route path="/admin" element={<Admin />} />
              <Route path="/profile" element={<Profile />} /> 
            </Routes>
          </div>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
