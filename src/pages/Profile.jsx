import React, { useState, useEffect } from "react";
import { NavLink } from "react-router-dom";
import Sidebar from "../components/Sidebar";
import Navbar from "../components/Navbar";

const Profile = () => {
  const [isSidebarOpen, setSidebarOpen] = useState(true);
  const [userData, setUserData] = useState({
    name: "",
    email: "",
    role: "",
    phoneNo: "",
    dateOfJoining: "",
    createdAt: "",
    updatedAt: "",
    reportsTo: null,
    password: "", // Add password field
  });
  const [isEditing, setIsEditing] = useState(false);
  const [originalData, setOriginalData] = useState(userData);
  const [isPasswordUpdated, setIsPasswordUpdated] = useState(false);

  const fetchProfileData = async () => {
    try {
      const token = sessionStorage.getItem("token");
      if (!token) throw new Error("Token not found. Please log in.");

      // Fetch profile data
      const profileResponse = await fetch("http://localhost:5238/profile", {
        method: "GET",
        headers: { Authorization: `Bearer ${token}` },
      });
      if (!profileResponse.ok)
        throw new Error(`HTTP error! status: ${profileResponse.status}`);

      const profile = await profileResponse.json();
      setUserData(profile);
      setOriginalData(profile); // Save original data for canceling edits
    } catch (error) {
      console.error("Error fetching profile data:", error);
    }
  };

  useEffect(() => {
    fetchProfileData();
  }, []);

  // Format date to dd-mm-yyyy
  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleDateString("en-GB"); // "en-GB" gives dd/mm/yyyy format
  };

  // Handle Save or Cancel editing
  const handleSaveClick = async () => {
    try {
      const token = sessionStorage.getItem("token");
      if (!token) throw new Error("Token not found. Please log in.");

      // Send only updated fields (avoid sending an empty password if not changed)
      const updateData = {
        name: userData.name,
        email: userData.email,
        phoneNo: userData.phoneNo,
        NewPassword: userData.password, // Send it as NewPassword
      };

      // Make the POST request to update the profile
      const response = await fetch("http://localhost:5238/profile/update", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(updateData), // Send only the necessary fields
      });
      console.log("Update Data: ", updateData);

      if (!response.ok) {
        throw new Error("Failed to update profile.");
      }
      console.log("Password:", userData.password); // Add this inside handleSaveClick to check the password value.

      // If successful, show the popup
      setIsPasswordUpdated(true);
      setTimeout(() => {
        setIsPasswordUpdated(false); // Hide the popup after 3 seconds
      }, 3000);

      // After updating, stop editing
      setIsEditing(false);
      setOriginalData(userData); // Save updated data as the original
    } catch (error) {
      console.error("Error saving profile data:", error);
    }
  };

  const handleCancelClick = () => {
    setIsEditing(false);
    setUserData(originalData);
  };

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="flex-1 overflow-y-auto px-6">
        <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
          Home &gt; Profile
        </div>

        <div className="mt-10 p-4 bg-white shadow-md rounded-lg font-rem w-full">
          <div className="flex items-center justify-between mt-2">
            <div className="text-[18px] font-rem font-normal">
              {userData.name}
            </div>
            <div className="flex items-center space-x-4 justify-end">
              {!isEditing && (
                <button
                  onClick={() => setIsEditing(true)}
                  className="text-white font-rem w-[85px]"
                >
                  Edit
                </button>
              )}

              {/* Save and Cancel buttons when editing */}
              {isEditing && (
                <>
                  <button
                    onClick={handleSaveClick}
                    className="text-white font-rem w-[85px]"
                  >
                    Save
                  </button>
                  <button
                    onClick={handleCancelClick}
                    className="font-rem bg-[#FF00003D] text-[#A90000] hover:bg-red-600 hover:text-white w-[85px]"
                  >
                    Cancel
                  </button>
                </>
              )}
            </div>
          </div>

          <div className="flex-col p-4 rounded-lg mt-4 items-center text-left space-y-4 font-rem border-[2px]">
            <p>
              Full Name: <span className="text-[#979797]">{userData.name}</span>
            </p>
            <p>
              Email: <span className="text-[#979797]">{userData.email}</span>
            </p>
            <p>
              Password:{" "}
              {isEditing ? (
                <input
                  type="password"
                  value={userData.password || ""}
                  onChange={(e) =>
                    setUserData({ ...userData, password: e.target.value })
                  }
                  className="border p-1 rounded"
                />
              ) : (
                <span className="text-[#979797]">*****</span>
              )}
            </p>

            <p>
              Phone: <span className="text-[#979797]">{userData.phoneNo}</span>
            </p>
            <p>
              Role: <span className="text-[#979797]">{userData.role}</span>
            </p>
            <p>
              Reports To:{" "}
              <span className="text-[#979797]">
                {userData.reportsTo ? userData.reportsTo : "N/A"}
              </span>
            </p>
            <p>
              Date of Joining:{" "}
              <span className="text-[#979797]">
                {formatDate(userData.dateOfJoining)}
              </span>
            </p>
            <p>
              Created At:{" "}
              <span className="text-[#979797]">
                {formatDate(userData.createdAt)}
              </span>
            </p>
            <p>
              Updated At:{" "}
              <span className="text-[#979797]">
                {formatDate(userData.updatedAt)}
              </span>
            </p>
          </div>
        </div>

        {/* Popup for Password Update */}
        {isPasswordUpdated && (
          <div className="fixed top-0 left-0 right-0 bottom-0 flex justify-center items-center bg-black bg-opacity-50">
            <div className="bg-white p-4 rounded shadow-md">
              <p>Password updated successfully!</p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Profile;
