import React, { useEffect, useState } from "react";

const KanbanBoard = () => {
  const [kanbanData, setKanbanData] = useState({});
  const [userRole, setUserRole] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchUserRole = async () => {
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

    fetchUserRole();
  }, []);

  useEffect(() => {
    if (!userRole) return;

    const fetchKanbanData = async () => {
      try {
        setLoading(true);
        const token = sessionStorage.getItem("token");
        if (!token) {
          console.error("No token found.");
          return;
        }

        // Dynamically determine API URL based on role
        const apiUrl = {
          Admin: "http://localhost:5238/kanban/admin",
          Manager: "http://localhost:5238/kanban/manager",
          "Sales Representative": "http://localhost:5238/kanban/salesRep",
        }[userRole];

        if (!apiUrl) {
          console.error("Invalid role:", userRole);
          return;
        }

        const response = await fetch(apiUrl, {
          method: "GET",
          headers: {
            "Content-Type": "application/json",
            Authorization: `Bearer ${token}`,
          },
        });

        if (!response.ok) {
          console.error("Failed to fetch Kanban data:", response.status);
          return;
        }

        const data = await response.json();
        setKanbanData(data);
      } catch (error) {
        console.error("Error fetching Kanban data:", error);
      } finally {
        setLoading(false);
      }
    };

    fetchKanbanData();
  }, [userRole]);

  if (!userRole) return <p>Loading user role...</p>;
  if (loading) return <p>Loading Kanban data...</p>;

  return (
    <div className="h-screen w-full bg-transparent text-neutral-900 p-8">
      <Board kanbanData={kanbanData} />
    </div>
  );
};

const Board = ({ kanbanData }) => {
  return (
    <div className="flex h-auto w-full gap-2 px-5 bg-white mt-10 mb-10 rounded-xl shadow-md">
      {["New", "Contacted", "Follow-up", "Converted", "Lost"].map((status) => (
        <Column key={status} title={status} leads={kanbanData[status] || []} />
      ))}
    </div>
  );
};

const Column = ({ title, leads }) => {
  return (
    <div className="w-52 bg-transparent p-3 flex flex-col h-auto mt-5 border-r-2">
      <div className="mb-3 flex justify-between items-center">
        <h3 className="text-lg font-semibold">{title}</h3>
        <span className="text-sm text-gray-600">{leads.length}</span>
      </div>
      <div className="space-y-3 pr-2 flex-1">
        {leads.map((lead) => (
          <Card key={lead.lid} lead={lead} />
        ))}
      </div>
    </div>
  );
};

const Card = ({ lead }) => (
  <div className="rounded-lg border p-3 shadow hover:shadow-lg transition text-sm text-start break-words">
    <p className="text-md font-semibold">Id: {lead.lid}</p>
    <p className="text-sm font-semibold">{lead.name}</p>
    <p className="text-gray-600">{lead.email}</p>
    <p className="text-gray-600">{lead.phone}</p>
    <p className="text-gray-500">Source: {lead.source}</p>
  </div>
);

export default KanbanBoard;
