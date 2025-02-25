import "./App.css";
import AppRoutes from "./routes/AppRoutes";
import React from "react";
import Sidebar from "./components/Sidebar";
import Navbar from "./components/Navbar";
import { useLocation } from "react-router-dom";

function App() {
  const location = useLocation();

  const validBaseRoutes = [
    "/dashboard",
    "/profile",
    "/viewleads",
    "/leadinfo",
    "/export",
    "/kanban",
    "/addlead",
    "/userManagement",
    "/assignSalesRep",
    "/assign",
  ];

  // Check if the current route starts with any valid base route
  const isValidRoute = validBaseRoutes.some(
    (route) =>
      location.pathname.startsWith(route + "/") || location.pathname === route
  );

  // Hide Navbar and Sidebar if it's a login page or an unknown route (404)
  const hideLayout = location.pathname === "/login" || !isValidRoute;

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
