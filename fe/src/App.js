// // src/App.js
// import React from 'react';
// import './App.css';
// import SidebarMenu from './components/SidebarMenu/SidebarMenu';
// import MapWithDraw from './components/MapComponent/MapComponent';


// function App() {
//   return (
//     <div className="app-container">
//       <div className="sidebar">
//         <SidebarMenu />
//       </div>
//       <div className="main-content">
//         <h1>My First React App</h1>
//         <MapWithDraw />
//       </div>
//     </div>
//   );
// }

// export default App;
import React from "react";
import { BrowserRouter as Router, Route, Routes } from "react-router-dom";
import SidebarMenu from "./components/SidebarMenu/SidebarMenu";
import Login from "./components/Auth/Login";
import Register from "./components/Auth/Register";
import { AuthProvider } from "./contexts/AuthContext";
import MapWithDraw from './components/MapComponent/MapComponent';
import './App.css';

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
              <Route path="/login" element={<Login />} />
              <Route path="/register" element={<Register />} />
            </Routes>
            <MapWithDraw />
          </div>
        </div>
      </Router>
    </AuthProvider>
  );
}

export default App;
