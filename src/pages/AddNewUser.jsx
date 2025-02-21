import React, { useEffect, useState } from "react";

const AddNewUser = () => {
  const [userRole, setUserRole] = useState(null);
  const [managers, setManagers] = useState([]);
  const [selectedRole, setSelectedRole] = useState("Manager");
  const [selectedManager, setSelectedManager] = useState("");
  const [phone, setPhone] = useState("");
  const [name, setName] = useState("");
  const [email, setEmail] = useState("");
  const [loading, setLoading] = useState(false);

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
      } catch (error) {
        console.error("Error fetching role:", error);
      }
    };

    fetchData();
  }, []);

  useEffect(() => {
    if (selectedRole === "Sales Representative" && managers.length === 0) {
      fetchManagers();
    }
  }, [selectedRole]);

  const fetchManagers = async () => {
    try {
      const token = sessionStorage.getItem("token");
      if (!token) {
        console.error("No token found");
        return;
      }

      const response = await fetch("http://localhost:5238/admin/managers", {
        method: "GET",
        headers: { Authorization: `Bearer ${token}` },
      });

      if (!response.ok) {
        console.error("Failed to fetch managers");
        return;
      }

      const data = await response.json();
      setManagers(Array.isArray(data) ? data : data.data || []);
    } catch (error) {
      console.error("Error fetching managers:", error);
    }
  };

  const handlePhoneChange = (e) => {
    const value = e.target.value;
    if (/^\d{0,10}$/.test(value)) {
      setPhone(value);
    }
  };

  const handleSubmit = async () => {
    if (!name || !email || !phone || phone.length !== 10) {
      alert("Please fill in all fields correctly.");
      return;
    }

    setLoading(true);
    try {
      const token = sessionStorage.getItem("token");
      if (!token) {
        alert("Authentication error: No token found.");
        setLoading(false);
        return;
      }

      let apiUrl = "";
      let requestBody = {};

      if (userRole === "Admin") {
        apiUrl = "http://localhost:5238/admin/create-user";
        requestBody = {
          name,
          email,
          phoneNo: phone,
          role: selectedRole,
          reportsTo:
            selectedRole === "Manager" ? 1 : parseInt(selectedManager) || null,
        };
      } else if (userRole === "Manager") {
        apiUrl = "http://localhost:5238/manager-leads/addNewLeads";
        requestBody = {
          name,
          email,
          phoneNo: phone,
          source: "Manual", // Adjust this if needed
          status: "New",
        };
      } else {
        alert("Unauthorized action.");
        setLoading(false);
        return;
      }

      console.log("Sending data:", requestBody); // Debugging log

      const response = await fetch(apiUrl, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${token}`,
        },
        body: JSON.stringify(requestBody),
      });

      const data = await response.json();
      console.log("Response received:", data); // Debugging log

      if (!response.ok) {
        throw new Error(data.message || "Failed to process request");
      }

      alert(
        userRole === "Admin"
          ? "User created successfully!"
          : "Lead added successfully!"
      );
      setName("");
      setEmail("");
      setPhone("");
      setSelectedRole("Manager");
      setSelectedManager("");
    } catch (error) {
      console.error("Error:", error.message);
      alert(error.message);
    } finally {
      setLoading(false);
    }
  };

  if (!userRole) {
    return <p>Loading...</p>;
  }

  return (
    <div className="flex-1 w-full overflow-hidden">
      <div className="text-left text-[18px] text-[#616161] font-rem font-normal mt-20">
        Home &gt; Create User
      </div>

      <div className="mt-10 p-4 bg-white shadow-md rounded-lg font-rem w-full">
        {userRole === "Admin" ? (
          <div className="flex-col items-center text-left mt-2">
            <div className="text-[20px] font-normal font-rem text-black">
              Create User
            </div>
            <div className="space-y-2 mt-[21px] w-[592px]">
              <label className="text-[16px] font-normal text-black">
                Full Name
              </label>
              <input
                type="text"
                value={name}
                onChange={(e) => setName(e.target.value)}
                className="border p-1 rounded-[10px] w-[592px] h-12"
              />

              <label className="text-[16px] font-normal text-black">
                Email Address
              </label>
              <input
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="border p-1 rounded-[10px] w-[592px] h-12"
              />

              <label className="text-[16px] font-normal text-black">
                Phone Number
              </label>
              <input
                type="text"
                value={phone}
                onChange={handlePhoneChange}
                className="border p-1 rounded-[10px] w-[592px] h-12"
              />

              <label className="text-[16px] font-normal text-black">Role</label>
              <select
                value={selectedRole}
                onChange={(e) => setSelectedRole(e.target.value)}
                className="border p-1 rounded-[10px] w-[592px] h-12"
              >
                <option value="Manager">Manager</option>
                <option value="Sales Representative">
                  Sales Representative
                </option>
              </select>

              {selectedRole === "Sales Representative" && (
                <>
                  <label className="text-[16px] font-normal text-black">
                    Reports To
                  </label>
                  <select
                    value={selectedManager}
                    onChange={(e) => setSelectedManager(e.target.value)}
                    className="border p-1 rounded-[10px] w-[592px] h-12"
                  >
                    <option value="">Select Manager</option>
                    {managers.length > 0 ? (
                      managers.map((manager) => (
                        <option key={manager.uid} value={manager.uid}>
                          {manager.name} ({manager.salesRepCount} Sales Reps)
                        </option>
                      ))
                    ) : (
                      <option disabled>Loading...</option>
                    )}
                  </select>
                </>
              )}

              <div className="flex justify-end space-x-5 pt-5">
                <button
                  className="bg-transparent text-[#009699] border-[#B1B1B1] border-[2px] rounded-[10px] px-5 py-2"
                  onClick={() => {
                    setName("");
                    setEmail("");
                    setPhone("");
                    setSelectedRole("Manager");
                    setSelectedManager("");
                  }}
                >
                  Cancel
                </button>
                <button
                  onClick={handleSubmit}
                  className="bg-[#009699] text-white rounded-[10px] px-5 py-2"
                  disabled={loading}
                >
                  {loading ? "Saving..." : "Save"}
                </button>
              </div>
            </div>
          </div>
        ) : (
          <p>Only Admins can create users.</p>
        )}
      </div>
    </div>
  );
};

export default AddNewUser;
