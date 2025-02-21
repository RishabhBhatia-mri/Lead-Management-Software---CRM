import React, { useState, useEffect } from "react";
import ManualLeadForm from "./ManualLeadForm";
import CsvLeadImport from "./CsvLeadImport";

const AddNewLeadsManager = () => {
  const [formData, setFormData] = useState({
    name: "",
    email: "",
    phone: "",
    source: "",
    status: "New",
    salesRepAssigned: "",
  });

  const [salesReps, setSalesReps] = useState([]);
  const token = sessionStorage.getItem("token");
  const [userRole, setUserRole] = useState(null);
  const [userId, setUserId] = useState(null);

  useEffect(() => {
    const fetchData = async () => {
      try {
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
        setUserId(data.userId);

        // If the user is a Sales Representative, set their own ID in the dropdown
        if (data.role === "Sales Representative") {
          setFormData((prev) => ({
            ...prev,
            salesRepAssigned: data.userId,
          }));
        }
      } catch (error) {
        console.error("Error fetching role:", error);
      }
    };

    fetchData();
  }, []);

  useEffect(() => {
    if (userRole && userRole !== "Sales Representative") {
      fetch("http://localhost:5238/manager-leads/getSalesReps", {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
      })
        .then((res) => res.json())
        .then((data) => setSalesReps(data))
        .catch((error) => console.error("Error fetching sales reps:", error));
    }
  }, [userRole]);

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (
      !formData.name ||
      !formData.email ||
      !formData.phone ||
      !formData.source ||
      !formData.salesRepAssigned
    ) {
      return alert("All fields are required!");
    }

    try {
      const response = await fetch(
        "http://localhost:5238/manager-leads/addNewLeads",
        {
          method: "POST",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
          body: JSON.stringify({
            ...formData,
            salesRepAssigned: parseInt(formData.salesRepAssigned) || null,
          }),
        }
      );

      if (response.ok) {
        alert("Lead added successfully!");
        setFormData({
          name: "",
          email: "",
          phone: "",
          source: "",
          status: "New",
          salesRepAssigned: userRole === "Sales Representative" ? userId : "",
        });
      } else {
        alert("Error adding lead");
      }
    } catch (error) {
      console.error("Error submitting lead:", error);
    }
  };

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20 ">
        Home &gt; Create User
      </div>
      <div className="bg-white p-5 mt-5 rounded-md">
        <ManualLeadForm
          formData={formData}
          handleChange={handleChange}
          handleSubmit={handleSubmit}
          salesReps={salesReps}
          leadSources={["Website", "Reference", "Ads", "Social Media"]}
          statuses={["New", "Contacted", "Follow-up", "Converted", "Lost"]}
        />
        {userRole === "Manager" ? <CsvLeadImport token={token} /> : null}
      </div>
    </div>
  );
};

export default AddNewLeadsManager;
