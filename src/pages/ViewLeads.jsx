import React, { useState, useEffect } from "react";
import { Link } from "react-router-dom";

const ViewLeads = () => {
  const [isSidebarOpen, setSidebarOpen] = useState(true);
  const [leads, setLeads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const [userRole, setUserRole] = useState(null); // Store user role
  const leadsPerPage = 5; // Show 5 leads per page

  // Define status colors
  const statusColors = {
    New: "bg-green-200",
    Lost: "bg-red-200",
    Contacted: "bg-blue-200",
    Converted: "bg-yellow-200",
  };

  useEffect(() => {
    const fetchData = async () => {
      try {
        const token = sessionStorage.getItem("token");
        if (!token) {
          console.error("No token found");
          return;
        }

        // Fetch User Role
        const roleResponse = await fetch(
          "http://localhost:5238/auth/user-role",
          {
            method: "GET",
            headers: { Authorization: `Bearer ${token}` },
          }
        );

        if (roleResponse.ok) {
          const roleData = await roleResponse.json();
          setUserRole(roleData.role);
        } else {
          console.error("Failed to fetch user role");
        }

        // Fetch Leads
        const leadsResponse = await fetch("http://localhost:5238/viewLeads", {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
        });

        if (!leadsResponse.ok) {
          throw new Error("Failed to fetch leads");
        }

        const leadsData = await leadsResponse.json();
        setLeads(leadsData);
        console.log(leadsData);
      } catch (error) {
        console.error("Error fetching data:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchData();
  }, []);

  // Pagination logic
  const indexOfLastLead = currentPage * leadsPerPage;
  const indexOfFirstLead = indexOfLastLead - leadsPerPage;
  const currentLeads = leads.slice(indexOfFirstLead, indexOfLastLead);

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        Home &gt; View Leads
      </div>

      <div className="mt-10 p-6 bg-white shadow-md rounded-lg font-rem w-full">
        <div className="flex justify-between mb-4">
          <h2 className="text-[22px] font-semibold mb-4 text-left">Leads</h2>
          <span className="bg-transparent rounded-[10px] text-[#333333] px-3 py-1 text-lg">
            Total Leads: {leads.length}
          </span>
        </div>

        {/* Leads Table */}
        {loading ? (
          <p>Loading leads...</p>
        ) : (
          <>
            <table className="w-full border-collapse border rounded-lg overflow-hidden">
              <thead>
                <tr className="text-left">
                  <th className="p-3 font-normal">Id</th>
                  <th className="p-3 font-normal">Name</th>
                  <th className="p-3 font-normal">Email</th>
                  <th className="p-3 font-normal">Phone</th>
                  <th className="p-3 font-normal">Status</th>
                  <th className="p-3 font-normal">Lead Source</th>
                  {/* Hide "Assigned To" for Manager and Sales Rep */}
                  {userRole !== "Sales Representative" && (
                    <th className="p-3 font-normal">Assigned To</th>
                  )}
                  <th className="p-3 font-normal">Actions</th>
                </tr>
              </thead>
              <tbody>
                {currentLeads.map((lead) => (
                  <tr key={lead.id}>
                    <td>{lead.id}</td>
                    <td className="text-left p-3 font-normal">{lead.name}</td>
                    <td className="text-left p-3 font-normal text-[#979797]">
                      {lead.email}
                    </td>
                    <td className="text-left p-3 font-normal text-[#979797]">
                      {lead.phone}
                    </td>
                    <td className="text-left p-3 font-normal">
                      <span
                        className={`px-2 py-1 rounded-[10px] text-[13px] ${
                          statusColors[lead.status] || "bg-gray-200"
                        }`}
                      >
                        {lead.status}
                      </span>
                    </td>
                    <td className="text-left p-3 font-normal text-[#979797]">
                      {lead.leadSource}
                    </td>
                    {/* Hide "Assigned To" for Manager and Sales Rep */}
                    {userRole !== "Sales Representative" && (
                      <td className="text-left p-3 font-normal text-[#979797]">
                        {lead.assignedTo || "Unassigned"}
                      </td>
                    )}
                    <td className="text-left p-3">
                      <Link to={`/leadinfo/${lead.id}`}>
                        <button className="bg-[#009699] text-white px-4 py-2 rounded-lg">
                          View
                        </button>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Pagination Controls */}
            <div className="flex justify-between mt-4 mx-5">
              <button
                onClick={() => setCurrentPage((prev) => Math.max(prev - 1, 1))}
                disabled={currentPage === 1}
                className={`px-4 py-2 rounded-lg text-white ${
                  currentPage === 1 ? "bg-gray-400" : "bg-[#009699]"
                }`}
              >
                Previous
              </button>
              <span className="px-4 py-2 rounded-lg border text-[#333333]">
                Page {currentPage} of {Math.ceil(leads.length / leadsPerPage)}
              </span>
              <button
                onClick={() =>
                  setCurrentPage((prev) =>
                    prev < Math.ceil(leads.length / leadsPerPage)
                      ? prev + 1
                      : prev
                  )
                }
                disabled={currentPage >= Math.ceil(leads.length / leadsPerPage)}
                className={`px-4 py-2 rounded-lg text-white ${
                  currentPage >= Math.ceil(leads.length / leadsPerPage)
                    ? "bg-gray-400"
                    : "bg-[#009699]"
                }`}
              >
                Next
              </button>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default ViewLeads;
