import React, { useState, useEffect } from "react";
import { NavLink } from "react-router-dom";
import CountUp from "react-countup";

import {
  FaUsers,
  FaPlus,
  FaChartPie,
  FaArrowUp,
  FaClipboardList,
} from "react-icons/fa";
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Legend,
  Pie,
  Cell,
} from "recharts";

const AdminDashboard = () => {
  const [isSidebarOpen, setSidebarOpen] = useState(true);
  const [managersData, setManagersData] = useState([]);
  const [dashboardData, setDashboardData] = useState({
    totalLeads: 0,
    newLeads: 0,
    convertedLeads: 0,
    followUpLeads: 0,
  });

  const fetchDashboardData = async () => {
    try {
      const token = sessionStorage.getItem("token");
      if (!token) throw new Error("Token not found. Please log in.");

      // Fetch dashboard stats
      const dashboardResponse = await fetch(
        "http://localhost:5238/dashboard/admin/count",
        {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (!dashboardResponse.ok)
        throw new Error(`HTTP error! status: ${dashboardResponse.status}`);

      const dashboard = await dashboardResponse.json();
      setDashboardData(dashboard);

      // Fetch managers details
      const managersResponse = await fetch(
        "http://localhost:5238/dashboard/admin/managers/details",
        {
          method: "GET",
          headers: { Authorization: `Bearer ${token}` },
        }
      );
      if (!managersResponse.ok)
        throw new Error(`HTTP error! status: ${managersResponse.status}`);

      const managers = await managersResponse.json();
      console.log("Managers API Response:", managers);
      setManagersData(managers.managers || []);
    } catch (error) {
      console.error("Error fetching data:", error);
    }
  };

  // Fetch data when the component mounts
  useEffect(() => {
    fetchDashboardData();
  }, []);

  // Render a loading state if the data is still being fetched
  if (dashboardData.totalLeads === 0) {
    return <div>Loading...</div>;
  }

  const cardData = [
    {
      title: "Total Leads",
      value: dashboardData.totalLeads,
      icon: <FaUsers size={30} />,
    },
    {
      title: "New Leads",
      value: dashboardData.newLeads,
      icon: <FaPlus size={30} />,
    },
    {
      title: "Converted Leads",
      value: dashboardData.convertedLeads,
      icon: <FaChartPie size={30} />,
    },
    {
      title: "Lead Conversion",
      value: dashboardData.convertedLeads,
      icon: <FaArrowUp size={30} />,
    },
    {
      title: "Pending Follow-ups",
      value: dashboardData.followUpLeads,
      icon: <FaClipboardList size={30} />,
    },
  ];

  // Line chart
  const lineData = managersData.map((manager) => ({
    name: manager.name,
    leadsAssigned: Number(manager.leadsAssigned) || 0,
  }));
  console.log("Line Chart Data:", lineData);

  // Pie chart data

  const COLORS = ["#0088FE", "#00C49F", "#FFBB28", "#FF8042", "#FF4567"];

  // Transform API data into Pie Chart format
  const pieData = [
    { name: "New Leads", value: dashboardData.newLeads, color: COLORS[0] },
    {
      name: "Contacted Leads",
      value: dashboardData.contactedLeads,
      color: COLORS[1],
    },
    {
      name: "Follow-ups",
      value: dashboardData.followUpLeads,
      color: COLORS[2],
    },
    {
      name: "Converted Leads",
      value: dashboardData.convertedLeads,
      color: COLORS[3],
    },
    { name: "Lost Leads", value: dashboardData.lostLeads, color: COLORS[4] },
  ];

  return (
    <div className="flex w-full overflow-hidden">
      <div className="flex-1 overflow-auto pt-20 px-0 w-full">
        {/* Dashboard Cards */}
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          {cardData.map((card, index) => (
            <div
              key={index}
              className="p-4 bg-white shadow rounded-lg flex items-center justify-evenly"
            >
              <div className="text-teal-600 text-2xl">{card.icon}</div>
              <div className="ml-4">
                <p className="text-sm text-gray-500">{card.title}</p>
                <p className="text-xl font-bold">
                  <CountUp end={card.value} duration={2} />
                </p>
              </div>
            </div>
          ))}
        </div>

        {/* Graphs Section */}
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mt-6">
          {/* Line Chart */}
          <div className="bg-white shadow-lg p-4 rounded-lg">
            {lineData.length === 0 ? (
              <p className="text-center text-gray-500">Loading data...</p>
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={lineData}>
                  <XAxis dataKey="name" tick={{ fill: "#333", fontSize: 12 }} />{" "}
                  <YAxis
                    domain={[0, 50]}
                    tick={{ fill: "#333", fontSize: 12 }}
                  />{" "}
                  <Tooltip />
                  <Legend />
                  <Line
                    type="monotone"
                    dataKey="leadsAssigned"
                    stroke="#FF5733"
                    strokeWidth={3}
                    dot={{ fill: "#FF5733", r: 5 }}
                  />
                </LineChart>
              </ResponsiveContainer>
            )}
          </div>
          {/* Pie Chart */}
          <div className="bg-white shadow-lg p-4 rounded-lg">
            <h2 className="text-lg font-semibold mb-2">Lead Status</h2>
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={pieData}
                  dataKey="value"
                  nameKey="name"
                  outerRadius={100}
                >
                  {pieData.map((entry, index) => (
                    <Cell key={index} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip />
                <Legend />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </div>
        {/* Managers Performance table */}
        <div className="bg-white shadow-lg p-6 rounded-lg mt-6">
          <h2 className="text-lg font-semibold font-rem text-left">
            Managers Performance
          </h2>
          <div className="overflow-x-auto mt-4 border rounded-xl">
            <table className="w-full">
              <thead>
                <tr className="bg-[#EDF2F6] text-[#636060]">
                  <th className="text-left font-normal font-rem p-3">Id</th>
                  <th className="text-left font-normal font-rem p-3">
                    Manager Name
                  </th>
                  <th className="text-left font-normal font-rem p-3">
                    Leads Assigned
                  </th>
                  <th className="text-left font-normal font-rem p-3">
                    Follow-ups Completed
                  </th>
                  <th className="text-left font-normal font-rem p-3">
                    Converted Leads
                  </th>
                  <th className="text-left font-normal font-rem p-3">
                    Conversion Rate (%)
                  </th>
                </tr>
              </thead>
              <tbody>
                {managersData.length > 0 ? (
                  managersData.map((manager, index) => (
                    <tr key={manager.uid} className="font-rem text-[#333333]">
                      <td className="p-3">{index + 1}</td>
                      <td className="p-3">{manager.name}</td>
                      <td className="p-3">{manager.leadsAssigned}</td>
                      <td className="p-3">{manager.followUpLeads}</td>
                      <td className="p-3">{manager.convertedLeads}</td>
                      <td className="p-3">
                        {manager.leadsAssigned > 0
                          ? (
                              (manager.convertedLeads / manager.leadsAssigned) *
                              100
                            ).toFixed(2)
                          : "0.00"}
                      </td>
                    </tr>
                  ))
                ) : (
                  <tr>
                    <td colSpan="6" className="text-center text-gray-500 p-4">
                      No data available
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

export default AdminDashboard;
