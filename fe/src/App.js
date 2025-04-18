import React from "react";
import { BrowserRouter as Router, Route, Routes } from "react-router-dom";
import SidebarMenu from "./components/SidebarMenu/SidebarMenu";
import Login from "./components/Auth/Login";
import Register from "./components/Auth/Register";
import { AuthProvider } from "./contexts/AuthContext";
import MapWithDraw from "./components/MapComponents/MapComponent";
import Admin from "./components/Admin/Admin";
import "./App.css";
import Profile from "./components/Profile/Profile";
import ModelDebugPage from "./pages/ModelDebugPage"; 
import HomePage from "./pages/HomePage";
import ResetPasswordForm from "./components/Auth/ResetPasswordForm";
import ForgotPassword from "./components/Auth/ForgotPassword";
import LayerPage from "./pages/LayerPage";

function App() {
  return (
    <AuthProvider>
      <Router>
        <div className="app-container">
          <div className="sidebar">
            <SidebarMenu />
          </div>
          <div className="main-content">
            <Routes>
              <Route path="/" element={<MapWithDraw />} />
              <Route path="/map" element={<HomePage />} />
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
              <Route path="/forgotpassword" element={<ForgotPassword />} />
              <Route path="/resetpassword" element={<ResetPasswordForm />} />
              <Route path="/admin" element={<Admin />} />
              <Route path="/profile" element={<Profile />} />
              <Route path="/debug-model" element={<ModelDebugPage />} />
              <Route path="/layer" element={<LayerPage />} />
            </Routes>
          </div>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
