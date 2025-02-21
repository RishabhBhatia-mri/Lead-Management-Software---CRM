import React, { useState } from "react";
import { useNavigate } from "react-router-dom";

const ExportPage = () => {
  const [managerId, setManagerId] = useState(null);
  const [salesRepId, setSalesRepId] = useState(null);
  const [status, setStatus] = useState("");
  const [startDate, setStartDate] = useState("");
  const [endDate, setEndDate] = useState("");
  const [format, setFormat] = useState("excel"); // Default to Excel
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);

  const navigate = useNavigate();

  // Function to handle form submission
  const handleExport = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError(null);

    const exportRequest = {
      managerId,
      salesRepId,
      status,
      startDate: startDate ? new Date(startDate).toISOString() : null,
      endDate: endDate ? new Date(endDate).toISOString() : null,
      format,
    };

    try {
      const token = sessionStorage.getItem("token");
      const response = await fetch("http://localhost:5238/export/leads", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(exportRequest),
      });

      if (response.ok) {
        const fileBlob = await response.blob();
        const fileName = response.headers
          .get("Content-Disposition")
          .split("filename=")[1]
          .replace(/"/g, "");
        const link = document.createElement("a");
        link.href = URL.createObjectURL(fileBlob);
        link.download = fileName;
        link.click();
      } else {
        setError("Failed to export data. Please try again.");
      }
    } catch (err) {
      setError("An error occurred. Please try again.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        Home &gt; Export
      </div>
      <div className="mt-10 p-6 bg-white shadow-md rounded-lg font-rem w-full">
        <h2 className="text-xl font-semibold mb-6">Export Leads</h2>

        <form onSubmit={handleExport} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            {/* Manager ID */}
            <div className="flex items-center">
              <label className="w-32">Manager ID:</label>
              <input
                type="number"
                value={managerId || ""}
                onChange={(e) => setManagerId(e.target.value)}
                className="border p-2 rounded w-full"
              />
            </div>

            {/* Sales Rep ID */}
            <div className="flex items-center">
              <label className="w-32">Sales Rep ID:</label>
              <input
                type="number"
                value={salesRepId || ""}
                onChange={(e) => setSalesRepId(e.target.value)}
                className="border p-2 rounded w-full"
              />
            </div>

            {/* Status */}
            <div className="flex items-center">
              <label className="w-32">Status:</label>
              <select
                value={status}
                onChange={(e) => setStatus(e.target.value)}
                className="border p-2 rounded w-full"
              >
                <option value="">Select Status</option>
                <option value="New">New</option>
                <option value="Contacted">Contacted</option>
                <option value="Follow-up">Follow-up</option>
                <option value="Converted">Converted</option>
                <option value="Lost">Lost</option>
              </select>
            </div>

            {/* Start Date */}
            <div className="flex items-center">
              <label className="w-32">Start Date:</label>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="border p-2 rounded w-full"
              />
            </div>

            {/* End Date */}
            <div className="flex items-center">
              <label className="w-32">End Date:</label>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="border p-2 rounded w-full"
              />
            </div>

            {/* Format Dropdown */}
            <div className="flex items-center">
              <label className="w-32">Format:</label>
              <select
                value={format}
                onChange={(e) => setFormat(e.target.value)}
                className="border p-2 rounded w-full bg-white"
              >
                <option value="excel">Excel</option>
                <option value="pdf">PDF</option>
              </select>
            </div>
          </div>

          {/* Submit Button */}
          <div className="flex justify-end mt-4">
            <button
              type="submit"
              disabled={loading}
              className="bg-[#009699] text-white px-4 py-2 rounded hover:bg-[#009699] disabled:bg-gray-400"
            >
              {loading ? "Exporting..." : "Export Data"}
            </button>
          </div>
        </form>

        {/* Error Message */}
        {error && <p className="text-red-500 mt-4">{error}</p>}
      </div>
    </div>
  );
};

export default ExportPage;
