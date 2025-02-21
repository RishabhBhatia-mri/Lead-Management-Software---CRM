import "./App.css";
import AppRoutes from "./routes/AppRoutes";
import React from "react";
import Sidebar from "./components/Sidebar";
import Navbar from "./components/Navbar";
import { useLocation } from "react-router-dom";

function App() {
  const location = useLocation();

  // Define routes where Navbar and Sidebar should be hidden
  const hideLayoutRoutes = ["/login", "*"];

  // Hide layout if it's a login page or 404 page
  const hideLayout = hideLayoutRoutes.includes(location.pathname);

  return (
    <div className="flex flex-col h-screen">
      {!hideLayout && <Navbar />}
      <div className="flex flex-1">
        {!hideLayout && <Sidebar />}
        <div
          className={`flex flex-col flex-1 min-h-screen transition-all duration-300 ${
            !hideLayout ? "pl-40" : "pl-0"
          }`}
        >
          <AppRoutes />
        </div>
      </div>
    </div>
  );
}

export default App;
