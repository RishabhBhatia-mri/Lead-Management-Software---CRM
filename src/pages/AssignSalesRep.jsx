import React, { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

const LeadAssignments = () => {
  const [leads, setLeads] = useState([]);
  const [loading, setLoading] = useState(true);
  const [currentPage, setCurrentPage] = useState(1);
  const leadsPerPage = 5;
  const navigate = useNavigate();
  const token = sessionStorage.getItem("token");

  useEffect(() => {
    const fetchLeads = async () => {
      try {
        const response = await fetch(
          "http://localhost:5238/dashboard/manager/assignment",
          {
            method: "GET",
            headers: {
              Authorization: `Bearer ${token}`,
              "Content-Type": "application/json",
            },
          }
        );
        if (!response.ok) throw new Error("Failed to fetch leads");
        const data = await response.json();
        setLeads(data.leadList);
      } catch (error) {
        console.error("Error fetching leads:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchLeads();
  }, []);

  // Pagination
  const indexOfLastLead = currentPage * leadsPerPage;
  const indexOfFirstLead = indexOfLastLead - leadsPerPage;
  const currentLeads = leads.slice(indexOfFirstLead, indexOfLastLead);

  return (
    <div className="flex-auto min-h-screen w-[1094px] p-6">
      <div className="text-left text-[18px] text-gray-600 font-rem font-normal mt-20">
        Home &gt; Lead Assignments
      </div>

      {/* Leads table */}
      <div className="mt-10 p-6 bg-white shadow-md rounded-lg font-rem w-full">
        <div className="text-left text-2xl mt-2 mb-4">Unassigned Leads</div>

        {loading ? (
          <p className="text-center text-gray-600">Loading leads...</p>
        ) : (
          <table className="w-full border rounded-lg overflow-hidden">
            <thead>
              <tr className="text-left bg-gray-200">
                <th className="p-3 font-normal">Id</th>
                <th className="p-3 font-normal">Name</th>
                <th className="p-3 font-normal">Email</th>
                <th className="p-3 font-normal">Phone</th>
                <th className="p-3 font-normal">Status</th>
                <th className="p-3 font-normal">Action</th>
              </tr>
            </thead>
            <tbody>
              {currentLeads.map((lead) => (
                <tr key={lead.lid}>
                  <td className="text-left p-3 font-normal border-b">
                    {lead.lid}
                  </td>
                  <td className="text-left p-3 font-normal border-b">
                    {lead.name}
                  </td>
                  <td className="text-left p-3 font-normal text-gray-500 border-b">
                    {lead.email}
                  </td>
                  <td className="text-left p-3 font-normal text-gray-500 border-b">
                    {lead.phone}
                  </td>
                  <td className="text-left p-3 font-normal border-b">
                    <span className="px-2 py-1 rounded-md text-sm bg-gray-100 text-gray-700">
                      {lead.status}
                    </span>
                  </td>
                  <td className="text-left p-3 font-normal border-b">
                    {lead.salesRep_Assigned === "Unassigned" ? (
                      <button
                        onClick={() => navigate(`/assign/${lead.lid}`)}
                        className="px-3 py-2 bg-blue-500 text-white rounded-md hover:bg-blue-600"
                      >
                        Assign
                      </button>
                    ) : (
                      <span>{lead.salesRep_Assigned}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}

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
                prev < Math.ceil(leads.length / leadsPerPage) ? prev + 1 : prev
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
      </div>
    </div>
  );
};

export default LeadAssignments;
