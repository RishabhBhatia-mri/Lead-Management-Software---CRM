import React, { useEffect, useState } from "react";
import AddNewUser from "./AddNewUser";
import AddNewLeadsManager from "./AddNewLeadsManager"; // Assuming this is the normal Add New Lead component

const AddNewLeads = () => {
  const [role, setRole] = useState(null);
  const token = sessionStorage.getItem("token");

  useEffect(() => {
    const fetchUserRole = async () => {
      try {
        const response = await fetch("http://localhost:5238/auth/user-role", {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
        });

        if (response.ok) {
          const data = await response.json();
          setRole(data.role); // Assuming API returns { role: "Manager" } or { role: "Admin" }
        } else {
          console.error("Failed to fetch user role");
        }
      } catch (error) {
        console.error("Error fetching role:", error);
      }
    };

    fetchUserRole();
  }, [token]);

  if (role === null) {
    return <div>Loading...</div>; // Show a loading state while fetching role
  }

  return role === "Admin" ? <AddNewUser /> : <AddNewLeadsManager />;
};

export default AddNewLeads;
