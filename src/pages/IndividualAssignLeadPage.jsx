import React, { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";

const IndividualAssignLeadPage = () => {
  const { leadId } = useParams();
  const navigate = useNavigate();
  const token = sessionStorage.getItem("token");

  const [leadData, setLeadData] = useState(null);
  const [salesReps, setSalesReps] = useState([]);
  const [selectedSalesRep, setSelectedSalesRep] = useState("");
  const [error, setError] = useState(null);

  useEffect(() => {
    const fetchLeadAndSalesReps = async () => {
      try {
        // Fetch lead details
        const leadResponse = await fetch(
          `http://localhost:5238/leadInfo/${leadId}`,
          {
            method: "GET",
            headers: { Authorization: `Bearer ${token}` },
          }
        );

        if (!leadResponse.ok) throw new Error("Failed to fetch lead data");
        const lead = await leadResponse.json();
        setLeadData(lead);

        // Fetch sales reps
        const salesRepResponse = await fetch(
          "http://localhost:5238/manager-leads/getSalesReps",
          {
            method: "GET",
            headers: { Authorization: `Bearer ${token}` },
          }
        );

        if (!salesRepResponse.ok) throw new Error("Failed to fetch sales reps");
        const reps = await salesRepResponse.json();
        console.log("Sales Reps API Response:", reps);
        setSalesReps(reps);
      } catch (error) {
        setError(error.message);
      }
    };

    fetchLeadAndSalesReps();
  }, [leadId, token]);

  const handleAssignLead = async () => {
    if (!selectedSalesRep) {
      alert("Please select a Sales Rep before assigning.");
      return;
    }

    const payload = { salesRepId: parseInt(selectedSalesRep) }; // Ensure it's an integer

    console.log("Assigning Lead:", leadId);
    console.log("Request Payload:", payload);

    try {
      const response = await fetch(
        `http://localhost:5238/management/assignment/${leadId}/assign`,
        {
          method: "POST",
          headers: {
            Authorization: `Bearer ${token}`,
            "Content-Type": "application/json",
          },
          body: JSON.stringify(payload),
        }
      );

      if (!response.ok) {
        const errorData = await response.json();
        console.error("API Error:", errorData);
        throw new Error(errorData.message || "Failed to assign lead");
      }
      console.log("Sales Reps Data:", salesReps);

      alert("Sales rep assigned successfully");
      navigate("/assignSalesRep");
    } catch (error) {
      console.error("Error assigning lead:", error);
    }
  };

  if (error) return <div>Error: {error}</div>;
  if (!leadData) return <div>Loading...</div>;

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        Home &gt; Assign Leads
      </div>
      <div className="mt-10 p-4 bg-white shadow-md rounded-lg font-rem w-full text-start">
        <h2 className="text-xl font-bold text-start">Assign Lead</h2>
        <p>
          <strong>Email:</strong> {leadData.email}
        </p>
        <p>
          <strong>Phone:</strong> {leadData.phone}
        </p>
        <p>
          <strong>Status:</strong> {leadData.status}
        </p>
        <p>
          <strong>Current Assigned To:</strong>{" "}
          {leadData.assignedTo || "Not Assigned"}
        </p>
        <label className="pr-2 mt-4 font-semibold inline">
          Assign Sales Rep:
        </label>
        <select
          value={selectedSalesRep}
          onChange={(e) => setSelectedSalesRep(e.target.value)}
          className="border border-black rounded-md p-2 mr-5"
        >
          <option value="">Select Sales Rep</option>
          {salesReps.map((rep, index) => (
            <option key={rep.uid || index} value={rep.uid}>
              {rep.name}
            </option>
          ))}
        </select>

        <button
          className="mt-4 bg-blue-500 text-white px-4 py-2 rounded-md block"
          onClick={handleAssignLead}
        >
          Assign Lead
        </button>
      </div>
    </div>
  );
};

export default IndividualAssignLeadPage;
