import React, { useState } from "react";
import { FileUploader } from "react-drag-drop-files";

const fileTypes = ["CSV"];

const CsvLeadImport = ({ token }) => {
  const [csvFile, setCsvFile] = useState(null);

  const handleChange = (file) => {
    setCsvFile(file);
  };

  const handleCsvUpload = async () => {
    if (!csvFile) return alert("Please select a CSV file");

    const formData = new FormData();
    formData.append("file", csvFile);

    try {
      const response = await fetch(
        "http://localhost:5238/manager-leads/addCsv",
        {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
          body: formData,
        }
      );

      if (response.ok) {
        alert("CSV uploaded successfully!");
        setCsvFile(null); // Reset after upload
      } else {
        alert("CSV upload failed!");
      }
    } catch (error) {
      console.error("Error uploading CSV:", error);
    }
  };

  return (
    <div className="w-[592px]">
      <label className="text-[16px] font-normal text-black text-start pb-5">
        Upload CSV File
      </label>

      <FileUploader handleChange={handleChange} name="file" types={fileTypes} />

      <button
        onClick={handleCsvUpload}
        className="mt-2 bg-[#009699] text-white p-2 rounded w-full"
      >
        Upload CSV
      </button>
    </div>
  );
};

export default CsvLeadImport;
