import React, { useEffect, useState } from "react";

const ManualLeadForm = ({
  formData,
  handleChange,
  handleSubmit,
  salesReps = [],
  leadSources = [],
  statuses = [],
}) => {
  const [userRole, setUserRole] = useState(null);
  const [userId, setUserId] = useState(null);
  const [loading, setLoading] = useState(true);

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
        setUserId(data.uid);
      } catch (error) {
        console.error("Error fetching role:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  if (loading) {
    return <p>Loading...</p>; // Prevents UI glitches before fetching user details
  }

  return (
    <div>
      <h2 className="text-[20px] font-normal text-start text-black">
        Add New Lead
      </h2>
      <div className="space-y-2 mt-4 w-[592px] text-start">
        <label>Full Name</label>
        <input
          type="text"
          name="name"
          value={formData.name}
          onChange={handleChange}
          className="border p-2 rounded w-full"
        />

        <label>Email Address</label>
        <input
          type="email"
          name="email"
          value={formData.email}
          onChange={handleChange}
          className="border p-2 rounded w-full"
        />

        <label>Phone Number</label>
        <input
          type="number"
          name="phone"
          value={formData.phone}
          onChange={handleChange}
          className="border p-2 rounded w-full"
        />

        <label>Lead Source</label>
        <select
          name="source"
          value={formData.source}
          onChange={handleChange}
          className="border p-2 rounded w-full"
        >
          <option value="">Select Source</option>
          {leadSources.map((source) => (
            <option key={source} value={source}>
              {source}
            </option>
          ))}
        </select>

        <label>Status</label>
        <select
          name="status"
          value={formData.status}
          onChange={handleChange}
          className="border p-2 rounded w-full"
        >
          {statuses.map((status) => (
            <option key={status} value={status}>
              {status}
            </option>
          ))}
        </select>

        <label>Assigned Sales Rep</label>
        <select
          name="salesRepAssigned"
          value={
            userRole === "Sales Representative"
              ? userId
              : formData.salesRepAssigned || ""
          }
          onChange={handleChange}
          className="border p-2 rounded w-full"
          disabled={userRole === "Sales Representative"}
        >
          <option value="">Select Sales Rep</option>
          {userRole === "Sales Representative" ? (
            <option value={userId}>
              {formData.salesRepAssignedName || "You"}
            </option>
          ) : salesReps.length > 0 ? (
            salesReps.map((rep) => (
              <option key={rep.uid} value={rep.uid}>
                {rep.name}
              </option>
            ))
          ) : (
            <option disabled>No Sales Reps Available</option>
          )}
        </select>

        <div className="flex justify-end mt-6 space-x-2">
          <button className="bg-transparent text-[#009699] border-[#B1B1B1] border-[2px] rounded-[10px] px-5 py-2">
            Cancel
          </button>
          <button
            onClick={handleSubmit}
            className="bg-[#009699] text-white rounded-[10px] px-5 py-2"
          >
            Save
          </button>
        </div>
      </div>
    </div>
  );
};

export default ManualLeadForm;
