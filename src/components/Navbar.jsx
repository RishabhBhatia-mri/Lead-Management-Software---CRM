import React, { useState, useEffect } from "react";
import { Button, Avatar, Menu, MenuItem, Fade } from "@mui/material";
import { useNavigate, NavLink } from "react-router-dom"; // Import useNavigate

const Navbar = ({ isSidebarOpen }) => {
  const [anchorEl, setAnchorEl] = useState(null);
  const [open, setOpen] = useState(false);
  const [userName, setUserName] = useState("");
  const navigate = useNavigate(); // Initialize navigate for redirection
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

  useEffect(() => {
    const fetchUserData = async () => {
      try {
        const token = sessionStorage.getItem("token");

        const response = await fetch("http://localhost:5238/profile", {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`, // Add the token here
          },
        });

        if (response.ok) {
          const data = await response.json();
          setUserName(data.name);
        } else {
          console.error("Failed to fetch user data.");
        }
      } catch (error) {
        console.error("Error fetching user data:", error);
      }
    };

    fetchUserData();
  }, []);

  const firstLetter = userName.trim().charAt(0).toUpperCase();
  const firstName = userName.split(" ")[0];

  const handleClick = (event) => {
    setAnchorEl(event.currentTarget);
    setOpen(true);
  };

  const handleClose = () => {
    setOpen(false);
  };

  const handleProfileClick = () => {
    navigate("/Profile");
    handleClose();
  };

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

  return (
    <header
      className={`fixed top-0 left-0 right-0 bg-white shadow-md p-4 flex justify-end items-center h-16 transition-all duration-300 ${
        isSidebarOpen ? "pl-64" : "pl-0"
      }`}
    >
      <div className="flex items-center space-x-6">
        {userRole === "Admin" ? (
          <NavLink to="/export">
            <button className="bg-teal-600 text-white px-4 py-2 rounded-lg hidden md:block">
              Export
            </button>
          </NavLink>
        ) : null}
        <button className="bg-transparent hover:bg-transparent">🔔</button>
        <div style={{ display: "flex", flexDirection: "row", margin: "15px" }}>
          <Avatar
            className="avatar"
            style={{
              marginLeft: "10px",
              cursor: "pointer",
            }}
          >
            {firstLetter}
          </Avatar>
          <Button
            aria-controls="fade-menu"
            aria-haspopup="true"
            onClick={handleClick}
            style={{
              backgroundColor: open ? "lightblue" : "transparent",
            }}
          >
            {firstName}
          </Button>
        </div>
        <Menu
          id="fade-menu"
          anchorEl={anchorEl}
          keepMounted
          open={open}
          onClose={handleClose}
          TransitionComponent={Fade}
        >
          <MenuItem onClick={handleProfileClick}>Profile</MenuItem>
          <MenuItem onClick={handleLogout}>Logout</MenuItem>
        </Menu>
      </div>
    </header>
  );
};

export default Navbar;
