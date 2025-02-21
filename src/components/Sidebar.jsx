import React, { useEffect, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import {
  Home2,
  SearchNormal,
  UserAdd,
  ReceiveSquare,
  DocumentFilter,
  People,
  Setting2,
  LogoutCurve,
} from "iconsax-react";

const Sidebar = () => {
  const navigate = useNavigate();
  const [userRole, setUserRole] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = sessionStorage.getItem("token");
        if (!token) {
          console.error("No token found");
          return;
        }

        const response = await fetch("http://localhost:5238/auth/user-role", {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!response.ok) {
          console.error("Failed to fetch user role");
          return;
        }

        const data = await response.json();
        setUserRole(data.role);
      } catch (error) {
        console.error("Error fetching role:", error);
      }
    };

    fetchData();
  }, []);

  const handleLogout = async () => {
    try {
      const token = sessionStorage.getItem("token");
      const response = await fetch("http://localhost:5238/auth/logout", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      });

      if (response.ok) {
        sessionStorage.removeItem("token");
        navigate("/login");
      } else {
        console.error("Logout failed.");
      }
    } catch (error) {
      console.error("Error logging out:", error);
    }
  };

  const renderNavLink = (to, icon, text, roleCondition = true) => {
    if (!roleCondition) return null;
    return (
      <NavLink
        to={to}
        className={({ isActive }) =>
          `flex items-center space-x-3 p-3 text-lg ${
            isActive
              ? "text-[#636060] bg-[#CCFFFF] font-semibold rounded-[10px]"
              : "hover:bg-gray-200 rounded-lg"
          }`
        }
      >
        {icon}
        <span>{text}</span>
      </NavLink>
    );
  };

  return (
    <aside className="fixed left-0 top-0 h-screen w-64 bg-white shadow-md">
      <div className="p-6 flex items-center justify-between">
        <h1 className="text-4xl font-bold text-teal-600 font-montserrat">
          LMS
        </h1>
      </div>
      <nav className="flex flex-col space-y-4 p-4 text-gray-700">
        {renderNavLink(
          "/dashboard",
          <Home2 size="24" color="#636060" />,
          "Dashboard"
        )}
        {renderNavLink(
          "/viewleads",
          <SearchNormal size="24" color="#636060" />,
          "View Leads"
        )}

        {/* Conditional Rendering for Admin Role */}
        {renderNavLink(
          "/addlead",
          <UserAdd size="24" color="#636060" />,
          userRole === "Admin" ? "Create User" : "Add New Lead",
          userRole !== null
        )}
        {renderNavLink(
          "/export",
          <ReceiveSquare size="24" color="#636060" />,
          "Export",
          userRole === "Admin"
        )}
        {renderNavLink(
          "/assignSalesRep",
          <ReceiveSquare size="24" color="#636060" />,
          "Assign Sales Rep",
          userRole === "Manager"
        )}
        {renderNavLink(
          "/kanban",
          <DocumentFilter size="24" color="#636060" />,
          "Lead Pipeline"
        )}
        {renderNavLink(
          "/userManagement",
          <People size="24" color="#636060" />,
          "User Management",
          userRole !== "Sales Representative"
        )}
        {renderNavLink(
          "/Profile",
          <Setting2 size="24" color="#636060" />,
          "Settings"
        )}
        <NavLink
          onClick={handleLogout}
          className="flex items-center space-x-3 p-3 text-lg hover:bg-gray-200 rounded-lg"
        >
          <LogoutCurve size="24" color="#636060" />
          <span>Sign Out</span>
        </NavLink>
      </nav>
    </aside>
  );
};

export default Sidebar;
